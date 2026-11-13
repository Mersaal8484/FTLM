import React from 'react';
import { ClipboardCheck, Gauge, History, Zap, ArrowRightLeft, FileText, BarChart3 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

export const operationalActions = [
  { key: 'capture', icon: Zap, route: '/operations/capture' },
  { key: 'review', icon: ClipboardCheck, route: '/operations/review' },
  { key: 'history', icon: History, route: '/operations/history' },
  { key: 'refill', icon: ArrowRightLeft, route: '/operations/tanks/refill' },
  { key: 'dailyLog', icon: FileText, route: '/operations/daily-log' },
  { key: 'energyBalance', icon: BarChart3, route: '/energy-balance' },
];

export default function OperationsHub() {
  const { t } = useTranslation();

  return (
    <section className="operational-shell" aria-labelledby="operational-title">
      <div className="operational-eyebrow"><Gauge size={16} /> {t('operational.eyebrow')}</div>
      <h1 id="operational-title">{t('operational.title')}</h1>
      <p className="operational-intro">{t('operational.description')}</p>
      <div className="operational-status" role="status">
        <span className="operational-status-dot" aria-hidden="true" />
        {t('operational.foundationStatus')}
      </div>
      <div className="operational-action-grid">
        {operationalActions.map(({ key, icon: Icon, route }) => (
          <article className="operational-action-card" key={key}>
            <div className="operational-action-icon"><Icon size={22} /></div>
            <h2>{t(`operational.actions.${key}.title`)}</h2>
            <p>{t(`operational.actions.${key}.description`)}</p>
            {route ? <Link to={route}>{t('operational.actions.open')}</Link> : <span>{t('operational.actions.comingNext')}</span>}
          </article>
        ))}
      </div>
    </section>
  );
}
