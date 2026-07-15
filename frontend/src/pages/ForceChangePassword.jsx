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
    <div style={{
      position: 'fixed',
      inset: 0,
      minHeight: '100dvh',
      width: '100vw',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'var(--bg, #0f1117)',
      padding: 20,
      boxSizing: 'border-box',
      zIndex: 1,
    }}>
      <div style={{
        width: '100%',
        maxWidth: 440,
        background: 'var(--card-bg, #1a1d27)',
        border: '1px solid var(--border, rgba(255,255,255,0.08))',
        borderRadius: 16,
        padding: '34px 32px',
        boxShadow: '0 8px 40px rgba(0,0,0,0.35)',
      }}>
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 18 }}>
          <div style={{
            width: 58,
            height: 58,
            borderRadius: 16,
            background: 'rgba(59, 130, 246, 0.12)',
            color: 'var(--primary)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}>
            <Lock size={26} />
          </div>
        </div>

        <h1 style={{ textAlign: 'center', color: 'var(--text)', fontSize: 22, margin: '0 0 8px' }}>
          {t('auth.passwordChangeRequired')}
        </h1>
        <p style={{ textAlign: 'center', color: 'var(--text-muted)', fontSize: 13, margin: '0 0 24px', lineHeight: 1.7 }}>
          {t('auth.passwordChangeDescription')}
        </p>

        {error && <div className="alert alert-danger" style={{ marginBottom: 16 }}>{error}</div>}

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

          <button className="btn btn-primary" type="submit" disabled={loading} style={{ width: '100%', marginTop: 8, height: 44 }}>
            {loading ? t('common.saving') : t('auth.changePasswordAndContinue')}
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
