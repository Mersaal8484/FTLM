import { useEffect, useState } from 'react';
import { Building2, CheckCircle2, LoaderCircle } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import client from '../api/client';
import { resetCommercialModeCache } from '../hooks/useCommercialMode';

export default function CommercialStationModeSettings() {
  const { t } = useTranslation();
  const [stationName, setStationName] = useState('');
  const [stationCode, setStationCode] = useState('');
  const [status, setStatus] = useState(null);
  const [loading, setLoading] = useState(true);
  const [activating, setActivating] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    let isMounted = true;
    client.get('/api/commercial-station-mode')
      .then(({ data }) => isMounted && setStatus(data))
      .catch(() => isMounted && setError(t('commercialMode.loadFailed')))
      .finally(() => isMounted && setLoading(false));
    return () => { isMounted = false; };
  }, [t]);

  const activate = async (event) => {
    event.preventDefault();
    setError('');
    setActivating(true);
    try {
      await client.post('/api/commercial-station-mode/activate', {
        stationName: stationName.trim(),
        stationCode: stationCode.trim(),
      });
      resetCommercialModeCache();
      window.location.reload();
    } catch (requestError) {
      setError(requestError.response?.data?.error || t('commercialMode.activateFailed'));
      setActivating(false);
    }
  };

  if (loading) return <div className="page-loading">{t('common.loading')}</div>;

  if (status?.isCommercialMode) {
    return (
      <section className="settings-page">
        <div className="settings-card settings-card--success">
          <CheckCircle2 aria-hidden="true" size={28} />
          <div><h1>{t('commercialMode.activeTitle')}</h1><p>{t('commercialMode.activeDescription')}</p></div>
        </div>
      </section>
    );
  }

  return (
    <section className="settings-page">
      <form className="settings-card" onSubmit={activate}>
        <div className="settings-card__heading">
          <Building2 aria-hidden="true" size={28} />
          <div><h1>{t('commercialMode.title')}</h1><p>{t('commercialMode.description')}</p></div>
        </div>
        <label htmlFor="commercial-station-name">{t('commercialMode.stationName')}</label>
        <input id="commercial-station-name" value={stationName} onChange={(event) => setStationName(event.target.value)} maxLength={200} required />
        <label htmlFor="commercial-station-code">{t('commercialMode.stationCode')}</label>
        <input id="commercial-station-code" value={stationCode} onChange={(event) => setStationCode(event.target.value)} maxLength={100} required />
        {error && <p className="form-error" role="alert">{error}</p>}
        <button type="submit" className="btn btn-primary" disabled={activating}>
          {activating ? <LoaderCircle className="ft-spin" aria-hidden="true" size={16} /> : null}
          {activating ? t('commercialMode.activating') : t('commercialMode.activate')}
        </button>
      </form>
    </section>
  );
}
