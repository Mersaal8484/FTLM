import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Download, AlertTriangle, FileText } from 'lucide-react';
import client from '../api/client';
import { PageHeader, Button } from '../components/ui';
import { exportToExcel } from './reports/utils/reportExport';
import { exportToPdf } from '../utils/pdfExport';

export default function OperationalDailyLog() {
  const { t } = useTranslation();
  const [generators, setGenerators] = useState([]);
  const [generatorId, setGeneratorId] = useState('');
  const [businessDate, setBusinessDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [logData, setLogData] = useState(null);
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
      const res = await client.get(`/api/fuel-load-safety/generators/${generatorId}/daily-log/${businessDate}`);
      setLogData(res.data);
    } catch (err) {
      setErrorMsg(err.response?.data?.error || 'Failed to fetch operational log');
      setLogData(null);
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
    ['hour', 'capturedAtUtc'], ['activeKw', 'activeLoadKw'], ['apparentKva', 'apparentLoadKva'],
    ['pf', 'powerFactor'], ['frequency', 'frequencyHz'], ['lineVoltage', 'lineVoltageVolts'],
    ['current', 'currentAmps'], ['oilPress', 'oilPressure'], ['waterTemp', 'waterTemperatureCelsius'],
    ['oilTemp', 'oilTemperatureCelsius'], ['peakKw', 'peakLoadKw'], ['markers', 'markers'],
  ].map(([key, accessor]) => ({ header: t(`operational.log.${key}`), accessor }));

  const exportRows = () => (logData?.rows || []).map(row => ({
    ...row,
    capturedAtUtc: new Date(row.capturedAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    markers: row.markers?.join(', ') || '',
  }));

  const handleExportExcel = () => exportToExcel(exportRows(), exportColumns, `daily-log-${generatorId}-${businessDate}`);
  const handleExportPdf = () => exportToPdf(
    exportColumns.map(column => column.header),
    exportRows().map(row => exportColumns.map(column => row[column.accessor] ?? '')),
    t('operational.log.dailyTitle'),
    `daily-log-${generatorId}-${businessDate}.pdf`,
  );

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
              {logData.rows.map((row, idx) => (
                <tr key={idx} style={{ borderBottom: '1px solid var(--border)', background: idx % 2 === 0 ? 'var(--surface)' : 'var(--surface-2)' }}>
                  <td style={{ padding: '8px', fontWeight: 600 }}>{new Date(row.capturedAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</td>
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
