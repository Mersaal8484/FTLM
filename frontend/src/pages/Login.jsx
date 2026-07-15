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
    <div style={{
      minHeight: '100vh',
      width: '100vw',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'var(--bg, #0f1117)',
      margin: 0,
      padding: 0,
      position: 'fixed',
      top: 0,
      left: 0,
    }}>
      <div style={{
        width: '100%',
        maxWidth: 420,
        background: 'var(--card-bg, #1a1d27)',
        borderRadius: 16,
        padding: '40px 36px',
        boxShadow: '0 8px 40px rgba(0,0,0,0.4)',
        border: '1px solid var(--border, rgba(255,255,255,0.08))',
      }}>
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 20 }}>
          <img
            src="/pec-logo.png"
            alt="PEC"
            onError={e => { e.target.style.display = 'none'; }}
            style={{ width: 100, height: 100, objectFit: 'contain' }}
          />
        </div>

        <h1 style={{
          textAlign: 'center', fontSize: 24, fontWeight: 700,
          color: 'var(--text, #fff)', margin: '0 0 6px',
        }}>
          {t('common.appName')}
        </h1>
        <p style={{
          textAlign: 'center', fontSize: 13,
          color: 'var(--text-muted, rgba(255,255,255,0.5))',
          margin: '0 0 8px',
        }}>
          {t('common.pec')}
        </p>
        <p style={{
          textAlign: 'center', fontSize: 12,
          color: 'var(--text-faint, rgba(255,255,255,0.3))',
          margin: '0 0 28px',
        }}>
          {t('common.systemName')}
        </p>

        {error && (
          <div className="alert alert-danger" style={{ marginBottom: 20 }}>{error}</div>
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
                style={{ paddingRight: '42px' }}
                required
              />
              <button
                type="button"
                onClick={() => setShowPass(v => !v)}
                style={{
                  position: 'absolute', right: 12, top: '50%',
                  transform: 'translateY(-50%)',
                  background: 'none', border: 'none', cursor: 'pointer',
                  color: 'var(--text-muted)', display: 'flex', alignItems: 'center',
                }}
              >
                {showPass ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 24 }}>
            <Link
              to="/forgot-password"
              style={{
                color: 'var(--primary, #3b82f6)',
                fontSize: 13,
                textDecoration: 'none',
              }}
            >
              {t('auth.forgotPassword')}
            </Link>
          </div>

          <button
            className="btn btn-primary"
            type="submit"
            disabled={loading}
            style={{ width: '100%', padding: '12px', fontSize: 15, fontWeight: 600 }}
          >
            {loading ? t('common.loadingLogin') : t('common.login')}
          </button>
        </form>

        <p style={{
          textAlign: 'center', marginTop: 20, fontSize: 11,
          color: 'var(--text-faint, rgba(255,255,255,0.25))',
        }}>
          {t('auth.footerCredits')}
        </p>
      </div>
    </div>
  );
}

export default Login;
