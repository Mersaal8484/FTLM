import React, { useState, useEffect, useRef } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import client from '../api/client';
import {
  LayoutDashboard, Zap, Database, Droplet, MapPin, Users,
  BarChart2, LogOut, Bell, ClipboardCheck, Truck,
  PackageSearch, ChevronDown, Settings, Cpu, ShieldCheck, Gauge, Key, X, Lock, Sun, Moon, ScrollText, Menu, Undo2
} from 'lucide-react';
import usePermissions from '../permissions/usePermissions';

function Sidebar({ lang, theme, onToggleTheme, currentUser, unreadCount = 0, onLogout }) {
  const navigate = useNavigate();
  const ar = lang === 'ar';
  const roleKey = currentUser?.role || 'operator';
  const userRoles = (currentUser?.roles || []).map(r => (typeof r === 'string' ? r.toLowerCase() : ''));
  const hasRole = (name) => userRoles.includes(name);
  const { can, canExplicit } = usePermissions(currentUser);
  const [setupOpen, setSetupOpen] = useState(false);
  const [reportsOpen, setReportsOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [passwordModalOpen, setPasswordModalOpen] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [pwdLoading, setPwdLoading] = useState(false);
  const [pwdError, setPwdError] = useState('');
  const [pwdSuccess, setPwdSuccess] = useState('');
  
  const menuRef = useRef(null);

  const closeMobile = () => setMobileOpen(false);

  useEffect(() => { document.body.style.overflow = mobileOpen ? 'hidden' : ''; return () => { document.body.style.overflow = ''; }; }, [mobileOpen]);

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (menuRef.current && !menuRef.current.contains(event.target)) {
        setUserMenuOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleChangePassword = async (e) => {
    e.preventDefault();
    setPwdError(''); setPwdSuccess('');
    if (!currentPassword || !newPassword) return setPwdError(ar ? 'يرجى تعبئة جميع الحقول' : 'Please fill all fields');
    if (newPassword.length < 7) return setPwdError(ar ? 'كلمة المرور قصيرة (7 أحرف على الأقل)' : 'Password too short (min 7 chars)');
    
    setPwdLoading(true);
    try {
      const res = await client.post('/api/auth/change-password', { currentPassword, newPassword });
      setPwdSuccess(res.data.msg);
      setCurrentPassword('');
      setNewPassword('');
      setTimeout(() => { setPasswordModalOpen(false); setPwdSuccess(''); }, 2000);
    } catch (err) {
      setPwdError(err.response?.data?.msg || 'حدث خطأ');
    } finally {
      setPwdLoading(false);
    }
  };

  const mainNav = [
    { to: '/', icon: LayoutDashboard, label: ar ? 'لوحة التحكم' : 'Dashboard', show: can('dashboard') },
    { to: '/my-generators', icon: Zap, label: ar ? 'مولداتي' : 'My Generators', show: hasRole('operator') || roleKey === 'operator' },
    { to: '/generators', icon: Zap, label: ar ? 'المولدات' : 'Generators', show: canExplicit('generators', 'view') },
    { to: '/tanks', icon: Database, label: ar ? 'الخزانات' : 'Fuel Tanks', show: can('tanks') },
    { to: '/oils', icon: Droplet, label: ar ? 'مخزون الزيوت' : 'Oil Inventory', show: canExplicit('oil', 'view') },
    { to: '/fuel-movements', icon: Truck, label: ar ? 'حركات الوقود/الزيت' : 'Fuel & Oil Movements', show: can('fuel_movements') },
    { to: '/inventory', icon: PackageSearch, label: ar ? 'الجرد الدوري' : 'Periodic Inventory', show: can('inventory') },
    { to: '/review-changes', icon: ClipboardCheck, label: ar ? 'مراجعة التعديلات' : 'Review Changes', show: can('fuel_movements', 'approve') || can('readings', 'approve') || can('inventory', 'approve') },
  ].filter(item => item.show);

  const reportsNav = [
    { to: '/reports/station-readings', icon: BarChart2, label: ar ? 'قراءات المحطات' : 'Station Readings', show: can('reports') },
    { to: '/reports/fuel-transactions', icon: Truck, label: ar ? 'حركات الوقود' : 'Fuel Transactions', show: can('reports') },
    { to: '/reports/oil-transactions', icon: Droplet, label: ar ? 'حركات الزيوت' : 'Oil Transactions', show: can('reports') },
    { to: '/reports/generators', icon: Zap, label: ar ? 'تقرير المولدات' : 'Generators Report', show: can('reports') },
    { to: '/reports/efficiency', icon: BarChart2, label: ar ? 'تقرير الكفاءة' : 'Efficiency Report', show: can('reports') },
    { to: '/reports/periodic-inventory', icon: PackageSearch, label: ar ? 'تقرير الجرد الدوري' : 'Periodic Inventory', show: can('reports') },
  ].filter(item => item.show);

  // System Setup navigation items - filtered by permissions or role
  const setupNav = [
    { to: '/locations', icon: MapPin, label: ar ? 'المحافظات والمحطات' : 'Locations & Stations', show: can('settings', 'manage') },
    { to: '/generators', icon: Zap, label: ar ? 'إعداد المولدات' : 'Setup Generators', show: can('settings', 'manage') },
    { to: '/meters', icon: Gauge, label: ar ? 'العدادات' : 'Meters', show: can('settings', 'manage') },
    { to: '/setup-oils', icon: Droplet, label: ar ? 'إعداد مخزون الزيت' : 'Setup Oils', show: can('settings', 'manage') },
    { to: '/tanks', icon: Database, label: ar ? 'إعداد الخزانات' : 'Setup Tanks', show: can('settings', 'manage') },
    { to: '/controllers', icon: Cpu, label: ar ? 'إعداد شاشات المولدات' : 'Setup Controllers', show: can('settings', 'manage') },
    { to: '/users', icon: Users, label: ar ? 'إدارة المستخدمين' : 'Manage Users', show: can('users') },
    { to: '/roles', icon: ShieldCheck, label: ar ? 'الأدوار والصلاحيات' : 'Roles & Permissions', show: can('settings', 'manage') },
    { to: '/audits', icon: ScrollText, label: ar ? 'سجل التدقيق' : 'Audit Log', show: can('audits') },
    { to: '/rollback', icon: Undo2, label: ar ? 'التراجع عن الاعتماد' : 'Approval Rollback', show: hasRole('admin') || roleKey === 'admin' },
  ].filter(item => item.show);

  const roleLabels = {
    admin: ar ? 'مدير النظام' : 'System Admin',
    operator: ar ? 'مشغل' : 'Operator',
    supervisor: ar ? 'مشرف' : 'Supervisor',
    fuel_manager: ar ? 'مسؤول الوقود' : 'Fuel Manager',
  };

  const roleColors = {
    admin: 'var(--primary)',
    operator: 'var(--secondary)',
    supervisor: 'var(--warning)',
    fuel_manager: 'hsl(25,90%,55%)',
  };

  return (
    <>
      <button className="mobile-menu-toggle" onClick={() => setMobileOpen(o => !o)} aria-label={ar ? 'القائمة' : 'Menu'}>
        <Menu size={20} />
      </button>
      <div className={`mobile-sidebar-overlay${mobileOpen ? ' open' : ''}`} onClick={closeMobile} />
    <aside className={`sidebar${mobileOpen ? ' open' : ''}`}>
      <div className="sidebar-logo" style={{ cursor: 'pointer', marginBottom: 15 }} onClick={() => { navigate('/'); closeMobile(); }}>
        <img
          src="/pec-logo.png"
          alt="PEC"
          onError={e => { e.target.style.display = 'none'; }}
          style={{ width: 42, height: 42, objectFit: 'contain', borderRadius: 8, flexShrink: 0 }}
        />
        <div className="sidebar-logo-text">
          <span className="sidebar-logo-name">Fuel & Energy Tracking</span>
          <span className="sidebar-logo-sub">{ar ? 'نظام تتبع الوقود والطاقة' : 'Fuel & Energy Tracking'}</span>
        </div>
      </div>

      {currentUser && (
        <div className="sidebar-user-section" ref={menuRef} style={{ position: 'relative', padding: '0 15px', marginBottom: 20 }}>
          <div 
            className="sidebar-user" 
            style={{ cursor: 'pointer', padding: '10px 12px', background: 'var(--card-2)', borderRadius: 12, border: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 10, transition: 'all 0.2s', ...(userMenuOpen ? {background: 'var(--card)', borderColor: 'var(--border-hover)'} : {}) }}
            onClick={() => setUserMenuOpen(!userMenuOpen)}
          >
            <div className="sidebar-user-avatar" style={{ width: 34, height: 34, borderRadius: '50%', background: `linear-gradient(135deg, ${roleColors[roleKey] || 'var(--primary)'}, var(--secondary))`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 13, fontWeight: 700, color: '#fff', flexShrink: 0 }}>
              {currentUser.name?.[0]?.toUpperCase() || 'U'}
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div className="sidebar-user-name" style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', fontSize: 13, fontWeight: 700, color: 'var(--text)' }}>
                {currentUser.name}
              </div>
              <div className="sidebar-user-role" style={{ color: roleColors[roleKey] || 'var(--primary)', fontSize: 11, fontWeight: 600 }}>
                {roleLabels[roleKey] || roleKey}
              </div>
            </div>
            <ChevronDown size={14} style={{ transform: userMenuOpen ? 'rotate(180deg)' : 'none', transition: '0.2s', color: 'var(--text-muted)' }} />
          </div>
          
          {userMenuOpen && (
            <div className="user-dropdown-menu" style={{ 
              position: 'absolute', top: '100%', left: 15, right: 15, marginTop: 8, 
              background: 'var(--card)', border: '1px solid var(--border)', 
              borderRadius: 12, padding: '8px', zIndex: 100,
              boxShadow: '0 10px 25px -5px rgba(0, 0, 0, 0.3)',
              animation: 'fadeIn 0.2s ease forwards'
            }}>
              <button 
                style={{ width: '100%', display: 'flex', alignItems: 'center', gap: 10, padding: '10px 12px', background: 'transparent', border: 'none', color: 'var(--text)', fontSize: 13, fontWeight: 500, borderRadius: 8, cursor: 'pointer', transition: '0.2s' }}
                onMouseOver={e => e.currentTarget.style.background = 'var(--card-2)'}
                onMouseOut={e => e.currentTarget.style.background = 'transparent'}
                onClick={() => { setPasswordModalOpen(true); setUserMenuOpen(false); }}
              >
                <Key size={14} style={{ color: 'var(--primary)' }} />
                <span>{ar ? 'تغيير كلمة المرور' : 'Change Password'}</span>
              </button>
              <div style={{ height: 1, background: 'var(--border)', margin: '4px 0' }} />
              <button 
                style={{ width: '100%', display: 'flex', alignItems: 'center', gap: 10, padding: '10px 12px', background: 'transparent', border: 'none', color: 'var(--danger)', fontSize: 13, fontWeight: 500, borderRadius: 8, cursor: 'pointer', transition: '0.2s' }}
                onMouseOver={e => e.currentTarget.style.background = 'var(--danger-glow)'}
                onMouseOut={e => e.currentTarget.style.background = 'transparent'}
                onClick={onLogout}
              >
                <LogOut size={14} />
                <span>{ar ? 'تسجيل الخروج' : 'Logout'}</span>
              </button>
            </div>
          )}
        </div>
      )}

      <nav className="sidebar-nav">
        {can('alerts') && (
          <NavLink to="/notifications" onClick={closeMobile} className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`} style={{ position: 'relative' }}>
            <Bell size={18} strokeWidth={1.8} />
            <span>{ar ? 'الإشعارات' : 'Notifications'}</span>
            {unreadCount > 0 && (
              <span style={{ marginRight: ar ? 0 : 'auto', marginLeft: ar ? 'auto' : 0, background: 'var(--danger)', color: '#fff', fontSize: 10, fontWeight: 700, padding: '2px 7px', borderRadius: 99, minWidth: 20, textAlign: 'center' }}>
                {unreadCount > 99 ? '99+' : unreadCount}
              </span>
            )}
          </NavLink>
        )}

        <div className="sidebar-section-title">{ar ? 'القائمة الرئيسية' : 'Main Menu'}</div>
        {mainNav.map(({ to, icon: Icon, label }) => (
          <NavLink key={to} to={to} end={to === '/'} onClick={closeMobile} className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}>
            <Icon size={18} strokeWidth={1.8} />
            <span>{label}</span>
          </NavLink>
        ))}

        {reportsNav.length > 0 && (
          <>
            <button className="nav-item" style={{ marginTop: 8 }} onClick={() => setReportsOpen(o => !o)}>
              <BarChart2 size={18} strokeWidth={1.8} />
              <span style={{ flex: 1 }}>{ar ? 'التقارير' : 'Reports'}</span>
              <ChevronDown size={14} style={{ transform: reportsOpen ? 'rotate(180deg)' : 'none', transition: '0.2s', flexShrink: 0 }} />
            </button>
            {reportsOpen && (
              <div style={{ paddingRight: ar ? 0 : 8, paddingLeft: ar ? 8 : 0 }}>
                {reportsNav.map(({ to, icon: Icon, label }) => (
                  <NavLink key={to} to={to} onClick={closeMobile} className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`} style={{ fontSize: 13 }}>
                    <Icon size={15} strokeWidth={1.8} />
                    <span>{label}</span>
                  </NavLink>
                ))}
              </div>
            )}
          </>
        )}

        {setupNav.length > 0 && (
          <>
            <button className="nav-item" style={{ marginTop: 8 }} onClick={() => setSetupOpen(o => !o)}>
              <Settings size={18} strokeWidth={1.8} />
              <span style={{ flex: 1 }}>{ar ? 'إعداد النظام' : 'System Setup'}</span>
              <ChevronDown size={14} style={{ transform: setupOpen ? 'rotate(180deg)' : 'none', transition: '0.2s', flexShrink: 0 }} />
            </button>
            {setupOpen && (
              <div style={{ paddingRight: ar ? 0 : 8, paddingLeft: ar ? 8 : 0 }}>
                {setupNav.map(({ to, icon: Icon, label }) => (
                  <NavLink key={to} to={to} onClick={closeMobile} className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`} style={{ fontSize: 13 }}>
                    <Icon size={15} strokeWidth={1.8} />
                    <span>{label}</span>
                  </NavLink>
                ))}
              </div>
            )}
          </>
        )}
      </nav>

      <div className="sidebar-footer">
        <button className="lang-toggle-btn" onClick={onToggleTheme} style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8 }}>
          {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
          <span>{ar ? (theme === 'dark' ? 'الوضع النهاري' : 'الوضع الليلي') : (theme === 'dark' ? 'Light Mode' : 'Dark Mode')}</span>
        </button>
      </div>

      {passwordModalOpen && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setPasswordModalOpen(false)} style={{ zIndex: 9999 }}>
          <div className="modal-box" style={{ maxWidth: 400 }}>
            <div className="modal-header">
              <div className="modal-title" style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <Lock size={18} style={{ color: 'var(--primary)' }} />
                {ar ? 'تغيير كلمة المرور' : 'Change Password'}
              </div>
              <button className="modal-close" onClick={() => setPasswordModalOpen(false)}><X size={18} /></button>
            </div>
            <div className="modal-body">
              {pwdError && <div className="alert alert-danger" style={{ marginBottom: 15, padding: 10, fontSize: 13 }}>{pwdError}</div>}
              {pwdSuccess && <div className="alert alert-success" style={{ marginBottom: 15, padding: 10, fontSize: 13, background: 'rgba(16, 185, 129, 0.1)', color: '#10b981', border: '1px solid rgba(16, 185, 129, 0.2)' }}>{pwdSuccess}</div>}
              
              <form onSubmit={handleChangePassword}>
                <div className="form-group" style={{ marginBottom: 15 }}>
                  <label className="form-label" style={{ fontSize: 13, marginBottom: 8 }}>{ar ? 'كلمة المرور الحالية' : 'Current Password'}</label>
                  <input type="password" className="form-control" value={currentPassword} onChange={e => setCurrentPassword(e.target.value)} required />
                </div>
                <div className="form-group" style={{ marginBottom: 20 }}>
                  <label className="form-label" style={{ fontSize: 13, marginBottom: 8 }}>{ar ? 'كلمة المرور الجديدة' : 'New Password'}</label>
                  <input type="password" className="form-control" value={newPassword} onChange={e => setNewPassword(e.target.value)} required minLength={7} />
                  <div style={{ color: 'var(--text-muted)', fontSize: 12, marginTop: 6 }}>
                    {ar ? 'يجب أن تتضمن حرفًا ورقمًا ورمزًا.' : 'Must include a letter, number, and symbol.'}
                  </div>
                </div>
                <button type="submit" className="btn btn-primary" style={{ width: '100%', height: 42, display: 'flex', justifyContent: 'center' }} disabled={pwdLoading}>
                  {pwdLoading ? <div className="ft-spinner" style={{ width: 16, height: 16, borderWidth: 2 }} /> : (ar ? 'تأكيد التغيير' : 'Confirm Change')}
                </button>
              </form>
            </div>
          </div>
        </div>
      )}
    </aside>
    </>
  );
}

export default Sidebar;
