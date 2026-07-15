import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    exclude: [
      '**/e2e/**',
      '**/node_modules/**',
      '**/dist/**',
      '**/build/**',
      '**/*.spec.js',
    ],
  },
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg', 'logo192.svg', 'logo512.svg'],
      manifest: {
        name: 'FuelTrack',
        short_name: 'FuelTrack',
        description: 'Enterprise Fuel Management System',
        theme_color: '#1976d2',
        background_color: '#ffffff',
        display: 'standalone',
        icons: [
          { src: 'logo192.svg', sizes: '192x192', type: 'image/svg+xml' },
          { src: 'logo512.svg', sizes: '512x512', type: 'image/svg+xml' },
          { src: 'logo192.svg', sizes: '192x192', type: 'image/png', purpose: 'maskable' },
          { src: 'logo512.svg', sizes: '512x512', type: 'image/png', purpose: 'maskable' }
        ]
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2}'],
        navigateFallback: '/index.html',
        navigateFallbackDenylist: [/^\/api\//]
      }
    })
  ],
  server: {
    host: '0.0.0.0',
    port: 3000,
    proxy: {
      '/api': 'http://localhost:5000',
      '/hubs': {
        target: 'http://localhost:5000',
        ws: true,
      },
    },
  },
  build: {
    outDir: 'build',
    sourcemap: false,
  },
});
