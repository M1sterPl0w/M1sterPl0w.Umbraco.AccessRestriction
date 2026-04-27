export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "M1sterPl 0wUmbraco Access Restriction Entrypoint",
    alias: "M1sterPl0w.Umbraco.AccessRestriction.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
