import { css, html, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { umbHttpClient } from '@umbraco-cms/backoffice/http-client';

interface Settings {
    enabled: boolean;
    isEnabledForced: boolean;
}

interface AllowedIpAddressEntry {
    ipAddress: string;
    description: string | null;
    createdDate: string | null;
    createdBy: string | null;
    canDelete: boolean;
}

interface RestrictedPathEntry {
    path: string;
    description: string | null;
    createdDate: string | null;
    createdBy: string | null;
    canDelete: boolean;
}

const API_BASE = '/umbraco/m1sterpl0wumbracoaccessrestriction/api/v1';
const AUTH = [{ scheme: 'bearer', type: 'http' }] as const;

@customElement('access-restriction-dashboard')
export default class AccessRestrictionDashboardElement extends UmbLitElement {
    @state() private _entries: AllowedIpAddressEntry[] = [];
    @state() private _settings: Settings = { enabled: true, isEnabledForced: false };
    @state() private _settingsSaving = false;
    @state() private _loading = true;
    @state() private _saving = false;
    @state() private _showAddForm = false;
    @state() private _newIp = '';
    @state() private _newDescription = '';
    @state() private _error = '';
    @state() private _paths: RestrictedPathEntry[] = [];
    @state() private _showAddPathForm = false;
    @state() private _newPath = '';
    @state() private _newPathDescription = '';
    @state() private _pathSaving = false;

    override connectedCallback() {
        super.connectedCallback();
        this._load();
    }

    private async _load() {
        this._loading = true;
        this._error = '';
        try {
            const [listRes, settingsRes, pathsRes] = await Promise.all([
                umbHttpClient.get({ url: `${API_BASE}/ipaddresses`, security: AUTH }),
                umbHttpClient.get({ url: `${API_BASE}/settings`, security: AUTH }),
                umbHttpClient.get({ url: `${API_BASE}/paths`, security: AUTH }),
            ]);
            if (listRes.response.ok) this._entries = (listRes.data as AllowedIpAddressEntry[]) ?? [];
            if (settingsRes.response.ok) this._settings = settingsRes.data as Settings;
            if (pathsRes.response.ok) this._paths = (pathsRes.data as RestrictedPathEntry[]) ?? [];
        } catch {
            this._error = 'Failed to load data.';
        } finally {
            this._loading = false;
        }
    }

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

    private async _addEntry(ip: string, description: string | null): Promise<boolean> {
        this._saving = true;
        this._error = '';
        try {
            const res = await umbHttpClient.post({
                url: `${API_BASE}/ipaddresses`,
                security: AUTH,
                body: { ipAddress: ip, description },
                headers: { 'Content-Type': 'application/json' },
            });
            if (res.response.status === 201) {
                await this._load();
                return true;
            }
            this._error = res.response.status === 409
                ? `"${ip}" is already in the list.`
                : `Failed to add IP (${res.response.status}).`;
        } catch {
            this._error = 'Failed to add IP address.';
        } finally {
            this._saving = false;
        }
        return false;
    }

    private async _delete(ip: string) {
        this._error = '';
        try {
            const res = await umbHttpClient.delete({
                url: `${API_BASE}/ipaddresses/${encodeURIComponent(ip)}`,
                security: AUTH,
            });
            if (res.response.ok) {
                await this._load();
            } else {
                this._error = `Failed to delete (${res.response.status}).`;
            }
        } catch {
            this._error = 'Failed to delete IP address.';
        }
    }

    private async _submitForm() {
        const ip = this._newIp.trim();
        if (!ip) return;
        const ok = await this._addEntry(ip, this._newDescription.trim() || null);
        if (ok) {
            this._newIp = '';
            this._newDescription = '';
            this._showAddForm = false;
        }
    }

    private async _addPath(path: string, description: string | null): Promise<boolean> {
        this._pathSaving = true;
        this._error = '';
        try {
            const normalized = path.startsWith('/') ? path : '/' + path;
            const res = await umbHttpClient.post({
                url: `${API_BASE}/paths`,
                security: AUTH,
                body: { path: normalized, description },
                headers: { 'Content-Type': 'application/json' },
            });
            if (res.response.status === 201) {
                await this._load();
                return true;
            }
            this._error = res.response.status === 409
                ? `"${normalized}" is already in the list.`
                : `Failed to add path (${res.response.status}).`;
        } catch {
            this._error = 'Failed to add path.';
        } finally {
            this._pathSaving = false;
        }
        return false;
    }

    private async _deletePath(path: string) {
        this._error = '';
        try {
            const encoded = encodeURIComponent(path.replace(/^\//, ''));
            const res = await umbHttpClient.delete({
                url: `${API_BASE}/paths/${encoded}`,
                security: AUTH,
            });
            if (res.response.ok) {
                await this._load();
            } else {
                this._error = `Failed to delete path (${res.response.status}).`;
            }
        } catch {
            this._error = 'Failed to delete path.';
        }
    }

    private async _submitPathForm() {
        const path = this._newPath.trim();
        if (!path) return;
        const ok = await this._addPath(path, this._newPathDescription.trim() || null);
        if (ok) {
            this._newPath = '';
            this._newPathDescription = '';
            this._showAddPathForm = false;
        }
    }

    private _onPathKeyDown(e: KeyboardEvent) {
        if (e.key === 'Enter') this._submitPathForm();
    }

    private _onKeyDown(e: KeyboardEvent) {
        if (e.key === 'Enter') this._submitForm();
    }

    private _formatDate(dateStr: string | null): string {
        if (!dateStr) return '';
        try { return new Date(dateStr).toLocaleDateString(); } catch { return dateStr; }
    }

    override render() {
        return html`
            <!-- Settings section -->
            <div class="settings-section">
                <h2>Settings</h2>
                <div class="settings-form">
                    <label class="toggle-row">
                        <input
                            type="checkbox"
                            .checked=${this._settings.enabled || this._settings.isEnabledForced}
                            ?disabled=${this._settings.isEnabledForced}
                            @change=${(e: Event) => {
                                if (this._settings.isEnabledForced) return;
                                this._settings = { ...this._settings, enabled: (e.target as HTMLInputElement).checked };
                                this._saveSettings();
                            }}>
                        <span>Enable IP whitelisting${this._settings.isEnabledForced ? html` <em class="forced-label">(forced by configuration)</em>` : ''}</span>
                    </label>
                    ${this._settingsSaving ? html`<p class="settings-saving">Saving…</p>` : ''}
                </div>
            </div>

            <!-- Restricted Paths section -->
            <div class="module-section">
                <h2>Restricted Paths</h2>
                <p class="module-description">When no paths are configured, all paths are blocked for non-whitelisted IP addresses. Add specific paths to limit restrictions to those paths only — e.g. <code>/umbraco</code> blocks everything under it, while <code>/umbraco/settings</code> blocks only that sub-path.</p>

                <div class="toolbar">
                    <uui-button look="primary" label="Add path" @click=${() => { this._showAddPathForm = !this._showAddPathForm; this._error = ''; }}>
                        <uui-icon name="icon-add"></uui-icon>
                    </uui-button>
                </div>

                ${this._showAddPathForm ? html`
                    <div class="add-form">
                        <div class="form-row">
                            <div class="form-field">
                                <label>Path *</label>
                                <uui-input
                                    placeholder="e.g. /umbraco"
                                    .value=${this._newPath}
                                    @input=${(e: Event) => (this._newPath = (e.target as HTMLInputElement).value)}
                                    @keydown=${this._onPathKeyDown}>
                                </uui-input>
                            </div>
                            <div class="form-field">
                                <label>Description</label>
                                <uui-input
                                    placeholder="Optional description"
                                    .value=${this._newPathDescription}
                                    @input=${(e: Event) => (this._newPathDescription = (e.target as HTMLInputElement).value)}
                                    @keydown=${this._onPathKeyDown}>
                                </uui-input>
                            </div>
                            <div class="form-actions">
                                <uui-button
                                    look="primary"
                                    ?disabled=${this._pathSaving || !this._newPath.trim()}
                                    @click=${this._submitPathForm}>
                                    ${this._pathSaving ? 'Saving…' : 'Save'}
                                </uui-button>
                                <uui-button @click=${() => { this._showAddPathForm = false; this._error = ''; }}>
                                    Cancel
                                </uui-button>
                            </div>
                        </div>
                    </div>
                ` : ''}

                ${this._error ? html`<p class="error-msg">${this._error}</p>` : ''}

                ${this._loading ? html`<p>Loading…</p>` : html`
                    <uui-table>
                        <uui-table-head>
                            <uui-table-head-cell>Path</uui-table-head-cell>
                            <uui-table-head-cell>Description</uui-table-head-cell>
                            <uui-table-head-cell>Created</uui-table-head-cell>
                            <uui-table-head-cell>Created By</uui-table-head-cell>
                            <uui-table-head-cell></uui-table-head-cell>
                        </uui-table-head>

                        ${this._paths.length === 0 ? html`
                            <uui-table-row>
                                <uui-table-cell>
                                    <em class="empty-text">No restricted paths configured — all paths are blocked for non-whitelisted IP addresses.</em>
                                </uui-table-cell>
                                <uui-table-cell></uui-table-cell>
                                <uui-table-cell></uui-table-cell>
                                <uui-table-cell></uui-table-cell>
                                <uui-table-cell></uui-table-cell>
                            </uui-table-row>
                        ` : this._paths.map(p => html`
                            <uui-table-row>
                                <uui-table-cell class="cell-mono">${p.path}</uui-table-cell>
                                <uui-table-cell>${p.description ?? ''}</uui-table-cell>
                                <uui-table-cell>${this._formatDate(p.createdDate)}</uui-table-cell>
                                <uui-table-cell>${p.createdBy ?? ''}</uui-table-cell>
                                <uui-table-cell>
                                    ${p.canDelete ? html`
                                        <uui-button
                                            color="danger"
                                            look="primary"
                                            label="Delete"
                                            @click=${() => this._deletePath(p.path)}>
                                            <uui-icon name="icon-trash"></uui-icon>
                                        </uui-button>
                                    ` : html`<em class="static-label">static</em>`}
                                </uui-table-cell>
                            </uui-table-row>
                        `)}
                    </uui-table>
                `}
            </div>

            <h2>Whitelisted IP Addresses</h2>

            <!-- Toolbar -->
            <div class="toolbar">
                <uui-button look="primary" label="Add IP address" @click=${() => { this._showAddForm = !this._showAddForm; this._error = ''; }}>
                    <uui-icon name="icon-add"></uui-icon>
                </uui-button>
            </div>

            <!-- Inline add form -->
            ${this._showAddForm ? html`
                <div class="add-form">
                    <div class="form-row">
                        <div class="form-field">
                            <label>IP Address *</label>
                            <uui-input
                                placeholder="e.g. 192.168.1.1"
                                .value=${this._newIp}
                                @input=${(e: Event) => (this._newIp = (e.target as HTMLInputElement).value)}
                                @keydown=${this._onKeyDown}>
                            </uui-input>
                        </div>
                        <div class="form-field">
                            <label>Description</label>
                            <uui-input
                                placeholder="Optional description"
                                .value=${this._newDescription}
                                @input=${(e: Event) => (this._newDescription = (e.target as HTMLInputElement).value)}
                                @keydown=${this._onKeyDown}>
                            </uui-input>
                        </div>
                        <div class="form-actions">
                            <uui-button
                                look="primary"
                                ?disabled=${this._saving || !this._newIp.trim()}
                                @click=${this._submitForm}>
                                ${this._saving ? 'Saving…' : 'Save'}
                            </uui-button>
                            <uui-button @click=${() => { this._showAddForm = false; this._error = ''; }}>
                                Cancel
                            </uui-button>
                        </div>
                    </div>
                </div>
            ` : ''}

            ${this._error ? html`<p class="error-msg">${this._error}</p>` : ''}

            ${this._loading ? html`<p>Loading…</p>` : html`
                <uui-table>
                    <uui-table-head>
                        <uui-table-head-cell>IP address</uui-table-head-cell>
                        <uui-table-head-cell>Description</uui-table-head-cell>
                        <uui-table-head-cell>Created</uui-table-head-cell>
                        <uui-table-head-cell>Created By</uui-table-head-cell>
                        <uui-table-head-cell></uui-table-head-cell>
                    </uui-table-head>

                    ${this._entries.length === 0 ? html`
                        <uui-table-row>
                            <uui-table-cell>
                                <em class="empty-text">No IP addresses configured — all visitors are currently allowed.</em>
                            </uui-table-cell>
                            <uui-table-cell></uui-table-cell>
                            <uui-table-cell></uui-table-cell>
                            <uui-table-cell></uui-table-cell>
                            <uui-table-cell></uui-table-cell>
                        </uui-table-row>
                    ` : this._entries.map(e => html`
                        <uui-table-row>
                            <uui-table-cell class="cell-mono">${e.ipAddress}</uui-table-cell>
                            <uui-table-cell>${e.description ?? ''}</uui-table-cell>
                            <uui-table-cell>${this._formatDate(e.createdDate)}</uui-table-cell>
                            <uui-table-cell>${e.createdBy ?? ''}</uui-table-cell>
                            <uui-table-cell>
                                ${e.canDelete ? html`
                                    <uui-button
                                        color="danger"
                                        look="primary"
                                        label="Delete"
                                        @click=${() => this._delete(e.ipAddress)}>
                                        <uui-icon name="icon-trash"></uui-icon>
                                    </uui-button>
                                ` : html`<em class="static-label">static</em>`}
                            </uui-table-cell>
                        </uui-table-row>
                    `)}
                </uui-table>
            `}
        `;
    }

    static override readonly styles = [css`
        :host {
            display: block;
            padding: 24px;
        }

        .toolbar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 16px;
        }

        .ip-status {
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .status-icon {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 20px;
            height: 20px;
            border-radius: 50%;
            font-size: 11px;
            font-weight: bold;
            flex-shrink: 0;
        }

        .status-icon.danger {
            background: #e74c3c;
            color: white;
        }

        .status-icon.ok {
            background: #27ae60;
            color: white;
        }

        .status-text {
            font-size: 14px;
            color: var(--uui-color-text, #333);
        }

        .add-form {
            background: var(--uui-color-surface, #fff);
            border: 1px solid var(--uui-color-border, #e0e0e0);
            border-radius: 4px;
            padding: 16px;
            margin-bottom: 16px;
        }

        .form-row {
            display: flex;
            gap: 16px;
            align-items: flex-end;
            flex-wrap: wrap;
        }

        .form-field {
            display: flex;
            flex-direction: column;
            gap: 4px;
            flex: 1;
            min-width: 160px;
        }

        .form-field label {
            font-size: 13px;
            font-weight: 500;
            color: var(--uui-color-text, #333);
        }

        .form-actions {
            display: flex;
            gap: 8px;
            align-items: center;
        }

        .error-msg {
            color: var(--uui-color-danger, #e74c3c);
            font-size: 13px;
            margin: 0 0 12px;
        }

        h2 {
            margin: 0 0 16px;
            font-size: 18px;
            font-weight: 600;
        }

        uui-table {
            width: 100%;
        }

        .cell-mono {
            font-family: monospace;
        }

        .empty-text {
            color: var(--uui-color-text-alt, #888);
        }

        .settings-section {
            margin-bottom: 32px;
            padding-bottom: 24px;
            border-bottom: 1px solid var(--uui-color-border, #e0e0e0);
        }

        .module-section {
            margin-bottom: 32px;
            padding-bottom: 24px;
            border-bottom: 1px solid var(--uui-color-border, #e0e0e0);
        }

        .module-description {
            font-size: 13px;
            color: var(--uui-color-text-alt, #888);
            margin: 0 0 16px;
        }

        .settings-form {
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .toggle-row {
            display: flex;
            align-items: center;
            gap: 10px;
            cursor: pointer;
            font-size: 14px;
            color: var(--uui-color-text, #333);
        }

        .toggle-row input[type='checkbox'] {
            width: 16px;
            height: 16px;
            cursor: pointer;
        }

        .settings-saving {
            font-size: 13px;
            color: var(--uui-color-text-alt, #888);
            margin: 0;
        }

        .static-label {
            font-size: 12px;
            color: var(--uui-color-text-alt, #888);
        }

        .forced-label {
            font-size: 12px;
            color: var(--uui-color-text-alt, #888);
            font-style: italic;
        }
    `];
}