import React, { useState, useEffect, useRef } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import client from '../api/client';
import {
  LayoutDashboard, Zap, Database, Droplet, MapPin, Users,
  BarChart2, LogOut, Bell, ClipboardCheck, Truck,
  PackageSearch, ChevronDown, ChevronLeft, ChevronRight, Settings, Cpu, ShieldCheck, Gauge, Key, X, Lock, Sun, Moon, ScrollText, Menu, Undo2, FileText, Wrench, PackageCheck, Boxes, Clock
} from 'lucide-react';
import usePermissions from '../permissions/usePermissions';
import useUiStore from '../store/uiStore';
import useTenantBranding from '../hooks/useTenantBranding';
import { useTranslation } from 'react-i18next';
import NavGroup from './NavGroup';


function Sidebar({ lang, theme, onToggleTheme, currentUser, unreadCount = 0, onLogout }) {
  const branding = useTenantBranding();
  const navigate = useNavigate();
  const ar = lang === 'ar';
  const { setLang, sidebarCollapsed, toggleSidebar } = useUiStore();
  const { t } = useTranslation();

  const roleKey = currentUser?.role || 'operator';
  const userRoles = (currentUser?.roles || []).map(r => (typeof r === 'string' ? r.toLowerCase() : ''));
  const hasRole = (name) => userRoles.includes(name);
  const { can, canExplicit } = usePermissions(currentUser);

  const navDomain = (path) => {
    if (path === '/' || path.startsWith('/generators') || path.startsWith('/my-generators') || path.startsWith('/operations')) return 'blue';
    if (path.startsWith('/fuel') || path.startsWith('/stock') || path.startsWith('/inventory')) return 'amber';
    if (path.startsWith('/maintenance') || path.startsWith('/review')) return 'red';
    if (path.startsWith('/warehouse')) return 'teal';
    if (path.startsWith('/reports')) return 'purple';
    return 'indigo';
  };
  
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [passwordModalOpen, setPasswordModalOpen] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [notificationMenuOpen, setNotificationMenuOpen] = useState(false);
  const [notificationPreview, setNotificationPreview] = useState([]);
  
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [pwdLoading, setPwdLoading] = useState(false);
  const [pwdError, setPwdError] = useState('');
  const [pwdSuccess, setPwdSuccess] = useState('');
  
  const menuRef = useRef(null);
  const closeMobile = () => setMobileOpen(false);

  useEffect(() => { 
    document.body.style.overflow = mobileOpen ? 'hidden' : ''; 
    return () => { document.body.style.overflow = ''; }; 
  }, [mobileOpen]);

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

  const openNotifications = async () => {
    setNotificationMenuOpen(open => !open);
    try {
      const res = await client.get('/api/notifications');
      setNotificationPreview((res.data.notifications || []).slice(0, 5));
    } catch {}
  };

  const markPreviewRead = async (id) => {
    try {
      await client.patch(`/api/notifications/${id}/read`);
      setNotificationPreview(items => items.map(item => item.id === id ? { ...item, isRead: true } : item));
    } catch {}
  };

  const beforeMaintenanceNav = [
    { to: '/', icon: LayoutDashboard, label: t('sidebar.dashboard'), show: can('dashboard') },
    { to: '/operations', icon: Zap, label: t('sidebar.operations'), show: can('operational_data', 'view') },
    { to: '/my-generators', icon: Zap, label: t('sidebar.myGenerators'), show: hasRole('operator') || roleKey === 'operator', contextual: true },
  ].filter(item => item.show);

  const maintenanceNav = [
    { to: '/maintenance/service', icon: Clock, label: t('sidebar.serviceIntervals'), show: can('maintenance') && can('operational_data', 'view') },
    { to: '/maintenance/permits', icon: FileText, label: t('sidebar.workPermits'), show: can('maintenance') },
    { to: '/warehouse/parts', icon: PackageSearch, label: t('sidebar.partsCatalog'), show: can('warehouse') },
    { to: '/warehouse/stock', icon: PackageSearch, label: t('sidebar.warehouseStock'), show: can('warehouse') },
    { to: '/maintenance/installed-parts', icon: Wrench, label: t('sidebar.installedParts'), show: can('maintenance') },
    { to: '/warehouse/dispatch', icon: PackageCheck, label: t('sidebar.dispatchRequests'), show: can('warehouse') },
    { to: '/warehouse/supply', icon: Boxes, label: t('sidebar.warehouseSupply'), show: can('warehouse') },
  ].filter(item => item.show);

  const afterMaintenanceNav = [
    { to: '/fuel-movements', icon: Truck, label: t('sidebar.fuelMovements'), show: can('fuel_movements') },
    { to: '/generators/overview', icon: Zap, label: t('sidebar.generatorsOverview'), show: canExplicit('generators', 'view') },
    { to: '/stock/fuel', icon: Database, label: t('sidebar.fuelStock'), show: can('inventory') },
    { to: '/stock/oil', icon: Droplet, label: t('sidebar.oilStock'), show: canExplicit('oil', 'view') },
    { to: '/review-changes', icon: ClipboardCheck, label: t('sidebar.reviewReadings'), show: can('fuel_movements', 'approve') || can('readings', 'approve') || can('inventory', 'approve') },
    { to: '/inventory', icon: PackageSearch, label: t('sidebar.periodicInventory'), show: can('inventory') },
  ].filter(item => item.show);

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

  const preferredMobilePaths = ['/', '/operations', '/maintenance', '/warehouse/stock'];
  const availableNav = [
    ...beforeMaintenanceNav,
    { to: '/maintenance', icon: Wrench, label: t('sidebar.maintenance'), show: can('maintenance') },
    ...maintenanceNav,
    ...afterMaintenanceNav,
  ].filter(item => item.show !== false);
  const preferredMobileNav = preferredMobilePaths
    .map(path => availableNav.find(item => item.to === path))
    .filter(Boolean);
  const mobileNav = [
    ...preferredMobileNav,
    ...availableNav.filter(item => !preferredMobilePaths.includes(item.to)),
  ].slice(0, 4);

  const roleLabels = {
    admin: ar ? 'مدير النظام' : 'System Admin',
    operator: ar ? 'مشغل' : 'Operator',
    supervisor: ar ? 'مشرف' : 'Supervisor',
    fuel_manager: ar ? 'مسؤول الوقود' : 'Fuel Manager',
  };

  return (
    <>
      <button className="mobile-menu-toggle" onClick={() => setMobileOpen(o => !o)} aria-label={ar ? 'القائمة' : 'Menu'}>
        <Menu size={20} />
      </button>
      <div className={`mobile-sidebar-overlay${mobileOpen ? ' open' : ''}`} onClick={closeMobile} />
      <aside className={`sidebar${mobileOpen ? ' open' : ''}${sidebarCollapsed ? ' collapsed' : ''}`}>
        <div className="sidebar-logo" onClick={() => { navigate('/'); closeMobile(); }} style={{ cursor: 'pointer' }}>
          {branding.logoUrl && <div className="sidebar-logo-icon">
             <img src={branding.logoUrl} alt={branding.name} onError={e => e.target.style.display = 'none'} style={{ width: '100%', height: '100%', objectFit: 'contain' }} />
          </div>}
          <div className="sidebar-logo-text" style={{ flex: 1 }}>
            <div className="sidebar-logo-name">{branding.name}</div>
            <div className="sidebar-logo-sub">{ar ? 'نظام تتبع الوقود والطاقة' : 'Energy Tracking'}</div>
          </div>
          <button
            type="button"
            className="sidebar-collapse-toggle"
            onClick={(event) => { event.stopPropagation(); toggleSidebar(); }}
            aria-label={sidebarCollapsed ? (ar ? 'توسيع القائمة' : 'Expand sidebar') : (ar ? 'طي القائمة' : 'Collapse sidebar')}
            title={sidebarCollapsed ? (ar ? 'توسيع القائمة' : 'Expand sidebar') : (ar ? 'طي القائمة' : 'Collapse sidebar')}
          >
            {sidebarCollapsed ? <ChevronLeft size={18} /> : <ChevronRight size={18} />}
          </button>
        </div>

        {currentUser && (
          <div className="sidebar-user-section" ref={menuRef} style={{ position: 'relative', padding: '16px 12px 8px' }}>
            <div className="sidebar-user" onClick={() => setUserMenuOpen(!userMenuOpen)} style={{ cursor: 'pointer' }}>
              <div className="sidebar-user-avatar">
                {currentUser.name?.[0]?.toUpperCase() || 'U'}
              </div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div className="sidebar-user-name" style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {currentUser.name}
                </div>
                <div className="sidebar-user-role">
                  {roleLabels[roleKey] || roleKey}
                </div>
              </div>
              <ChevronDown size={14} style={{ transform: userMenuOpen ? 'rotate(180deg)' : 'none', transition: '0.2s', color: 'var(--text-muted)' }} />
            </div>
            
            {userMenuOpen && (
              <div className="user-dropdown-menu" style={{ 
                position: 'absolute', top: '100%', insetInline: 12, marginTop: 4, 
                background: 'var(--surface)', border: '1px solid var(--border-strong)', 
                borderRadius: 'var(--radius)', padding: '6px', zIndex: 100,
                boxShadow: 'var(--shadow-md)', animation: 'fadeIn 0.15s ease'
              }}>
                <button 
                  style={{ width: '100%', display: 'flex', alignItems: 'center', gap: 10, padding: '10px 12px', background: 'transparent', border: 'none', color: 'var(--text)', fontSize: 'var(--text-md)', fontWeight: 500, borderRadius: 'var(--radius-sm)', cursor: 'pointer', transition: 'var(--transition)' }}
                  onMouseOver={e => e.currentTarget.style.background = 'var(--surface-2)'}
                  onMouseOut={e => e.currentTarget.style.background = 'transparent'}
                  onClick={() => { setPasswordModalOpen(true); setUserMenuOpen(false); }}
                >
                  <Key size={16} className="text-muted" />
                  <span>{ar ? 'تغيير كلمة المرور' : 'Change Password'}</span>
                </button>
                <div style={{ height: 1, background: 'var(--border)', margin: '4px 0' }} />
                <button 
                  style={{ width: '100%', display: 'flex', alignItems: 'center', gap: 10, padding: '10px 12px', background: 'transparent', border: 'none', color: 'var(--danger)', fontSize: 'var(--text-md)', fontWeight: 500, borderRadius: 'var(--radius-sm)', cursor: 'pointer', transition: 'var(--transition)' }}
                  onMouseOver={e => e.currentTarget.style.background = 'var(--danger-subtle)'}
                  onMouseOut={e => e.currentTarget.style.background = 'transparent'}
                  onClick={onLogout}
                >
                  <LogOut size={16} />
                  <span>{ar ? 'تسجيل الخروج' : 'Logout'}</span>
                </button>
              </div>
            )}
          </div>
        )}

        <nav className="sidebar-nav">
          {can('alerts') && (
            <div style={{ position: 'relative' }} data-nav-top={ar ? 'الإشعارات' : 'Notifications'}>
              <div className="nav-item" title={sidebarCollapsed ? (ar ? 'الإشعارات' : 'Notifications') : undefined}>
                <span className="nav-icon-tile nav-icon-tile-red"><Bell size={19} strokeWidth={2.25} /></span>
                <NavLink className="nav-item-label" to="/notifications" onClick={closeMobile} style={{ flex: 1, color: 'inherit', textDecoration: 'none' }}>
                  {ar ? 'الإشعارات' : 'Notifications'}
                </NavLink>
                {unreadCount > 0 && <span className="badge badge-danger" style={{ padding: '2px 6px' }}>{unreadCount > 99 ? '99+' : unreadCount}</span>}
                <button onClick={openNotifications} aria-label={ar ? 'فتح الإشعارات السريعة' : 'Open notification preview'} style={{ border: 0, background: 'transparent', color: 'inherit', cursor: 'pointer', display: 'flex', padding: 2 }}>
                  <ChevronDown size={14} style={{ transform: notificationMenuOpen ? 'rotate(180deg)' : 'none', transition: 'var(--transition)' }} />
                </button>
              </div>
              {notificationMenuOpen && (
                <div role="menu" style={{ position: 'absolute', insetInline: 6, top: '100%', zIndex: 120, background: 'var(--surface)', border: '1px solid var(--border-strong)', borderRadius: 'var(--radius)', boxShadow: 'var(--shadow-md)', overflow: 'hidden' }}>
                  {notificationPreview.length === 0 ? (
                    <div style={{ padding: 14, fontSize: 12, color: 'var(--text-muted)' }}>{ar ? 'لا توجد إشعارات حديثة' : 'No recent notifications'}</div>
                  ) : notificationPreview.map(notification => (
                    <button key={notification.id} role="menuitem" onClick={() => { if (!notification.isRead) markPreviewRead(notification.id); navigate('/notifications'); closeMobile(); }} style={{ width: '100%', textAlign: 'start', padding: '10px 12px', border: 0, borderBottom: '1px solid var(--border)', background: notification.isRead ? 'transparent' : 'var(--surface-2)', color: 'var(--text)', cursor: 'pointer' }}>
                      <div style={{ fontWeight: notification.isRead ? 500 : 700, fontSize: 12 }}>{isTranslationKey(notification.title) ? t(notification.title) : notification.title}</div>
                      <div style={{ color: 'var(--text-muted)', fontSize: 11, marginTop: 3, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{isTranslationKey(notification.message) ? t(notification.message) : notification.message}</div>
                    </button>
                  ))}
                  <NavLink to="/notifications" onClick={() => { setNotificationMenuOpen(false); closeMobile(); }} style={{ display: 'block', padding: 10, textAlign: 'center', color: 'var(--primary)', fontSize: 12, textDecoration: 'none' }}>{ar ? 'عرض كل الإشعارات' : 'View all notifications'}</NavLink>
                </div>
              )}
            </div>
          )}

          <div className="sidebar-section-title">{ar ? 'القائمة الرئيسية' : 'Main Menu'}</div>
          {beforeMaintenanceNav.map(({ to, icon: Icon, label }) => (
            <NavLink key={to} to={to} data-nav-top={label} title={sidebarCollapsed ? label : undefined} end={to === '/' || to === '/maintenance'} onClick={closeMobile} className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}>
              <span className={`nav-icon-tile nav-icon-tile-${navDomain(to)}`}><Icon size={19} strokeWidth={2.25} /></span>
              <span className="nav-item-label">{label}</span>
            </NavLink>
          ))}
          <NavGroup icon={Wrench} label={t('sidebar.maintenance')} headerTo="/maintenance" items={maintenanceNav} onNavigate={closeMobile} collapsed={sidebarCollapsed} />
          {afterMaintenanceNav.map(({ to, icon: Icon, label }) => (
            <NavLink key={to} to={to} data-nav-top={label} title={sidebarCollapsed ? label : undefined} onClick={closeMobile} className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}>
              <span className={`nav-icon-tile nav-icon-tile-${navDomain(to)}`}><Icon size={19} strokeWidth={2.25} /></span><span className="nav-item-label">{label}</span>
            </NavLink>
          ))}
          {(can('reports') || can('warehouse')) && (
            <NavLink to="/reports" data-nav-top={t('sidebar.reports')} title={sidebarCollapsed ? t('sidebar.reports') : undefined} onClick={closeMobile} className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}>
              <span className="nav-icon-tile nav-icon-tile-purple"><BarChart2 size={19} strokeWidth={2.25} /></span>
              <span className="nav-item-label">{t('sidebar.reports')}</span>
            </NavLink>
          )}
          <NavGroup icon={Settings} label={t('sidebar.systemSettings')} items={setupNav} onNavigate={closeMobile} collapsed={sidebarCollapsed} />
        </nav>

        <div className="sidebar-footer">
          <button className="theme-toggle-btn" onClick={onToggleTheme}>
            {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
            <span style={{ flex: 1, textAlign: 'start' }}>{ar ? (theme === 'dark' ? 'الوضع الفاتح' : 'الوضع الداكن') : (theme === 'dark' ? 'Light Mode' : 'Dark Mode')}</span>
          </button>
          <button className="lang-toggle-btn" onClick={() => setLang(ar ? 'en' : 'ar')}
            title={ar ? 'Switch to English' : 'التبديل إلى العربية'}
            style={{ marginTop: 2 }}
          >
            <span style={{ fontSize: 16, lineHeight: 1 }}>{ar ? '🇬🇧' : '🇸🇦'}</span>
            <span style={{ flex: 1, textAlign: 'start' }}>{ar ? 'English' : 'عربي'}</span>
          </button>
        </div>

        {passwordModalOpen && (
          <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setPasswordModalOpen(false)}>
            <div className="modal-box">
              <div className="modal-header">
                <div className="modal-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Lock size={20} style={{ color: 'var(--primary)' }} />
                  {ar ? 'تغيير كلمة المرور' : 'Change Password'}
                </div>
                <button className="modal-close" aria-label="Close" onClick={() => setPasswordModalOpen(false)}><X size={20} /></button>
              </div>
              <div className="modal-body">
                {pwdError && <div className="form-error" role="alert" style={{ marginBottom: 16 }}>{pwdError}</div>}
                {pwdSuccess && <div className="text-success" style={{ marginBottom: 16, fontWeight: 500 }}>{pwdSuccess}</div>}
                
                <form onSubmit={handleChangePassword}>
                  <div className="form-group">
                    <label className="form-label">{ar ? 'كلمة المرور الحالية' : 'Current Password'}</label>
                    <input type="password" className="form-control" value={currentPassword} onChange={e => setCurrentPassword(e.target.value)} required />
                  </div>
                  <div className="form-group" style={{ marginBottom: 24 }}>
                    <label className="form-label">{ar ? 'كلمة المرور الجديدة' : 'New Password'}</label>
                    <input type="password" className="form-control" value={newPassword} onChange={e => setNewPassword(e.target.value)} required minLength={7} />
                    <div className="form-help">
                      {ar ? 'يجب أن تتضمن حرفًا ورقمًا ورمزًا.' : 'Must include a letter, number, and symbol.'}
                    </div>
                  </div>
                  <button type="submit" className="btn btn-primary" style={{ width: '100%' }} disabled={pwdLoading}>
                    {pwdLoading ? <div className="ft-spinner" style={{ width: 20, height: 20, borderWidth: 2 }} /> : (ar ? 'تأكيد التغيير' : 'Confirm Change')}
                  </button>
                </form>
              </div>
            </div>
          </div>
        )}
      </aside>
      <nav className="mobile-bottom-nav" aria-label={ar ? 'التنقل السريع' : 'Quick navigation'}>
        {mobileNav.map(({ to, icon: Icon, label }) => (
          <NavLink key={to} to={to} end={to === '/'} onClick={closeMobile} className={({ isActive }) => `mobile-bottom-nav-item${isActive ? ' active' : ''}`}>
            <Icon size={20} strokeWidth={2.25} />
            <span>{label}</span>
          </NavLink>
        ))}
      </nav>
    </>
  );
}

export default Sidebar;

const isTranslationKey = (value) => typeof value === 'string' &&
  (value.startsWith('operational.') || value.startsWith('maintenanceService.'));
