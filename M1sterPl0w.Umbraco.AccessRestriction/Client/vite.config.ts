import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      entry: "src/access-restriction-dashboard.element.ts",
      formats: ["es"],
      fileName: () => "access-restriction-dashboard.js",
    },
    outDir: "../wwwroot/App_Plugins/M1sterPl0w.Umbraco.AccessRestriction",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
  base: "/App_Plugins/M1sterPl0w.Umbraco.AccessRestriction/",
});