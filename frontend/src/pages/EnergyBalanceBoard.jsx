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
      padding: '10px 0', borderBottom: '1px solid var(--chart-grid)'
    }}>
      <span style={{ color: warning ? 'var(--operational-amber)' : 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: 6 }}>
        {warning && <AlertTriangle size={15} />}
        {label}
      </span>
      <strong style={{ color: highlight ? 'var(--operational-teal)' : 'var(--text)', fontSize: 16 }}>
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
          className="form-control" style={{ flex: 1, minWidth: 160 }}
        >
          <option value="">{t('operational.balance.station')}…</option>
          {stations.map((s) => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>
        <input
          type="date" value={date} onChange={(e) => setDate(e.target.value)}
          className="form-control"
        />
      </div>

      {!stationId && (
        <p style={{ color: 'var(--text-muted)', textAlign: 'center', paddingTop: 32 }}>
          {t('operational.balance.station')} …
        </p>
      )}
      {isLoading && <p style={{ padding: 24 }}>…</p>}
      {error && <p style={{ color: 'var(--danger)' }}>{error.message}</p>}

      {data && (
        <div style={{ background: 'var(--card-bg)', border: '1px solid var(--border)', borderRadius: 14, padding: '8px 20px' }}>
          {/* Threshold warning banner */}
          {data.thresholdExceeded && (
            <div style={{ background: 'var(--operational-amber-subtle)', border: '1px solid var(--operational-amber)', borderRadius: 8, padding: '10px 14px', marginBottom: 12, color: 'var(--operational-amber)', display: 'flex', gap: 8, alignItems: 'center' }}>
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
          <div style={{ padding: '10px 0', borderBottom: '1px solid var(--chart-grid)', display: 'flex', justifyContent: 'space-between' }}>
            <span style={{ color: 'var(--text-muted)' }}>{t('operational.balance.unexplained')} %</span>
            <strong style={{ color: data.thresholdExceeded ? 'var(--operational-amber)' : 'var(--text)' }}>
              {pct(data.unexplainedDiffPct)}
            </strong>
          </div>

          {/* Solar share */}
          {data.solarSharePct != null && (
            <div style={{ padding: '10px 0', display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--text-muted)' }}>{t('operational.balance.solarShare')}</span>
              <strong style={{ color: 'var(--operational-amber)' }}>{pct(data.solarSharePct)}</strong>
            </div>
          )}
        </div>
      )}
    </section>
  );
}
