import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    host: true,
    proxy: {
      "/api": {
        target: "http://localhost:5265",
        changeOrigin: true,
      },
      "/hubs": {
        target: "http://localhost:5265",
        ws: true,
        changeOrigin: true,
      },
    },
  },
});
