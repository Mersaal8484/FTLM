import React, { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import client from '../api/client';
import { Eye, EyeOff } from 'lucide-react';

function Login({ onLogin }) {
  const { t } = useTranslation();
  const [form, setForm] = useState({ email: '', password: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [showPass, setShowPass] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      const payload = { userName: form.email, password: form.password };
      await client.post('/api/auth/login', payload);
      
      const meRes = await client.get('/api/auth/me');
      const user = meRes.data;
      onLogin?.(user);
      const from = location.state?.from || '/';
      navigate(user?.mustChangePassword ? '/force-password-change' : from, { replace: true });
    } catch (err) {
      const msg = err.response?.data?.msg;
      setError(msg || t('auth.invalidCredentials'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-logo">
          <img
            src="/pec-logo.png"
            alt="PEC"
            onError={e => { e.target.style.display = 'none'; }}
          />
        </div>

        <h1 className="login-title">
          {t('common.appName')}
        </h1>
        <p className="login-sub">
          {t('common.pec')}
        </p>
        <p className="login-desc">
          {t('common.systemName')}
        </p>

        {error && (
          <div className="alert-inline alert-inline-danger" style={{ marginBottom: 20 }}>{error}</div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group" style={{ marginBottom: 16 }}>
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
