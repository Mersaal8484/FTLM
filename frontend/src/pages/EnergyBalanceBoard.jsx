import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, BarChart3 } from 'lucide-react';
import client from '../api/client';

const fetchBalance = (stationId, date) =>
  client.get(`/api/energy-balance/${stationId}`, { params: { date } }).then((r) => r.data);

const fetchStations = () =>
  client.get('/api/stations').then((r) => r.data).catch(() => []);

function BalanceRow({ label, value, unit = 'kWh', highlight, warning }) {
  return (
    <div style={{
      display: 'flex', justifyContent: 'space-between', alignItems: 'center',
      padding: '10px 0', borderBottom: '1px solid #1e293b'
    }}>
      <span style={{ color: warning ? '#f59e0b' : '#94a3b8', display: 'flex', alignItems: 'center', gap: 6 }}>
        {warning && <AlertTriangle size={15} />}
        {label}
      </span>
      <strong style={{ color: highlight ? '#22c55e' : '#fff', fontSize: 16 }}>
        {value != null ? `${Number(value).toFixed(2)} ${unit}` : '—'}
      </strong>
    </div>
  );
}

export default function EnergyBalanceBoard() {
  const { t, i18n } = useTranslation();
  const [stationId, setStationId] = useState('');
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));

  const { data: stations = [] } = useQuery({ queryKey: ['stations-list'], queryFn: fetchStations });

  const { data, isLoading, error } = useQuery({
    queryKey: ['energy-balance', stationId, date],
    queryFn: () => fetchBalance(stationId, date),
    enabled: !!stationId,
  });

  const pct = (v) => (v != null ? `${Number(v).toFixed(1)}%` : '—');

  return (
    <section className="capture-page">
      <div className="capture-heading">
        <div>
          <span className="operational-eyebrow">
            <BarChart3 size={16} /> {t('operational.balance.title')}
          </span>
          <h1>{t('operational.balance.title')}</h1>
        </div>
      </div>

      {/* Controls */}
      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 20 }}>
        <select
          value={stationId}
          onChange={(e) => setStationId(e.target.value)}
          style={{ padding: '8px 12px', borderRadius: 8, background: '#0f172a', color: '#fff', border: '1px solid #334155', flex: 1, minWidth: 160 }}
        >
          <option value="">{t('operational.balance.station')}…</option>
          {stations.map((s) => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>
        <input
          type="date" value={date} onChange={(e) => setDate(e.target.value)}
          style={{ padding: '8px 12px', borderRadius: 8, background: '#0f172a', color: '#fff', border: '1px solid #334155' }}
        />
      </div>

      {!stationId && (
        <p style={{ color: '#94a3b8', textAlign: 'center', paddingTop: 32 }}>
          {t('operational.balance.station')} …
        </p>
      )}
      {isLoading && <p style={{ padding: 24 }}>…</p>}
      {error && <p style={{ color: '#ef4444' }}>{error.message}</p>}

      {data && (
        <div style={{ background: 'var(--card-bg, #1e293b)', borderRadius: 14, padding: '8px 20px' }}>
          {/* Threshold warning banner */}
          {data.thresholdExceeded && (
            <div style={{ background: '#7c2d1244', border: '1px solid #f59e0b', borderRadius: 8, padding: '10px 14px', marginBottom: 12, color: '#fcd34d', display: 'flex', gap: 8, alignItems: 'center' }}>
              <AlertTriangle size={18} /> {t('operational.balance.threshold')}
            </div>
          )}

          <BalanceRow label={t('operational.balance.import')} value={data.importKwh} highlight />
          <BalanceRow label={`  · ${t('operational.balance.generatorImport')}`} value={data.generatorImportKwh} />
          {data.solarImportKwh > 0 && (
            <BalanceRow label={`  · ${t('operational.balance.solarImport')}`} value={data.solarImportKwh} />
          )}
          <BalanceRow label={t('operational.balance.export')} value={data.exportKwh} />
          {data.isInternalMeasured && (
            <BalanceRow label={t('operational.balance.internal')} value={data.internalConsumptionKwh} />
          )}
          <BalanceRow
            label={data.isInternalMeasured ? t('operational.balance.unexplained') : data.unexplainedLabel}
            value={data.unexplainedDiffKwh}
            warning={data.thresholdExceeded}
          />

          {/* Percentage row */}
          <div style={{ padding: '10px 0', borderBottom: '1px solid #1e293b', display: 'flex', justifyContent: 'space-between' }}>
            <span style={{ color: '#94a3b8' }}>{t('operational.balance.unexplained')} %</span>
            <strong style={{ color: data.thresholdExceeded ? '#f59e0b' : '#fff' }}>
              {pct(data.unexplainedDiffPct)}
            </strong>
          </div>

          {/* Solar share */}
          {data.solarSharePct != null && (
            <div style={{ padding: '10px 0', display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: '#94a3b8' }}>{t('operational.balance.solarShare')}</span>
              <strong style={{ color: '#f59e0b' }}>{pct(data.solarSharePct)}</strong>
            </div>
          )}
        </div>
      )}
    </section>
  );
}
