import React, { useState, useEffect, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Download, AlertTriangle, FileText } from 'lucide-react';
import { Bar } from 'react-chartjs-2';
import { BarElement, CategoryScale, Chart as ChartJS, Legend, LinearScale, Tooltip } from 'chart.js';
import client from '../api/client';
import { PageHeader, Button } from '../components/ui';
import { exportToExcel } from './reports/utils/reportExport';
import { exportToPdf } from '../utils/pdfExport';

ChartJS.register(BarElement, CategoryScale, LinearScale, Tooltip, Legend);

export default function OperationalDailyLog() {
  const { t } = useTranslation();
  const [generators, setGenerators] = useState([]);
  const [generatorId, setGeneratorId] = useState('');
  const [businessDate, setBusinessDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [logData, setLogData] = useState(null);
  const [completeness, setCompleteness] = useState(null);
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    client.get('/api/generators').then(res => {
      const list = res.data?.data || res.data?.items || res.data || [];
      setGenerators(list);
      if (list.length > 0 && !generatorId) {
        setGeneratorId(list[0].id);
      }
    }).catch(() => {});
  }, []);

  const fetchLog = async () => {
    if (!generatorId || !businessDate) return;
    setLoading(true);
    setErrorMsg('');
    try {
      const [log, quality] = await Promise.all([
        client.get(`/api/fuel-load-safety/generators/${generatorId}/daily-log/${businessDate}`),
        client.get(`/api/operational-completeness/generators/${generatorId}/${businessDate}`),
      ]);
      setLogData(log.data);
      setCompleteness(quality.data);
    } catch (err) {
      setErrorMsg(err.response?.data?.error || 'Failed to fetch operational log');
      setLogData(null);
      setCompleteness(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (generatorId && businessDate) {
      fetchLog();
    }
  }, [generatorId, businessDate]);

  const handleDownloadCsv = () => {
    if (!generatorId || !businessDate) return;
    window.open(`/api/fuel-load-safety/generators/${generatorId}/daily-log/${businessDate}.csv`, '_blank');
  };

  const exportColumns = [
    ['hour', 'displayHour'], ['activeKw', 'activeLoadKw'], ['apparentKva', 'apparentLoadKva'],
    ['pf', 'powerFactor'], ['frequency', 'frequencyHz'], ['lineVoltage', 'lineVoltageVolts'],
    ['current', 'currentAmps'], ['oilPress', 'oilPressure'], ['waterTemp', 'waterTemperatureCelsius'],
    ['oilTemp', 'oilTemperatureCelsius'], ['peakKw', 'peakLoadKw'], ['markers', 'markers'],
  ].map(([key, accessor]) => ({ header: t(`operational.log.${key}`), accessor }));

  const displayRows = () => {
    const readingsByHour = new Map((logData?.rows || []).map(row => [new Date(row.capturedAtUtc).getHours(), row]));
    return Array.from({ length: 24 }, (_, hour) => {
      const reading = readingsByHour.get(hour);
      return reading
        ? { ...reading, displayHour: new Date(reading.capturedAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) }
        : { displayHour: `${String(hour).padStart(2, '0')}:00`, isGap: true, markers: [t('operational.log.hourGap')] };
    });
  };

  const exportRows = () => displayRows().map(row => ({
    ...row,
    displayHour: row.displayHour,
    markers: row.markers?.join(', ') || '',
  }));

  const handleExportExcel = () => exportToExcel(exportRows(), exportColumns, `daily-log-${generatorId}-${businessDate}`);
  const handleExportPdf = () => exportToPdf(
    exportColumns.map(column => column.header),
    exportRows().map(row => exportColumns.map(column => row[column.accessor] ?? '')),
    t('operational.log.dailyTitle'),
    `daily-log-${generatorId}-${businessDate}.pdf`,
  );

  const productionChart = useMemo(() => {
    const points = logData?.hourlyProduction || [];
    if (!points.some(point => point.kwh != null)) return null;
    return {
      labels: points.map(point => new Date(point.slotStartUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })),
      datasets: [{
        label: t('operational.log.hourlyProduction'),
        data: points.map(point => point.kwh),
        backgroundColor: points.map(point => point.isEstimated ? 'rgba(245, 158, 11, 0.82)' : 'rgba(45, 212, 191, 0.86)'),
        borderRadius: 4,
        borderSkipped: false,
      }],
      estimated: points.some(point => point.isEstimated && point.kwh != null),
    };
  }, [logData, t]);

  const productionChartOptions = useMemo(() => ({
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: context => `${context.raw ?? '-'} kWh`,
          afterLabel: context => productionChart?.estimated && logData?.hourlyProduction?.[context.dataIndex]?.isEstimated
            ? t('operational.log.estimatedFromLoad') : t('operational.log.measured'),
        },
      },
    },
    scales: {
      x: { ticks: { color: 'var(--text-muted)' }, grid: { display: false } },
      y: { beginAtZero: true, ticks: { color: 'var(--text-muted)' }, grid: { color: 'rgba(148, 163, 184, 0.14)' } },
    },
  }), [logData, productionChart?.estimated, t]);

  return (
    <div className="capture-page operational-shell" style={{ maxWidth: 1200, margin: '0 auto', padding: 24 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 16 }}>
        <PageHeader
          title={t('operational.log.dailyTitle')}
          subtitle={t('operational.log.dailySubtitle')}
        />
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          <Button onClick={handleDownloadCsv} disabled={!generatorId || !logData?.rows?.length}>
            <Download size={16} /> {t('operational.log.exportCsv')}
          </Button>
          <Button onClick={handleExportExcel} disabled={!generatorId || !logData?.rows?.length}>
            <Download size={16} /> {t('operational.log.exportExcel')}
          </Button>
          <Button onClick={handleExportPdf} disabled={!generatorId || !logData?.rows?.length}>
            <FileText size={16} /> {t('operational.log.exportPdf')}
          </Button>
        </div>
      </div>

      <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', margin: '20px 0', alignItems: 'center' }}>
        <div style={{ minWidth: 220 }}>
          <label style={{ display: 'block', fontSize: 13, color: 'var(--text-muted)', marginBottom: 4 }}>
            {t('operational.log.generator')}
          </label>
          <select
            value={generatorId}
            onChange={e => setGeneratorId(e.target.value)}
            style={{ width: '100%', padding: '9px 12px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
          >
            {generators.map(g => (
              <option key={g.id} value={g.id}>{g.name} ({g.subStationName || `Station ${g.subStationId}`})</option>
            ))}
          </select>
        </div>

        <div>
          <label style={{ display: 'block', fontSize: 13, color: 'var(--text-muted)', marginBottom: 4 }}>
            {t('operational.log.date')}
          </label>
          <input
            type="date"
            value={businessDate}
            onChange={e => setBusinessDate(e.target.value)}
            style={{ padding: '8px 12px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
          />
        </div>
      </div>

      {errorMsg && (
        <div style={{ padding: 12, borderRadius: 8, background: 'rgba(239,68,68,0.15)', border: '1px solid #ef4444', color: '#ef4444', marginBottom: 16 }}>
          <AlertTriangle size={16} style={{ verticalAlign: 'middle', marginInlineEnd: 6 }} /> {errorMsg}
        </div>
      )}

      {completeness && (completeness.averageLoadWarning || completeness.missingSlots?.length || completeness.missingDailyClosing) && (
        <div style={{ padding: 12, borderRadius: 8, background: 'var(--operational-amber-subtle)', border: '1px solid var(--operational-amber)', color: 'var(--text)', marginBottom: 16 }}>
          <AlertTriangle size={16} style={{ verticalAlign: 'middle', marginInlineEnd: 6, color: 'var(--operational-amber)' }} />
          {completeness.averageLoadWarning && <span>{t('operational.log.averageLoadWarning')} </span>}
          {completeness.missingSlots?.length > 0 && <span>{t('operational.log.missingHours', { hours: completeness.missingSlots.join(', ') })} </span>}
          {completeness.missingDailyClosing && <span>{t('operational.log.missingDailyClosing')}</span>}
        </div>
      )}

      {productionChart && (
        <section style={{ marginBottom: 16, padding: 18, borderRadius: 12, border: '1px solid var(--border)', background: 'linear-gradient(145deg, var(--surface), var(--surface-2))' }}>
          <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', flexWrap: 'wrap', gap: 8, marginBottom: 12 }}>
            <div>
              <div style={{ fontSize: 15, fontWeight: 700 }}>{t('operational.log.hourlyProduction')}</div>
              <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{t('operational.log.productionMethod')}</div>
            </div>
            <div style={{ display: 'flex', gap: 12, fontSize: 12, color: 'var(--text-muted)' }}>
              <span><i style={{ display: 'inline-block', width: 9, height: 9, borderRadius: 2, background: 'rgba(45, 212, 191, 0.86)', marginInlineEnd: 5 }} />{t('operational.log.measured')}</span>
              <span><i style={{ display: 'inline-block', width: 9, height: 9, borderRadius: 2, background: 'rgba(245, 158, 11, 0.82)', marginInlineEnd: 5 }} />{t('operational.log.estimatedFromLoad')}</span>
            </div>
          </div>
          <div style={{ height: 210 }}><Bar data={productionChart} options={productionChartOptions} /></div>
        </section>
      )}

      {loading ? (
        <p style={{ padding: 32, textAlign: 'center', color: 'var(--text-muted)' }}>{t('operational.log.loading')}</p>
      ) : !logData?.rows?.length ? (
        <p style={{ padding: 32, textAlign: 'center', color: 'var(--text-muted)' }}>{t('operational.log.empty')}</p>
      ) : (
        <div style={{ overflowX: 'auto', borderRadius: 10, border: '1px solid var(--border)' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'center', fontSize: 13 }}>
            <thead>
              <tr style={{ background: 'var(--surface-3)', borderBottom: '1px solid var(--border)', color: 'var(--text-muted)' }}>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.hour')}</th>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.activeKw')}</th>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.apparentKva')}</th>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.pf')}</th>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.frequency')}</th>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.lineVoltage')}</th>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.current')}</th>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.oilPress')}</th>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.waterTemp')}</th>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.oilTemp')}</th>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.peakKw')}</th>
                <th style={{ padding: '10px 8px' }}>{t('operational.log.markers')}</th>
              </tr>
            </thead>
            <tbody>
              {displayRows().map((row, idx) => (
                <tr key={row.displayHour} style={{ borderBottom: '1px solid var(--border)', background: row.isGap ? 'rgba(245,158,11,0.08)' : idx % 2 === 0 ? 'var(--surface)' : 'var(--surface-2)' }}>
                  <td style={{ padding: '8px', fontWeight: 600 }}>{row.displayHour}</td>
                  <td style={{ padding: '8px', fontWeight: 600, color: 'var(--operational-amber)' }}>{row.activeLoadKw != null ? row.activeLoadKw : '-'}</td>
                  <td style={{ padding: '8px' }}>{row.apparentLoadKva != null ? row.apparentLoadKva : '-'}</td>
                  <td style={{ padding: '8px' }}>{row.powerFactor != null ? row.powerFactor : '-'}</td>
                  <td style={{ padding: '8px' }}>{row.frequencyHz != null ? row.frequencyHz : '-'}</td>
                  <td style={{ padding: '8px' }}>{row.lineVoltageVolts != null ? row.lineVoltageVolts : '-'}</td>
                  <td style={{ padding: '8px' }}>{row.currentAmps != null ? row.currentAmps : '-'}</td>
                  <td style={{ padding: '8px' }}>{row.oilPressure != null ? row.oilPressure : '-'}</td>
                  <td style={{ padding: '8px' }}>{row.waterTemperatureCelsius != null ? row.waterTemperatureCelsius : '-'}</td>
                  <td style={{ padding: '8px' }}>{row.oilTemperatureCelsius != null ? row.oilTemperatureCelsius : '-'}</td>
                  <td style={{ padding: '8px' }}>{row.peakLoadKw != null ? row.peakLoadKw : '-'}</td>
                  <td style={{ padding: '8px', color: '#f59e0b', fontSize: 12 }}>
                    {row.markers?.length ? row.markers.join(', ') : '-'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
