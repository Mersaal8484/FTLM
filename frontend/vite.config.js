import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/setupTests.js'],
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
        theme_color: '#1a1a2e',
        background_color: '#0f172a',
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
    port: parseInt(process.env.VITE_FRONTEND_PORT || '3000', 10),
    proxy: {
      '/api': {
        target: `http://127.0.0.1:${process.env.VITE_API_PORT || '5000'}`,
        changeOrigin: true,
        ...(process.env.VITE_LOCAL_DEMO === 'true' ? {
          configure: proxy => proxy.on('proxyReq', (proxyRequest, request) => proxyRequest.setHeader('Host', request.headers.host)),
        } : {}),
      },
      '/hubs': {
        target: `http://localhost:${process.env.VITE_API_PORT || '5000'}`,
        ws: true,
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: 'build',
    sourcemap: false,
    chunkSizeWarningLimit: 600,
    rollupOptions: {
      output: {
        // Rolldown (used by Vite 8) accepts a manual chunk callback, not the
        // package-to-chunk object supported by earlier Rollup versions.
        manualChunks(id) {
          const moduleId = id.replaceAll('\\\\', '/');

          if (!moduleId.includes('/node_modules/')) return undefined;

          if ([
            '/node_modules/react/',
            '/node_modules/react-dom/',
            '/node_modules/react-router-dom/',
            '/node_modules/zustand/',
            '/node_modules/i18next/',
            '/node_modules/react-i18next/',
          ].some(packagePath => moduleId.includes(packagePath))) return 'vendor-core';

          if (moduleId.includes('/node_modules/@tanstack/react-query/')) return 'vendor-query';
          if (['/node_modules/chart.js/', '/node_modules/react-chartjs-2/'].some(packagePath => moduleId.includes(packagePath))) return 'vendor-charts';
          if (moduleId.includes('/node_modules/lucide-react/')) return 'vendor-icons';
          if (moduleId.includes('/node_modules/xlsx-js-style/')) return 'vendor-xlsx';
          if (moduleId.includes('/node_modules/html2canvas/')) return 'vendor-canvas';

          return undefined;
        },
      }
    }
  },
});
