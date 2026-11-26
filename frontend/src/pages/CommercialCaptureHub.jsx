import { useEffect, useMemo, useState } from 'react';
import { Activity, AlertCircle, Droplets, Gauge, Zap } from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import usePermissions from '../permissions/usePermissions';

const isSolarMeter = (meter) => meter.meterTypeCode === 'SolarProductionMeter' || meter.meterTypeCode === 'SolarMeter';
const isDailyTank = (tank) => tank.tankRole === 2 || tank.tankRole === 'Daily';

function GeneratorCard({ source, details, dailyTank, prevEnergy, prevHours, mode }) {
  const { t } = useTranslation();
  const { can } = usePermissions();
  const generatorName = source?.name || details?.name || '';
  const generatorCode = source?.code || source?.model || details?.serialNumber || '';
  const oilLow = details?.assignedOilTypes?.some((oil) => oil.currentStock <= oil.minStock);
  const isDaily = mode === 'daily';
  const statusClass = details?.status === 'Running'
    ? 'badge-success'
    : details?.status === 'Idle'
      ? 'badge-warning'
      : details?.status === 'Maintenance'
        ? 'badge-danger'
        : 'badge-muted';
  const statusLabel = details?.status === 'Running'
    ? t('operational.commercialCapture.statusRunning')
    : details?.status === 'Idle'
      ? t('operational.commercialCapture.statusIdle')
      : details?.status === 'Maintenance'
        ? t('operational.commercialCapture.statusMaintenance')
        : details?.status === 'Offline'
          ? t('operational.commercialCapture.statusOffline')
          : details?.status;

  return <article className="gen-card">
    <div className="gen-card-header">
      <div>
        <div className="gen-name">{generatorName}</div>
        {generatorCode && <div className="gen-model">{generatorCode}</div>}
      </div>
      {details?.status && <span className={`badge ${statusClass}`}>{statusLabel}</span>}
    </div>
    {isDaily && <div className="gen-stats">
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.dailyTank')}</div><div className="gen-stat-value">{dailyTank?.name || t('operational.commercialCapture.noDailyTank')}</div></div>
      {dailyTank?.actualStockLiters != null && <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.currentTankStock')}</div><div className="gen-stat-value">{dailyTank.actualStockLiters}</div></div>}
    </div>}
    {!isDaily && (details || prevEnergy || prevHours) && <div className="gen-stats">
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.lastPower')}</div><div className="gen-stat-value">{prevEnergy?.rawValue != null ? Number(prevEnergy.rawValue).toLocaleString('en-US') : '—'}</div></div>
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.lastHours')}</div><div className="gen-stat-value">{prevHours?.rawValue != null ? Number(prevHours.rawValue).toLocaleString('en-US') : '—'}</div></div>
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.capacity')}</div><div className="gen-stat-value">{details?.actualPowerKW ?? '—'}</div></div>
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.oilType')}</div><div className="gen-stat-value">{details?.assignedOilTypes?.map((oil) => oil.name).join(', ') || '—'}</div></div>
    </div>}
    {oilLow && <div className="form-error"><AlertCircle size={15} />{t('operational.commercialCapture.lowOil')}</div>}
    <div className="gen-card-actions">
      {!isDaily ? (
        <Link to={`/operations/capture?kind=hourly-generator&generatorId=${source.id}`} className="btn btn-primary btn-sm">
          <Activity size={14} />{t('operational.commercialCapture.recordHourly')}
        </Link>
      ) : dailyTank ? (
        <>
          <Link to={`/operations/capture?kind=daily-generator&generatorId=${source.id}&tankId=${dailyTank.id}`} className="btn btn-primary btn-sm">
            <Activity size={14} />{t('operational.commercialCapture.recordDailyClosing')}
          </Link>
          {can('oil', 'create') && (
            <Link to={`/generators/${source.id}/add-oil`} className="btn btn-sm">
              <Droplets size={14} />{t('operational.commercialCapture.addOil')}
            </Link>
          )}
        </>
      ) : (
        <>
          <button type="button" className="btn btn-primary btn-sm" disabled aria-disabled="true">
            <Activity size={14} />{t('operational.commercialCapture.recordDailyClosing')}
          </button>
          <Link to="/commercial/daily-tanks" className="btn btn-sm">
            <Droplets size={14} />{t('operational.commercialCapture.configureDailyTank')}
          </Link>
        </>
      )}
    </div>
  </article>;
}

export default function CommercialCaptureHub() {
  const { t } = useTranslation();
  const { isCommercial, isCommercialMode } = useCommercialMode();
  const [site, setSite] = useState(null);
  const [generatorDetails, setGeneratorDetails] = useState([]);
  const [fuelTanks, setFuelTanks] = useState([]);
  const [previousReadings, setPreviousReadings] = useState([]);
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = searchParams.get('tab') === 'daily' ? 'daily' : 'hourly';
  const setActiveTab = (tab) => setSearchParams({ tab: tab === 'daily' ? 'daily' : 'hourly' }, { replace: true });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!(isCommercial || isCommercialMode)) return undefined;
    let active = true;
    const load = async () => {
      setLoading(true);
      setError(false);
      try {
        const [references, generatorResponse, tankResponse, prevResponse] = await Promise.all([
          client.get('/api/operational-captures/references'),
          client.get('/api/generators/my').catch(() => ({ data: [] })),
          client.get('/api/fuel-tanks').catch(() => ({ data: [] })),
          client.get('/api/operational-captures/previous-readings').catch(() => ({ data: { readings: [] } })),
        ]);
        if (!active) return;
        const tankData = tankResponse.data;
        setSite((references.data || [])[0] || null);
        setGeneratorDetails(Array.isArray(generatorResponse.data) ? generatorResponse.data : []);
        setFuelTanks(Array.isArray(tankData) ? tankData : (tankData?.items || []));
        setPreviousReadings(Array.isArray(prevResponse.data?.readings) ? prevResponse.data.readings : []);
      } catch {
        if (active) setError(true);
      } finally {
        if (active) setLoading(false);
      }
    };
    load();
    return () => { active = false; };
  }, [isCommercial, isCommercialMode]);

  const detailsById = useMemo(() => new Map(generatorDetails.map((generator) => [generator.id, generator])), [generatorDetails]);
  const prevReading = (generatorId, numericType, namedType) =>
    previousReadings.find((r) => r.subjectId === generatorId && (r.subjectType === numericType || r.subjectType === namedType))?.latestAny ?? null;
  const dailyTankByGeneratorId = useMemo(() => {
    const map = new Map();
    for (const tank of fuelTanks) {
      if (isDailyTank(tank) && tank.generatorId != null) {
        map.set(Number(tank.generatorId), tank);
      }
    }
    return map;
  }, [fuelTanks]);

  if (!(isCommercial || isCommercialMode)) return null;
  if (loading) return <div className="spinner-wrap"><div className="ft-spinner" /></div>;
  if (error) return <section className="capture-page"><p className="capture-empty">{t('operational.commercialCapture.loadFailed')}</p></section>;

  const generators = site?.generators || [];
  const feeders = site?.feeders || [];
  const solarMeters = (site?.meters || []).filter(isSolarMeter);
  const isDaily = activeTab === 'daily';

  return <section className="capture-page">
    <div className="page-header">
      <div>
        <h1><Zap size={22} /> {t('operational.commercialCapture.title')}</h1>
        <p>{t('operational.commercialCapture.subtitle')}</p>
      </div>
    </div>
    <div className="ft-tabs" aria-label={t('operational.commercialCapture.title')}>
      <button type="button" className={`ft-tab ${!isDaily ? 'active' : ''}`} onClick={() => setActiveTab('hourly')}>{t('operational.commercialCapture.hourlyTab')}</button>
      <button type="button" className={`ft-tab ${isDaily ? 'active' : ''}`} onClick={() => setActiveTab('daily')}>{t('operational.commercialCapture.dailyTab')}</button>
    </div>

    <section>
      <h2 className="operational-section-title"><Zap size={18} />{t(isDaily ? 'operational.commercialCapture.dailyGeneratorsTitle' : 'operational.commercialCapture.hourlyGeneratorsTitle')}</h2>
      {generators.length > 0
        ? <div className="grid-auto">{generators.map((generator) => <GeneratorCard key={generator.id} source={generator} details={detailsById.get(generator.id)} dailyTank={dailyTankByGeneratorId.get(Number(generator.id))} prevEnergy={prevReading(generator.id, 0, 'GeneratorEnergyProduced')} prevHours={prevReading(generator.id, 1, 'GeneratorRunningHours')} mode={activeTab} />)}</div>
        : <p className="capture-empty">{t('operational.commercialCapture.noGenerators')} <Link to="/generators">{t('operational.commercialCapture.configureGenerators')}</Link></p>}
    </section>

    {isDaily && <section>
      <h2 className="operational-section-title"><Gauge size={18} />{t('operational.commercialCapture.feedersTitle')}</h2>
      {(feeders.length > 0 || solarMeters.length > 0)
        ? <article className="section-card"><div><strong>{t('operational.commercialCapture.feedersTitle')}</strong><p>{t('operational.commercialCapture.feedersSummary', { feeders: feeders.length, solar: solarMeters.length })}</p></div><Link to="/operations/capture?kind=daily-feeders" className="btn btn-primary"><Activity size={16} />{t('operational.commercialCapture.recordDailyClosing')}</Link></article>
        : <p className="capture-empty">{t('operational.commercialCapture.noFeeders')} <Link to="/meters">{t('operational.commercialCapture.configureMeters')}</Link></p>}
    </section>}
  </section>;
}
