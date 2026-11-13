import React from 'react';
import ReactDOM from 'react-dom/client';
import axios from 'axios';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import './styles.css';
import './i18n';
import App from './App';
import { installChunkRecovery } from './utils/chunkRecovery';

installChunkRecovery();

// إرسال الـ cookie تلقائياً مع كل طلب axios في أي صفحة
axios.defaults.withCredentials = true;
axios.defaults.timeout = 30000;

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30 * 1000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </React.StrictMode>
);
