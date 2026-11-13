import React from 'react';
import { Bar, Line } from 'react-chartjs-2';
import {
  Chart as ChartJS, CategoryScale, LinearScale, BarElement, PointElement,
  LineElement, Title, Tooltip, Legend, Filler
} from 'chart.js';
import useThemeTokens from '../../../../utils/useThemeTokens';

ChartJS.register(CategoryScale, LinearScale, BarElement, PointElement, LineElement, Title, Tooltip, Legend, Filler);

export function BarChart({ data, options, height = 300 }) {
  const tokens = useThemeTokens();
  const defaultOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'top', labels: { color: tokens.muted, usePointStyle: true, boxWidth: 8 } },
      tooltip: { backgroundColor: tokens.surface, titleColor: tokens.text, bodyColor: tokens.muted, borderColor: tokens.grid, borderWidth: 1 },
    },
    scales: {
      x: { grid: { color: tokens.grid }, ticks: { color: tokens.muted } },
      y: { grid: { color: tokens.grid }, ticks: { color: tokens.muted } },
    },
    ...options
  };

  return (
    <div className="report-chart" style={{ height }}>
      <Bar data={data} options={defaultOptions} />
    </div>
  );
}

export function LineChart({ data, options, height = 300 }) {
  const tokens = useThemeTokens();
  const defaultOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'top', labels: { color: tokens.muted, usePointStyle: true, boxWidth: 8 } },
      tooltip: { backgroundColor: tokens.surface, titleColor: tokens.text, bodyColor: tokens.muted, borderColor: tokens.grid, borderWidth: 1 },
    },
    scales: {
      x: { grid: { color: tokens.grid }, ticks: { color: tokens.muted } },
      y: { grid: { color: tokens.grid }, ticks: { color: tokens.muted } },
    },
    ...options
  };

  return (
    <div className="report-chart" style={{ height }}>
      <Line data={data} options={defaultOptions} />
    </div>
  );
}
