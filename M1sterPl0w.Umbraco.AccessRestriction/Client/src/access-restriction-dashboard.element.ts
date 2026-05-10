import { css, html, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { umbHttpClient } from '@umbraco-cms/backoffice/http-client';
import type { UmbInputDocumentElement } from '@umbraco-cms/backoffice/document';

// ── Types ────────────────────────────────────────────────────────────────────

interface Settings {
    enabled: boolean;
    ipHeader: string | null;
    isIpHeaderForced: boolean;
    considerRemoteIp: boolean;
    denyStatusCode: number;
    denyContentNodeKey: string | null;
}

interface Condition {
    id: number;
    type: 'Ip' | 'Path' | 'UserGroup';
    values: string[];
    canDelete: boolean;
}

interface AccessRule {
    id: number;
    name: string;
    description: string | null;
    requireAll: boolean;
    result: 'Allow' | 'Deny';
    sortOrder: number;
    canDelete: boolean;
    createdBy: string | null;
    createdDate: string | null;
    conditions: Condition[];
}

const API_BASE = '/umbraco/m1sterpl0wumbracoaccessrestriction/api/v1';
const AUTH = [{ scheme: 'bearer', type: 'http' }] as const;

const CONDITION_LABELS: Record<string, string> = {
    Ip: 'IP List',
    Path: 'Path',
    UserGroup: 'User Group',
};

// ── Component ─────────────────────────────────────────────────────────────────

@customElement('access-restriction-dashboard')
export default class AccessRestrictionDashboardElement extends UmbLitElement {
    // Data
    @state() private _settings: Settings = { enabled: true, ipHeader: null, isIpHeaderForced: false, considerRemoteIp: false, denyStatusCode: 403, denyContentNodeKey: null };
    @state() private _rules: AccessRule[] = [];
    @state() private _loading = true;
    @state() private _error = '';

    // Settings UI
    @state() private _settingsSaving = false;

    // Rule list UI
    @state() private _expandedRuleIds = new Set<number>();
    @state() private _showAddRuleForm = false;

    // Add-rule form
    @state() private _newRuleName = '';
    @state() private _newRuleDescription = '';
    @state() private _newRuleResult: 'Allow' | 'Deny' = 'Allow';
    @state() private _newRuleRequireAll = true;
    @state() private _ruleSaving = false;

    // Add-condition form (tracks which rule is being edited)
    @state() private _addConditionRuleId: number | null = null;
    @state() private _newConditionType: 'Ip' | 'Path' | 'UserGroup' = 'Ip';
    @state() private _newConditionValues = '';
    @state() private _conditionSaving = false;

    override connectedCallback() {
        super.connectedCallback();
        this._load();
    }

    // ── Data loading ─────────────────────────────────────────────────────────

    private async _load() {
        this._loading = true;
        this._error = '';
        try {
            const [settingsRes, rulesRes] = await Promise.all([
                umbHttpClient.get({ url: `${API_BASE}/settings`, security: AUTH }),
                umbHttpClient.get({ url: `${API_BASE}/rules`, security: AUTH }),
            ]);
            if (settingsRes.response.ok) this._settings = settingsRes.data as Settings;
            if (rulesRes.response.ok) this._rules = (rulesRes.data as AccessRule[]) ?? [];
        } catch {
            this._error = 'Failed to load data.';
        } finally {
            this._loading = false;
        }
    }

    // ── Settings ─────────────────────────────────────────────────────────────

    private async _saveSettings() {
        this._settingsSaving = true;
        try {
            await umbHttpClient.put({
                url: `${API_BASE}/settings`,
                security: AUTH,
                body: this._settings,
                headers: { 'Content-Type': 'application/json' },
            });
        } catch {
            this._error = 'Failed to save settings.';
        } finally {
            this._settingsSaving = false;
        }
    }

    // ── Rules ─────────────────────────────────────────────────────────────────

    private _toggleRule(id: number) {
        const next = new Set(this._expandedRuleIds);
        if (next.has(id)) next.delete(id); else next.add(id);
        this._expandedRuleIds = next;
    }

    private async _createRule() {
        if (!this._newRuleName.trim()) return;
        this._ruleSaving = true;
        this._error = '';
        try {
            const res = await umbHttpClient.post({
                url: `${API_BASE}/rules`,
                security: AUTH,
                body: {
                    name: this._newRuleName.trim(),
                    description: this._newRuleDescription.trim() || null,
                    result: this._newRuleResult,
                    requireAll: this._newRuleRequireAll,
                },
                headers: { 'Content-Type': 'application/json' },
            });
            if (res.response.status === 201) {
                this._newRuleName = '';
                this._newRuleDescription = '';
                this._newRuleResult = 'Allow';
                this._newRuleRequireAll = true;
                this._showAddRuleForm = false;
                await this._load();
            } else {
                this._error = `Failed to create rule (${res.response.status}).`;
            }
        } catch {
            this._error = 'Failed to create rule.';
        } finally {
            this._ruleSaving = false;
        }
    }

    private async _deleteRule(id: number) {
        this._error = '';
        try {
            const res = await umbHttpClient.delete({ url: `${API_BASE}/rules/${id}`, security: AUTH });
            if (res.response.ok) {
                await this._load();
            } else {
                this._error = `Failed to delete rule (${res.response.status}).`;
            }
        } catch {
            this._error = 'Failed to delete rule.';
        }
    }

    // ── Conditions ────────────────────────────────────────────────────────────

    private _startAddCondition(ruleId: number) {
        this._addConditionRuleId = ruleId;
        this._newConditionType = 'Ip';
        this._newConditionValues = '';
    }

    private _cancelAddCondition() {
        this._addConditionRuleId = null;
        this._newConditionValues = '';
    }

    private _parseValues(raw: string): string[] {
        return raw
            .split(/[\n,]+/)
            .map(v => v.trim())
            .filter(v => v.length > 0);
    }

    private async _addCondition(ruleId: number) {
        const values = this._parseValues(this._newConditionValues);
        if (values.length === 0) { this._error = 'Enter at least one value.'; return; }
        this._conditionSaving = true;
        this._error = '';
        try {
            const res = await umbHttpClient.post({
                url: `${API_BASE}/rules/${ruleId}/conditions`,
                security: AUTH,
                body: { type: this._newConditionType, values },
                headers: { 'Content-Type': 'application/json' },
            });
            if (res.response.status === 201) {
                this._addConditionRuleId = null;
                this._newConditionValues = '';
                await this._load();
            } else {
                this._error = `Failed to add condition (${res.response.status}).`;
            }
        } catch {
            this._error = 'Failed to add condition.';
        } finally {
            this._conditionSaving = false;
        }
    }

    private async _deleteCondition(conditionId: number) {
        this._error = '';
        try {
            const res = await umbHttpClient.delete({ url: `${API_BASE}/conditions/${conditionId}`, security: AUTH });
            if (res.response.ok) {
                await this._load();
            } else {
                this._error = `Failed to delete condition (${res.response.status}).`;
            }
        } catch {
            this._error = 'Failed to delete condition.';
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private _formatDate(d: string | null) {
        if (!d) return '';
        try { return new Date(d).toLocaleDateString(); } catch { return d; }
    }

    // ── Render ────────────────────────────────────────────────────────────────

    override render() {
        return html`
            ${this._renderSettings()}
            ${this._renderRules()}
        `;
    }

    private _renderSettings() {
        return html`
            <div class="section">
                <h2>Settings</h2>
                <div class="settings-grid">
                    <span class="settings-label">Enable access restriction</span>
                    <uui-toggle
                        .checked=${this._settings.enabled}
                        @change=${(e: Event) => {
                            this._settings = { ...this._settings, enabled: (e.target as HTMLInputElement).checked };
                            this._saveSettings();
                        }}>
                    </uui-toggle>

                    <span class="settings-label">
                        IP header${this._settings.isIpHeaderForced ? html` <em class="static-label">static</em>` : ''}
                    </span>
                    <uui-input
                        placeholder="e.g. X-Forwarded-For"
                        .value=${this._settings.ipHeader ?? ''}
                        ?disabled=${this._settings.isIpHeaderForced}
                        @input=${(e: Event) => {
                            if (this._settings.isIpHeaderForced) return;
                            this._settings = { ...this._settings, ipHeader: (e.target as HTMLInputElement).value || null };
                        }}
                        @blur=${() => { if (!this._settings.isIpHeaderForced) this._saveSettings(); }}>
                    </uui-input>

                    ${this._settingsSaving ? html`<span class="settings-saving" style="grid-column: 1/-1">Saving…</span>` : ''}

                    <span class="settings-label">Also consider direct IP</span>
                    <uui-toggle
                        .checked=${this._settings.considerRemoteIp}
                        @change=${(e: Event) => {
                            this._settings = { ...this._settings, considerRemoteIp: (e.target as HTMLInputElement).checked };
                            this._saveSettings();
                        }}>
                    </uui-toggle>

                    <span class="settings-label">Deny status code</span>
                    <uui-input
                        type="number"
                        min="100"
                        max="599"
                        placeholder="403"
                        .value=${String(this._settings.denyStatusCode)}
                        @input=${(e: Event) => {
                            const v = parseInt((e.target as HTMLInputElement).value, 10);
                            if (!isNaN(v)) this._settings = { ...this._settings, denyStatusCode: v };
                        }}
                        @blur=${() => this._saveSettings()}>
                    </uui-input>

                    <span class="settings-label">Deny content node</span>
                    <umb-input-document
                        max="1"
                        .value=${this._settings.denyContentNodeKey ?? ''}
                        @change=${(e: Event) => {
                            const val = (e.target as UmbInputDocumentElement).value ?? null;
                            this._settings = { ...this._settings, denyContentNodeKey: val || null };
                            this._saveSettings();
                        }}>
                    </umb-input-document>
                </div>
            </div>
        `;
    }

    private _renderRules() {
        return html`
            <div class="section">
                <div class="section-header">
                    <div>
                        <h2>Access Rules</h2>
                        <p class="description">
                            Rules are evaluated in order. The first matching rule wins.
                            If no rule matches, access is <strong>allowed</strong>.
                            A rule with no conditions is skipped.
                        </p>
                    </div>
                    <uui-button look="primary" @click=${() => { this._showAddRuleForm = !this._showAddRuleForm; this._error = ''; }}>
                        <uui-icon name="icon-add"></uui-icon> Add rule
                    </uui-button>
                </div>

                ${this._showAddRuleForm ? this._renderAddRuleForm() : ''}
                ${this._error ? html`<p class="error-msg">${this._error}</p>` : ''}

                ${this._loading
                    ? html`<p>Loading…</p>`
                    : this._rules.length === 0
                        ? html`<p class="empty">No rules configured — all requests are currently allowed.</p>`
                        : this._rules.map(r => this._renderRuleCard(r))
                }
            </div>
        `;
    }

    private _renderAddRuleForm() {
        return html`
            <div class="card form-card">
                <h3>New Rule</h3>
                <div class="form-grid">
                    <label>Name *</label>
                    <uui-input
                        placeholder="e.g. Block public API"
                        .value=${this._newRuleName}
                        @input=${(e: Event) => (this._newRuleName = (e.target as HTMLInputElement).value)}>
                    </uui-input>

                    <label>Description</label>
                    <uui-input
                        placeholder="Optional"
                        .value=${this._newRuleDescription}
                        @input=${(e: Event) => (this._newRuleDescription = (e.target as HTMLInputElement).value)}>
                    </uui-input>

                    <label>Result</label>
                    <div class="radio-group">
                        <label class="radio-label">
                            <input type="radio" name="result" value="Allow"
                                .checked=${this._newRuleResult === 'Allow'}
                                @change=${() => (this._newRuleResult = 'Allow')}>
                            <span class="badge allow">Allow</span>
                        </label>
                        <label class="radio-label">
                            <input type="radio" name="result" value="Deny"
                                .checked=${this._newRuleResult === 'Deny'}
                                @change=${() => (this._newRuleResult = 'Deny')}>
                            <span class="badge deny">Deny</span>
                        </label>
                    </div>

                    <label>Match</label>
                    <div class="radio-group">
                        <label class="radio-label">
                            <input type="radio" name="match"
                                .checked=${this._newRuleRequireAll}
                                @change=${() => (this._newRuleRequireAll = true)}>
                            ALL conditions (AND)
                        </label>
                        <label class="radio-label">
                            <input type="radio" name="match"
                                .checked=${!this._newRuleRequireAll}
                                @change=${() => (this._newRuleRequireAll = false)}>
                            ANY condition (OR)
                        </label>
                    </div>
                </div>
                <div class="form-actions">
                    <uui-button look="primary"
                        ?disabled=${this._ruleSaving || !this._newRuleName.trim()}
                        @click=${this._createRule}>
                        ${this._ruleSaving ? 'Saving…' : 'Save rule'}
                    </uui-button>
                    <uui-button @click=${() => { this._showAddRuleForm = false; this._error = ''; }}>
                        Cancel
                    </uui-button>
                </div>
            </div>
        `;
    }

    private _renderRuleCard(rule: AccessRule) {
        const expanded = this._expandedRuleIds.has(rule.id);
        return html`
            <div class="card rule-card">
                <div class="rule-header" @click=${() => this._toggleRule(rule.id)}>
                    <span class="badge ${rule.result === 'Allow' ? 'allow' : 'deny'}">${rule.result}</span>
                    <span class="rule-name">${rule.name}</span>
                    <span class="rule-meta">${rule.requireAll ? 'ALL' : 'ANY'} &middot; ${rule.conditions.length} condition${rule.conditions.length !== 1 ? 's' : ''}</span>
                    ${!rule.canDelete ? html`<em class="static-label">static</em>` : ''}
                    ${rule.createdDate ? html`<span class="rule-meta">${this._formatDate(rule.createdDate)}</span>` : ''}
                    <uui-icon class="chevron" name="${expanded ? 'icon-arrow-up' : 'icon-arrow-down'}"></uui-icon>
                </div>

                ${expanded ? html`
                    <div class="rule-body">
                        ${rule.description ? html`<p class="rule-desc">${rule.description}</p>` : ''}

                        <div class="conditions-list">
                            ${rule.conditions.length === 0
                                ? html`<p class="empty-conditions">No conditions — add one below to make this rule match something.</p>`
                                : rule.conditions.map(c => this._renderCondition(c, rule.canDelete))
                            }
                        </div>

                        ${rule.canDelete && this._addConditionRuleId !== rule.id ? html`
                            <uui-button @click=${() => this._startAddCondition(rule.id)}>
                                <uui-icon name="icon-add"></uui-icon> Add condition
                            </uui-button>
                        ` : ''}

                        ${this._addConditionRuleId === rule.id ? this._renderAddConditionForm(rule.id) : ''}

                        ${rule.canDelete ? html`
                            <div class="rule-footer">
                                <uui-button color="danger" look="outline"
                                    @click=${() => this._deleteRule(rule.id)}>
                                    <uui-icon name="icon-trash"></uui-icon> Delete rule
                                </uui-button>
                            </div>
                        ` : ''}
                    </div>
                ` : ''}
            </div>
        `;
    }

    private _renderCondition(condition: Condition, ruleCanDelete: boolean) {
        return html`
            <div class="condition-row">
                <span class="condition-type-badge">${CONDITION_LABELS[condition.type] ?? condition.type}</span>
                <div class="condition-values">
                    ${condition.values.map(v => html`<code class="value-chip">${v}</code>`)}
                </div>
                ${ruleCanDelete && condition.canDelete ? html`
                    <uui-button color="danger" look="outline" compact
                        @click=${() => this._deleteCondition(condition.id)}>
                        <uui-icon name="icon-trash"></uui-icon>
                    </uui-button>
                ` : html`<em class="static-label">static</em>`}
            </div>
        `;
    }

    private _renderAddConditionForm(ruleId: number) {
        const placeholder = {
            Ip:        'e.g. 192.168.1.1\n10.0.0.0/8  (IPv4 or IPv6)',
            Path:      'e.g. /umbraco\n/api/private',
            UserGroup: 'e.g. Admin\nEditor',
        }[this._newConditionType];

        return html`
            <div class="add-condition-form">
                <div class="form-row">
                    <div class="form-field">
                        <label>Type</label>
                        <select
                            .value=${this._newConditionType}
                            @change=${(e: Event) => (this._newConditionType = (e.target as HTMLSelectElement).value as 'Ip' | 'Path' | 'UserGroup')}>
                            <option value="Ip">IP List</option>
                            <option value="Path">Path</option>
                            <option value="UserGroup">User Group</option>
                        </select>
                    </div>
                    <div class="form-field flex-grow">
                        <label>Values <em class="hint">(one per line or comma-separated)</em></label>
                        <textarea
                            rows="3"
                            placeholder=${placeholder}
                            .value=${this._newConditionValues}
                            @input=${(e: Event) => (this._newConditionValues = (e.target as HTMLTextAreaElement).value)}>
                        </textarea>
                    </div>
                </div>
                <div class="form-actions">
                    <uui-button look="primary"
                        ?disabled=${this._conditionSaving || !this._newConditionValues.trim()}
                        @click=${() => this._addCondition(ruleId)}>
                        ${this._conditionSaving ? 'Saving…' : 'Add condition'}
                    </uui-button>
                    <uui-button @click=${this._cancelAddCondition}>Cancel</uui-button>
                </div>
            </div>
        `;
    }

    // ── Styles ────────────────────────────────────────────────────────────────

    static override readonly styles = [css`
        :host {
            display: block;
            padding: 24px;
            max-width: 900px;
        }

        h2 {
            margin: 0 0 8px;
            font-size: 18px;
            font-weight: 600;
        }

        h3 {
            margin: 0 0 16px;
            font-size: 15px;
            font-weight: 600;
        }

        .section {
            margin-bottom: 40px;
            padding-bottom: 32px;
            border-bottom: 1px solid var(--uui-color-border, #e0e0e0);
        }

        .section-header {
            display: flex;
            align-items: flex-start;
            justify-content: space-between;
            gap: 16px;
            margin-bottom: 8px;
        }

        .description {
            font-size: 13px;
            color: var(--uui-color-text-alt, #888);
            margin: 0 0 20px;
        }

        /* ── Settings grid ── */
        .settings-grid {
            display: grid;
            grid-template-columns: 180px 1fr;
            column-gap: 20px;
            row-gap: 14px;
            align-items: center;
            max-width: 420px;
        }

        .settings-label {
            font-size: 14px;
            color: var(--uui-color-text, #333);
            white-space: nowrap;
        }

        .settings-saving {
            font-size: 13px;
            color: var(--uui-color-text-alt, #888);
        }

        /* ── Cards ── */
        .card {
            border: 1px solid var(--uui-color-border, #e0e0e0);
            border-radius: 6px;
            margin-bottom: 12px;
            background: var(--uui-color-surface, #fff);
            overflow: hidden;
        }

        .form-card {
            padding: 20px;
        }

        /* ── Rule card ── */
        .rule-header {
            display: flex;
            align-items: center;
            gap: 10px;
            padding: 14px 16px;
            cursor: pointer;
            user-select: none;
            flex-wrap: wrap;
        }

        .rule-header:hover {
            background: var(--uui-color-surface-alt, #f5f5f5);
        }

        .rule-name {
            font-weight: 600;
            font-size: 14px;
            flex: 1;
        }

        .rule-meta {
            font-size: 12px;
            color: var(--uui-color-text-alt, #888);
        }

        .chevron {
            margin-left: auto;
            flex-shrink: 0;
        }

        .rule-body {
            padding: 0 16px 16px;
            border-top: 1px solid var(--uui-color-border, #e0e0e0);
            padding-top: 12px;
        }

        .rule-desc {
            font-size: 13px;
            color: var(--uui-color-text-alt, #888);
            margin: 0 0 12px;
        }

        .rule-footer {
            margin-top: 16px;
            padding-top: 16px;
            border-top: 1px solid var(--uui-color-border, #e0e0e0);
        }

        /* ── Conditions ── */
        .conditions-list {
            display: flex;
            flex-direction: column;
            gap: 8px;
            margin-bottom: 12px;
        }

        .condition-row {
            display: flex;
            align-items: flex-start;
            gap: 10px;
            padding: 8px 10px;
            background: var(--uui-color-surface-alt, #f9f9f9);
            border-radius: 4px;
            flex-wrap: wrap;
        }

        .condition-type-badge {
            font-size: 11px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            background: var(--uui-color-current, #1b264f);
            color: #fff;
            border-radius: 3px;
            padding: 2px 6px;
            white-space: nowrap;
            flex-shrink: 0;
            margin-top: 2px;
        }

        .condition-values {
            display: flex;
            flex-wrap: wrap;
            gap: 4px;
            flex: 1;
        }

        .value-chip {
            font-family: monospace;
            font-size: 12px;
            background: var(--uui-color-surface, #fff);
            border: 1px solid var(--uui-color-border, #ddd);
            border-radius: 3px;
            padding: 1px 6px;
        }

        .empty-conditions {
            font-size: 13px;
            color: var(--uui-color-text-alt, #888);
            margin: 0;
        }

        /* ── Add condition form ── */
        .add-condition-form {
            background: var(--uui-color-surface-alt, #f5f5f5);
            border: 1px solid var(--uui-color-border, #e0e0e0);
            border-radius: 4px;
            padding: 14px;
            margin-top: 10px;
            margin-bottom: 10px;
        }

        .form-row {
            display: flex;
            gap: 14px;
            align-items: flex-start;
            flex-wrap: wrap;
        }

        .form-field {
            display: flex;
            flex-direction: column;
            gap: 4px;
        }

        .form-field.flex-grow {
            flex: 1;
            min-width: 200px;
        }

        .form-field label {
            font-size: 13px;
            font-weight: 500;
            color: var(--uui-color-text, #333);
        }

        .hint {
            font-weight: 400;
            font-style: italic;
            color: var(--uui-color-text-alt, #888);
        }

        .form-field select {
            height: 36px;
            padding: 0 8px;
            border: 1px solid var(--uui-color-border, #ccc);
            border-radius: 4px;
            font-size: 14px;
            background: var(--uui-color-surface, #fff);
        }

        .form-field textarea {
            padding: 6px 8px;
            border: 1px solid var(--uui-color-border, #ccc);
            border-radius: 4px;
            font-family: monospace;
            font-size: 13px;
            resize: vertical;
            background: var(--uui-color-surface, #fff);
            width: 100%;
            box-sizing: border-box;
        }

        /* ── Add rule form ── */
        .form-grid {
            display: grid;
            grid-template-columns: max-content 1fr;
            column-gap: 20px;
            row-gap: 12px;
            align-items: center;
            max-width: 560px;
            margin-bottom: 16px;
        }

        .form-grid label {
            font-size: 13px;
            font-weight: 500;
            color: var(--uui-color-text, #333);
        }

        .form-actions {
            display: flex;
            gap: 8px;
            align-items: center;
            margin-top: 8px;
        }

        /* ── Badges ── */
        .badge {
            display: inline-block;
            font-size: 11px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            border-radius: 3px;
            padding: 2px 7px;
            flex-shrink: 0;
        }

        .badge.allow {
            background: #d4edda;
            color: #155724;
        }

        .badge.deny {
            background: #f8d7da;
            color: #721c24;
        }

        /* ── Radio group ── */
        .radio-group {
            display: flex;
            gap: 20px;
            flex-wrap: wrap;
        }

        .radio-label {
            display: flex;
            align-items: center;
            gap: 6px;
            font-size: 14px;
            cursor: pointer;
        }

        /* ── Misc ── */
        .static-label {
            font-size: 12px;
            color: var(--uui-color-text-alt, #888);
        }

        .error-msg {
            color: var(--uui-color-danger, #e74c3c);
            font-size: 13px;
            margin: 8px 0;
        }

        .empty {
            font-size: 14px;
            color: var(--uui-color-text-alt, #888);
        }
    `];
}

