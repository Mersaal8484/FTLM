import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, CheckCircle, KeyRound } from 'lucide-react';
import client from '../api/client';
import useTenantBranding from '../hooks/useTenantBranding';

function ForgotPassword() {
  const branding = useTenantBranding();
  const { t, i18n } = useTranslation();
  const ar = i18n.language === 'ar';
  const navigate = useNavigate();

  const [step, setStep] = useState('request'); // request | reset | done
  const [email, setEmail] = useState('');
  const [token, setToken] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  const handleRequest = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const res = await client.post('/api/auth/forgot-password', { email });
      setSuccess(res.data.message);
      setStep('reset');
    } catch (err) {
      setError(err.response?.data?.msg || t('auth.forgotPasswordError'));
    } finally {
      setLoading(false);
    }
  };

  const handleReset = async (e) => {
    e.preventDefault();
    setError('');

    if (!token || !newPassword || !confirmPassword) {
      setError(t('auth.fillAllFields'));
      return;
    }
    if (newPassword !== confirmPassword) {
      setError(t('auth.passwordsDoNotMatch'));
      return;
    }

    setLoading(true);
    try {
      await client.post('/api/auth/reset-password', {
        email,
        token,
        newPassword,
      });
      setStep('done');
    } catch (err) {
      setError(err.response?.data?.msg || t('auth.resetPasswordError'));
    } finally {
      setLoading(false);
    }
  };

  const inputStyle = {
    width: '100%',
    padding: '12px 14px',
    fontSize: 14,
    background: 'var(--input-bg, rgba(255,255,255,0.05))',
    color: 'var(--text)',
    border: '1px solid var(--border, rgba(255,255,255,0.1))',
    borderRadius: 10,
    outline: 'none',
    boxSizing: 'border-box',
    transition: 'border-color 0.2s',
  };

  const labelStyle = {
    display: 'block',
    fontSize: 13,
    fontWeight: 500,
    color: 'var(--text-muted)',
    marginBottom: 6,
    textAlign: ar ? 'right' : 'left',
  };

  const cardStyle = {
    width: '100%',
    maxWidth: 420,
    background: 'var(--card-bg, #1a1d27)',
    borderRadius: 16,
    padding: '40px 36px',
    boxShadow: '0 8px 40px rgba(0,0,0,0.4)',
    border: '1px solid var(--border, rgba(255,255,255,0.08))',
  };

  return (
    <div className="login-page">
      <div className="login-card" style={{ maxWidth: 440 }}>
        {/* Logo */}
        {branding.logoUrl && <div className="login-logo">
          <img
            src={branding.logoUrl}
            alt={branding.name}
            onError={e => { e.target.style.display = 'none'; }}
          />
        </div>}

        {/* Icon */}
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 16 }}>
          <div style={{
            width: 52,
            height: 52,
            borderRadius: 14,
            background: step === 'done'
              ? 'rgba(16, 185, 129, 0.15)'
              : 'rgba(245, 158, 11, 0.15)',
            color: step === 'done' ? 'var(--success)' : 'var(--warning)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}>
            {step === 'done' ? <CheckCircle size={26} /> : <KeyRound size={26} />}
          </div>
        </div>

        {/* Title */}
        <h1 className="login-title" style={{ fontSize: '1.4rem' }}>
          {step === 'done' ? t('auth.resetPasswordSuccess') : t('auth.forgotPasswordTitle')}
        </h1>
        <p className="login-desc" style={{ marginBottom: 20 }}>
          {step === 'request' && t('auth.forgotPasswordDesc')}
          {step === 'reset' && t('auth.forgotPasswordResetDesc')}
          {step === 'done' && t('auth.forgotPasswordDoneDesc')}
        </p>

        {error && (
          <div className="alert-inline alert-inline-danger" style={{ marginBottom: 16 }}>
            {error}
          </div>
        )}

        {success && step === 'reset' && (
          <div className="alert-inline alert-inline-success" style={{ marginBottom: 16 }}>
            {success}
          </div>
        )}

        {/* Step: Request reset */}
        {step === 'request' && (
          <form onSubmit={handleRequest}>
            <div className="form-group" style={{ marginBottom: 20 }}>
              <label className="form-label">{t('common.email')}</label>
              <input
                className="form-control"
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                placeholder={t('auth.emailPlaceholder')}
                required
                autoFocus
              />
            </div>
            <button
              type="submit"
              disabled={loading}
              className="btn btn-primary login-btn"
            >
              {loading ? (
                <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                  <span className="ft-spinner" style={{ width: 16, height: 16 }} />
                  {t('common.sending')}
                </span>
              ) : (
                t('auth.sendResetCode')
              )}
            </button>
          </form>
        )}

        {/* Step: Reset password */}
        {step === 'reset' && (
          <form onSubmit={handleReset}>
            <div className="form-group" style={{ marginBottom: 16 }}>
              <label className="form-label">{t('auth.resetCode')}</label>
              <input
                className="form-control"
                type="text"
                value={token}
                onChange={e => setToken(e.target.value)}
                placeholder={t('auth.resetCodePlaceholder')}
                required
                autoFocus
              />
            </div>
            <div className="form-group" style={{ marginBottom: 16 }}>
              <label className="form-label">{t('common.newPassword')}</label>
              <input
                className="form-control"
                type="password"
                value={newPassword}
                onChange={e => setNewPassword(e.target.value)}
                placeholder="••••••••"
                minLength={7}
                required
              />
            </div>
            <div className="form-group" style={{ marginBottom: 20 }}>
              <label className="form-label">{t('common.confirmNewPassword')}</label>
              <input
                className="form-control"
                type="password"
                value={confirmPassword}
                onChange={e => setConfirmPassword(e.target.value)}
                placeholder="••••••••"
                minLength={7}
                required
              />
            </div>
            <button
              type="submit"
              disabled={loading}
              className="btn btn-primary login-btn"
            >
              {loading ? (
                <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                  <span className="ft-spinner" style={{ width: 16, height: 16 }} />
                  {t('common.saving')}
                </span>
              ) : (
                t('auth.resetPassword')
              )}
            </button>
          </form>
        )}

        {/* Step: Done */}
        {step === 'done' && (
          <button
            onClick={() => navigate('/login')}
            className="btn btn-primary login-btn"
          >
            {t('common.login')}
          </button>
        )}

        {/* Back to login */}
        <div style={{ textAlign: 'center', marginTop: 20 }}>
          <Link
            to="/login"
            style={{
              color: 'var(--text-muted)',
              fontSize: 13,
              textDecoration: 'none',
              display: 'inline-flex',
              alignItems: 'center',
              gap: 4,
            }}
          >
            <ArrowLeft size={14} style={{ transform: ar ? 'rotate(180deg)' : 'none' }} />
            {t('auth.backToLogin')}
          </Link>
        </div>
      </div>
    </div>
  );
}

export default ForgotPassword;
