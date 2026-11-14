import { ArrowLeft, ArrowRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

/** A consistent return path from an operational action to the action hub. */
export default function OperationalBackLink() {
  const { i18n, t } = useTranslation();
  const isRtl = i18n.dir() === 'rtl';
  const Arrow = isRtl ? ArrowRight : ArrowLeft;

  return (
    <Link to="/operations" className="operational-back-link" aria-label={t('operational.title')}>
      <Arrow size={18} aria-hidden="true" />
      <span>{t('operational.title')}</span>
    </Link>
  );
}
