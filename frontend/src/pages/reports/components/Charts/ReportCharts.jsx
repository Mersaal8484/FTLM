import React from 'react';
import { Bar, Line } from 'react-chartjs-2';
import {
  Chart as ChartJS, CategoryScale, LinearScale, BarElement, PointElement,
  LineElement, Title, Tooltip, Legend, Filler
} from 'chart.js';

ChartJS.register(CategoryScale, LinearScale, BarElement, PointElement, LineElement, Title, Tooltip, Legend, Filler);

export function BarChart({ data, options, height = 300 }) {
  const defaultOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'top', labels: { color: '#9ca3af' } },
    },
    scales: {
      x: { grid: { color: 'rgba(255, 255, 255, 0.05)' }, ticks: { color: '#9ca3af' } },
      y: { grid: { color: 'rgba(255, 255, 255, 0.05)' }, ticks: { color: '#9ca3af' } },
    },
    ...options
  };

  return (
    <div style={{ height, width: '100%', padding: '1rem', background: 'var(--report-card)', borderRadius: '8px', border: '1px solid var(--report-border)' }}>
      <Bar data={data} options={defaultOptions} />
    </div>
  );
}

export function LineChart({ data, options, height = 300 }) {
  const defaultOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'top', labels: { color: '#9ca3af' } },
    },
    scales: {
      x: { grid: { color: 'rgba(255, 255, 255, 0.05)' }, ticks: { color: '#9ca3af' } },
      y: { grid: { color: 'rgba(255, 255, 255, 0.05)' }, ticks: { color: '#9ca3af' } },
    },
    ...options
  };

  return (
    <div style={{ height, width: '100%', padding: '1rem', background: 'var(--report-card)', borderRadius: '8px', border: '1px solid var(--report-border)' }}>
      <Line data={data} options={defaultOptions} />
    </div>
  );
}
