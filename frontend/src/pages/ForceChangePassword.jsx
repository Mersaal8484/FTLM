import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Lock, LogOut } from 'lucide-react';
import client, { refreshSessionUser } from '../api/client';

function ForceChangePassword({ onPasswordChanged, onLogout }) {
  const { t } = useTranslation();
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const setField = (key, value) => setForm((prev) => ({ ...prev, [key]: value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (!form.currentPassword || !form.newPassword || !form.confirmPassword) {
      setError(t('auth.fillAllFields'));
      return;
    }

    if (form.newPassword !== form.confirmPassword) {
      setError(t('auth.passwordsDoNotMatch'));
      return;
    }

    setLoading(true);
    try {
      await client.post('/api/auth/change-password', {
        currentPassword: form.currentPassword,
        newPassword: form.newPassword,
      });
      const user = await refreshSessionUser();
      onPasswordChanged?.(user);
      navigate('/', { replace: true });
    } catch (err) {
      setError(err.response?.data?.msg || t('auth.unableToChangePassword'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-card" style={{ maxWidth: 440 }}>
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 18 }}>
          <div style={{
            width: 56,
            height: 56,
            borderRadius: 16,
            background: 'rgba(59, 130, 246, 0.15)',
            color: 'var(--primary)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}>
            <Lock size={26} />
          </div>
        </div>

        <h1 className="login-title" style={{ fontSize: '1.4rem' }}>
          {t('auth.passwordChangeRequired')}
        </h1>
        <p className="login-desc" style={{ marginBottom: 20 }}>
          {t('auth.passwordChangeDescription')}
        </p>

        {error && <div className="alert-inline alert-inline-danger" style={{ marginBottom: 16 }}>{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label">{t('common.currentPassword')}</label>
            <input
              className="form-control"
              type="password"
              value={form.currentPassword}
              onChange={(e) => setField('currentPassword', e.target.value)}
              autoComplete="current-password"
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label">{t('common.newPassword')}</label>
            <input
              className="form-control"
              type="password"
              value={form.newPassword}
              onChange={(e) => setField('newPassword', e.target.value)}
              autoComplete="new-password"
              minLength={7}
              required
            />
            <div style={{ color: 'var(--text-muted)', fontSize: 12, marginTop: 6 }}>
              {t('auth.passwordHint')}
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">{t('common.confirmNewPassword')}</label>
            <input
              className="form-control"
              type="password"
              value={form.confirmPassword}
              onChange={(e) => setField('confirmPassword', e.target.value)}
              autoComplete="new-password"
              minLength={7}
              required
            />
          </div>

          <button className="btn btn-primary login-btn" type="submit" disabled={loading} style={{ width: '100%', marginTop: 12 }}>
            {loading ? (
              <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                <span className="ft-spinner" style={{ width: 16, height: 16 }} />
                {t('common.saving')}
              </span>
            ) : (
              t('auth.changePasswordAndContinue')
            )}
          </button>
        </form>

        <button
          type="button"
          className="btn btn-ghost"
          onClick={onLogout}
          style={{ width: '100%', marginTop: 12, justifyContent: 'center' }}
        >
          <LogOut size={15} />
          {t('common.logout')}
        </button>
      </div>
    </div>
  );
}

export default ForceChangePassword;
