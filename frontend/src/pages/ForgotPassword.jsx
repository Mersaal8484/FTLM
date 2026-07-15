import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, CheckCircle, KeyRound } from 'lucide-react';
import client from '../api/client';

function ForgotPassword() {
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
    <div style={{
      minHeight: '100vh',
      width: '100vw',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'var(--bg, #0f1117)',
      margin: 0,
      padding: 20,
      boxSizing: 'border-box',
      position: 'fixed',
      top: 0,
      left: 0,
    }}>
      <div style={cardStyle}>
        {/* Logo */}
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 20 }}>
          <img
            src="/pec-logo.png"
            alt="PEC"
            onError={e => { e.target.style.display = 'none'; }}
            style={{ width: 80, height: 80, objectFit: 'contain' }}
          />
        </div>

        {/* Icon */}
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 16 }}>
          <div style={{
            width: 52,
            height: 52,
            borderRadius: 14,
            background: step === 'done'
              ? 'rgba(34, 197, 94, 0.12)'
              : 'rgba(245, 158, 11, 0.12)',
            color: step === 'done' ? '#22c55e' : '#f59e0b',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}>
            {step === 'done' ? <CheckCircle size={24} /> : <KeyRound size={24} />}
          </div>
        </div>

        {/* Title */}
        <h1 style={{
          textAlign: 'center', fontSize: 22, fontWeight: 700,
          color: 'var(--text, #fff)', margin: '0 0 8px',
        }}>
          {step === 'done' ? t('auth.resetPasswordSuccess') : t('auth.forgotPasswordTitle')}
        </h1>
        <p style={{
          textAlign: 'center', fontSize: 13,
          color: 'var(--text-muted, rgba(255,255,255,0.5))',
          margin: '0 0 24px',
          lineHeight: 1.6,
        }}>
          {step === 'request' && t('auth.forgotPasswordDesc')}
          {step === 'reset' && t('auth.forgotPasswordResetDesc')}
          {step === 'done' && t('auth.forgotPasswordDoneDesc')}
        </p>

        {error && (
          <div style={{
            padding: '10px 14px',
            borderRadius: 8,
            background: 'rgba(239, 68, 68, 0.1)',
            border: '1px solid rgba(239, 68, 68, 0.2)',
            color: '#ef4444',
            fontSize: 13,
            marginBottom: 16,
            textAlign: 'center',
          }}>
            {error}
          </div>
        )}

        {success && step === 'reset' && (
          <div style={{
            padding: '10px 14px',
            borderRadius: 8,
            background: 'rgba(34, 197, 94, 0.1)',
            border: '1px solid rgba(34, 197, 94, 0.2)',
            color: '#22c55e',
            fontSize: 13,
            marginBottom: 16,
            textAlign: 'center',
          }}>
            {success}
          </div>
        )}

        {/* Step: Request reset */}
        {step === 'request' && (
          <form onSubmit={handleRequest}>
            <div style={{ marginBottom: 20 }}>
              <label style={labelStyle}>{t('common.email')}</label>
              <input
                style={inputStyle}
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
              style={{
                width: '100%', padding: '12px', fontSize: 15, fontWeight: 600,
                background: 'var(--primary, #3b82f6)',
                color: '#fff',
                border: 'none',
                borderRadius: 10,
                cursor: loading ? 'wait' : 'pointer',
                opacity: loading ? 0.7 : 1,
              }}
            >
              {loading ? t('common.sending') : t('auth.sendResetCode')}
            </button>
          </form>
        )}

        {/* Step: Reset password */}
        {step === 'reset' && (
          <form onSubmit={handleReset}>
            <div style={{ marginBottom: 16 }}>
              <label style={labelStyle}>{t('auth.resetCode')}</label>
              <input
                style={inputStyle}
                type="text"
                value={token}
                onChange={e => setToken(e.target.value)}
                placeholder={t('auth.resetCodePlaceholder')}
                required
                autoFocus
              />
            </div>
            <div style={{ marginBottom: 16 }}>
              <label style={labelStyle}>{t('common.newPassword')}</label>
              <input
                style={inputStyle}
                type="password"
                value={newPassword}
                onChange={e => setNewPassword(e.target.value)}
                placeholder="••••••••"
                minLength={7}
                required
              />
            </div>
            <div style={{ marginBottom: 20 }}>
              <label style={labelStyle}>{t('common.confirmNewPassword')}</label>
              <input
                style={inputStyle}
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
              style={{
                width: '100%', padding: '12px', fontSize: 15, fontWeight: 600,
                background: 'var(--primary, #3b82f6)',
                color: '#fff',
                border: 'none',
                borderRadius: 10,
                cursor: loading ? 'wait' : 'pointer',
                opacity: loading ? 0.7 : 1,
              }}
            >
              {loading ? t('common.saving') : t('auth.resetPassword')}
            </button>
          </form>
        )}

        {/* Step: Done */}
        {step === 'done' && (
          <button
            onClick={() => navigate('/login')}
            style={{
              width: '100%', padding: '12px', fontSize: 15, fontWeight: 600,
              background: 'var(--primary, #3b82f6)',
              color: '#fff',
              border: 'none',
              borderRadius: 10,
              cursor: 'pointer',
            }}
          >
            {t('common.login')}
          </button>
        )}

        {/* Back to login */}
        <div style={{ textAlign: 'center', marginTop: 20 }}>
          <Link
            to="/login"
            style={{
              color: 'var(--text-muted, rgba(255,255,255,0.5))',
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
