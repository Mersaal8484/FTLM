import { ClipboardCheck, Gauge, History, Zap } from 'lucide-react';
import { useTranslation } from 'react-i18next';

const actions = [
  { key: 'capture', icon: Zap },
  { key: 'review', icon: ClipboardCheck },
  { key: 'history', icon: History },
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
        {actions.map(({ key, icon: Icon }) => (
          <article className="operational-action-card" key={key}>
            <div className="operational-action-icon"><Icon size={22} /></div>
            <h2>{t(`operational.actions.${key}.title`)}</h2>
            <p>{t(`operational.actions.${key}.description`)}</p>
            <span>{t('operational.comingNext')}</span>
          </article>
        ))}
      </div>
    </section>
  );
}
