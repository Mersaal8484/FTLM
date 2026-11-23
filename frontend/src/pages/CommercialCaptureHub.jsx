import { useEffect, useMemo, useState } from 'react';
import { Activity, AlertCircle, Droplets, Gauge, Plus, Sun, Zap } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import usePermissions from '../permissions/usePermissions';

const isSolarMeter = (meter) => meter.meterTypeCode === 'SolarProductionMeter' || meter.meterTypeCode === 'SolarMeter';

function GeneratorCard({ source, details }) {
  const { t } = useTranslation();
  const { can } = usePermissions();
  const generatorName = source?.name || details?.name || '';
  const generatorCode = source?.code || source?.model || details?.serialNumber || '';
  const oilLow = details?.assignedOilTypes?.some((oil) => oil.currentStock <= oil.minStock);

  return <article className="gen-card">
    <div className="gen-card-header">
      <div>
        <div className="gen-name">{generatorName}</div>
        {generatorCode && <div className="gen-model">{generatorCode}</div>}
      </div>
      {details?.status && <span className="badge badge-muted">{details.status}</span>}
    </div>
    {details && <div className="gen-stats">
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.lastPower')}</div><div className="gen-stat-value">{details.lastPowerRecord ?? '—'}</div></div>
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.lastHours')}</div><div className="gen-stat-value">{details.lastHoursRecord ?? '—'}</div></div>
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.capacity')}</div><div className="gen-stat-value">{details.actualPowerKW ?? '—'}</div></div>
      <div className="gen-stat-item"><div className="gen-stat-label">{t('operational.commercialCapture.oilType')}</div><div className="gen-stat-value">{details.assignedOilTypes?.map((oil) => oil.name).join(', ') || '—'}</div></div>
    </div>}
    {oilLow && <div className="form-error"><AlertCircle size={15} />{t('operational.commercialCapture.lowOil')}</div>}
    <div className="gen-card-actions">
      <Link to={`/operations/capture?kind=generator&generatorId=${source.id}`} className="btn btn-primary btn-sm"><Activity size={14} />{t('operational.commercialCapture.recordGenerator')}</Link>
      {can('oil', 'create') && <Link to={`/generators/${source.id}/add-oil`} className="btn btn-sm"><Droplets size={14} />{t('operational.commercialCapture.addOil')}</Link>}
    </div>
  </article>;
}

export default function CommercialCaptureHub() {
  const { t } = useTranslation();
  const { isCommercial, isCommercialMode } = useCommercialMode();
  const [site, setSite] = useState(null);
  const [generatorDetails, setGeneratorDetails] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!(isCommercial || isCommercialMode)) return undefined;
    let active = true;
    const load = async () => {
      setLoading(true);
      setError(false);
      try {
        const references = await client.get('/api/operational-captures/references');
        let details = [];
        try {
          const generators = await client.get('/api/generators/my');
          details = Array.isArray(generators.data) ? generators.data : [];
        } catch {
          details = [];
        }
        if (active) {
          setSite((references.data || [])[0] || null);
          setGeneratorDetails(details);
        }
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
  if (!(isCommercial || isCommercialMode)) return null;
  if (loading) return <div className="spinner-wrap"><div className="ft-spinner" /></div>;
  if (error) return <section className="capture-page"><p className="capture-empty">{t('operational.commercialCapture.loadFailed')}</p></section>;

  const generators = site?.generators || [];
  const feeders = site?.feeders || [];
  const tanks = site?.tanks || [];
  const solarMeters = (site?.meters || []).filter(isSolarMeter);

  return <section className="capture-page">
    <div className="page-header">
      <div>
        <h1><Zap size={22} /> {t('operational.commercialCapture.title')}</h1>
        <p>{t('operational.commercialCapture.subtitle')}</p>
      </div>
    </div>

    <section>
      <h2 className="operational-section-title"><Gauge size={18} />{t('operational.commercialCapture.feedersTitle')}</h2>
      {(feeders.length > 0 || solarMeters.length > 0) ? <article className="section-card">
        <div><strong>{t('operational.commercialCapture.feedersTitle')}</strong><p>{t('operational.commercialCapture.feedersSummary', { feeders: feeders.length, solar: solarMeters.length })}</p></div>
        <Link to="/operations/capture?kind=feeders" className="btn btn-primary"><Activity size={16} />{t('operational.commercialCapture.recordFeeders')}</Link>
      </article> : <p className="capture-empty">{t('operational.commercialCapture.noFeeders')} <Link to="/meters">{t('operational.commercialCapture.configureMeters')}</Link></p>}
    </section>

    <section>
      <h2 className="operational-section-title"><Zap size={18} />{t('operational.commercialCapture.generatorsTitle')}</h2>
      {generators.length > 0 ? <div className="grid-auto">{generators.map((generator) => <GeneratorCard key={generator.id} source={generator} details={detailsById.get(generator.id)} />)}</div> : <p className="capture-empty">{t('operational.commercialCapture.noGenerators')} <Link to="/generators">{t('operational.commercialCapture.configureGenerators')}</Link></p>}
    </section>

    <section>
      <h2 className="operational-section-title"><Droplets size={18} />{t('operational.commercialCapture.tanksTitle')}</h2>
      {tanks.length > 0 ? <article className="section-card">
        <div><strong>{t('operational.commercialCapture.tanksTitle')}</strong><p>{t('operational.commercialCapture.tanksSummary', { count: tanks.length })}</p></div>
        <Link to="/operations/capture?kind=tank" className="btn btn-primary"><Plus size={16} />{t('operational.commercialCapture.recordTanks')}</Link>
      </article> : <p className="capture-empty">{t('operational.commercialCapture.noTanks')} <Link to="/tanks">{t('operational.commercialCapture.configureTanks')}</Link></p>}
    </section>
  </section>;
}
