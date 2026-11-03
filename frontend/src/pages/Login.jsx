import React, { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import client from '../api/client';
import { Eye, EyeOff } from 'lucide-react';
import useTenantBranding from '../hooks/useTenantBranding';

function Login({ onLogin }) {
  const { t } = useTranslation();
  const branding = useTenantBranding();
  const [form, setForm] = useState({ email: '', password: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [showPass, setShowPass] = useState(false);
  const [challenge, setChallenge] = useState(null);
  const [code, setCode] = useState('');
  const navigate = useNavigate();
  const location = useLocation();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      if (challenge) {
        await client.post('/api/auth/login/2fa', { tempToken: challenge, code });
      } else {
        const response = await client.post('/api/auth/login', { userName: form.email, password: form.password });
        if (response.data?.requiresTwoFactor && response.data?.tempToken) {
          setChallenge(response.data.tempToken);
          setForm(previous => ({ ...previous, password: '' }));
          return;
        }
      }
      
      const meRes = await client.get('/api/auth/me');
      const user = meRes.data;
      onLogin?.(user);
      const from = location.state?.from || '/';
      navigate(user?.mustChangePassword ? '/force-password-change' : from, { replace: true });
    } catch (err) {
      const msg = err.response?.data?.msg;
      setError(challenge ? t('auth.twoFactorRetry') : (msg || t('auth.invalidCredentials')));
      if (challenge) { setChallenge(null); setCode(''); }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-card">
        {branding.logoUrl && <div className="login-logo">
          <img
            src={branding.logoUrl}
            alt={branding.name}
            onError={e => { e.target.style.display = 'none'; }}
          />
        </div>}

        <h1 className="login-title">
          {branding.name}
        </h1>
        <p className="login-desc">
          {t('common.systemName')}
        </p>

        {error && (
          <div className="alert-inline alert-inline-danger" style={{ marginBottom: 20 }}>{error}</div>
        )}

        <form onSubmit={handleSubmit}>
          {!challenge && <><div className="form-group" style={{ marginBottom: 16 }}>
            <label className="form-label">
              {t('common.email')}
            </label>
            <input
              className="form-control"
              type="email"
              placeholder={t('auth.emailPlaceholder')}
              value={form.email}
              onChange={e => setForm({ ...form, email: e.target.value })}
              required
            />
          </div>

          <div className="form-group" style={{ marginBottom: 24 }}>
            <label className="form-label">
              {t('common.password')}
            </label>
            <div style={{ position: 'relative' }}>
              <input
                className="form-control"
                type={showPass ? 'text' : 'password'}
                placeholder="••••••••"
                value={form.password}
                onChange={e => setForm({ ...form, password: e.target.value })}
                style={{ paddingInlineEnd: '42px' }}
                required
              />
              <button
                type="button"
                className="password-toggle-btn"
                onClick={() => setShowPass(v => !v)}
                aria-label={showPass ? 'Hide password' : 'Show password'}
              >
                {showPass ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>

          <div className="login-forgot">
            <Link to="/forgot-password">
              {t('auth.forgotPassword')}
            </Link>
          </div>
          </>}
          {challenge && <div className="form-group" style={{ marginBottom: 24 }}>
            <label className="form-label" htmlFor="two-factor-code">{t('auth.twoFactorCode')}</label>
            <input id="two-factor-code" className="form-control" autoComplete="one-time-code" pattern="([0-9]{6}|[0-9a-fA-F]{32})" maxLength={32} value={code} onChange={e => setCode(e.target.value)} required />
          </div>}

          <button
            className="btn btn-primary login-btn"
            type="submit"
            disabled={loading}
          >
            {loading ? (
              <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                <span className="ft-spinner" style={{ width: 16, height: 16 }} />
                {t('common.loadingLogin')}
              </span>
            ) : (
              t('common.login')
            )}
          </button>
        </form>

        <p className="login-footer">
          {t('auth.footerCredits')}
        </p>
      </div>
    </div>
  );
}

export default Login;
