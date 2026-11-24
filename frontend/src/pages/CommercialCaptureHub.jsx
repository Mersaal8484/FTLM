import { useEffect, useMemo, useState } from 'react';
import { Activity, AlertCircle, Droplets, Gauge, Zap } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import usePermissions from '../permissions/usePermissions';

const isSolarMeter = (meter) => meter.meterTypeCode === 'SolarProductionMeter' || meter.meterTypeCode === 'SolarMeter';
const isDailyTank = (tank) => tank.tankRole === 2 || tank.tankRole === 'Daily' || tank.generatorId != null;

function GeneratorCard({ source, details, dailyTank, mode }) {
  const { t } = useTranslation();
  const { can } = usePermissions();
  const generatorName = source?.name || details?.name || '';
  const generatorCode = source?.code || source?.model || details?.serialNumber || '';
  const oilLow = details?.assignedOilTypes?.some((oil) => oil.currentStock <= oil.minStock);
  const isDaily = mode === 'daily';

  return <article className="gen-card">
    <div className="gen-card-header">
      <div>
        <div className="gen-name">{generatorName}</div>
        {generatorCode && <div className="gen-model">{generatorCode}</div>}
      </div>
      {details?.status && <span className="badge badge-muted">{details.status}</span>}
    </div>
    {isDaily && <div className="gen-stats">
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.dailyTank')}</div><div className="gen-stat-value">{dailyTank?.name || t('operational.commercialCapture.noDailyTank')}</div></div>
      {dailyTank?.actualStockLiters != null && <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.currentTankStock')}</div><div className="gen-stat-value">{dailyTank.actualStockLiters}</div></div>}
    </div>}
    {!isDaily && details && <div className="gen-stats">
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.lastPower')}</div><div className="gen-stat-value">{details.lastPowerRecord ?? '—'}</div></div>
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.lastHours')}</div><div className="gen-stat-value">{details.lastHoursRecord ?? '—'}</div></div>
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.capacity')}</div><div className="gen-stat-value">{details.actualPowerKW ?? '—'}</div></div>
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.oilType')}</div><div className="gen-stat-value">{details.assignedOilTypes?.map((oil) => oil.name).join(', ') || '—'}</div></div>
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
        <Link to="/commercial/daily-tanks" className="btn btn-sm">
          <Droplets size={14} />{t('operational.commercialCapture.configureDailyTank')}
        </Link>
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
  const [activeTab, setActiveTab] = useState('hourly');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!(isCommercial || isCommercialMode)) return undefined;
    let active = true;
    const load = async () => {
      setLoading(true);
      setError(false);
      try {
        const [references, generatorResponse, tankResponse] = await Promise.all([
          client.get('/api/operational-captures/references'),
          client.get('/api/generators/my').catch(() => ({ data: [] })),
          client.get('/api/fuel-tanks').catch(() => ({ data: [] })),
        ]);
        if (!active) return;
        const tankData = tankResponse.data;
        setSite((references.data || [])[0] || null);
        setGeneratorDetails(Array.isArray(generatorResponse.data) ? generatorResponse.data : []);
        setFuelTanks(Array.isArray(tankData) ? tankData : (tankData?.items || []));
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
  const dailyTankByGeneratorId = useMemo(() => {
    const map = new Map();
    for (const tank of fuelTanks) {
      if (isDailyTank(tank) && tank.generatorId != null) {
        map.set(Number(tank.generatorId), tank);
      }
    }
    if (site?.tanks?.length) {
      const generators = site?.generators || [];
      for (const tank of site.tanks) {
        if (tank.generatorId != null && !map.has(Number(tank.generatorId))) {
          map.set(Number(tank.generatorId), tank);
        }
      }
      if (generators.length === 1 && site.tanks.length === 1 && !map.has(Number(generators[0].id))) {
        map.set(Number(generators[0].id), site.tanks[0]);
      }
    }
    return map;
  }, [fuelTanks, site]);

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
        ? <div className="grid-auto">{generators.map((generator) => <GeneratorCard key={generator.id} source={generator} details={detailsById.get(generator.id)} dailyTank={dailyTankByGeneratorId.get(Number(generator.id))} mode={activeTab} />)}</div>
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
