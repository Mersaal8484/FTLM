import React, { useState, useEffect, useRef } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import client from '../api/client';
import {
  LayoutDashboard, Zap, Database, Droplet, MapPin, Users,
  BarChart2, LogOut, Bell, ClipboardCheck, Truck,
  PackageSearch, ChevronDown, Settings, Cpu, ShieldCheck, Gauge, Key, X, Lock, Sun, Moon, ScrollText, Menu, Undo2, FileText, Wrench, PackageCheck, Boxes
} from 'lucide-react';
import usePermissions from '../permissions/usePermissions';
import useUiStore from '../store/uiStore';
import useTenantBranding from '../hooks/useTenantBranding';


function Sidebar({ lang, theme, onToggleTheme, currentUser, unreadCount = 0, onLogout }) {
  const branding = useTenantBranding();
  const navigate = useNavigate();
  const ar = lang === 'ar';
  const { setLang } = useUiStore();

  const roleKey = currentUser?.role || 'operator';
  const userRoles = (currentUser?.roles || []).map(r => (typeof r === 'string' ? r.toLowerCase() : ''));
  const hasRole = (name) => userRoles.includes(name);
  const { can, canExplicit } = usePermissions(currentUser);
  
  const [setupOpen, setSetupOpen] = useState(false);
  const [reportsOpen, setReportsOpen] = useState(false);
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

  const mainNav = [
    { to: '/', icon: LayoutDashboard, label: ar ? 'لوحة التحكم' : 'Dashboard', show: can('dashboard') },
    { to: '/my-generators', icon: Zap, label: ar ? 'مولداتي' : 'My Generators', show: hasRole('operator') || roleKey === 'operator' },
    { to: '/generators/overview', icon: Zap, label: ar ? 'عرض المولدات' : 'Generators Overview', show: canExplicit('generators', 'view') },
    { to: '/stock/fuel', icon: Database, label: ar ? 'مخزون الوقود' : 'Fuel Stock', show: can('inventory') },
    { to: '/stock/oil', icon: Droplet, label: ar ? 'مخزون الزيوت' : 'Oil Stock', show: canExplicit('oil', 'view') },
    { to: '/fuel-movements', icon: Truck, label: ar ? 'حركات الوقود/الزيت' : 'Fuel & Oil Movements', show: can('fuel_movements') },
    { to: '/movement-logs', icon: FileText, label: ar ? 'سجل الحركات العام' : 'Movement Logs', show: can('fuel_movements') },
    { to: '/inventory', icon: PackageSearch, label: ar ? 'الجرد الدوري' : 'Periodic Inventory', show: can('inventory') },
    { to: '/review-changes', icon: ClipboardCheck, label: ar ? 'اعتماد القراءات' : 'Review Readings', show: can('fuel_movements', 'approve') || can('readings', 'approve') || can('inventory', 'approve') },
    { to: '/warehouse/dispatch', icon: PackageCheck, label: ar ? 'طلبات الصرف' : 'Dispatch Requests', show: can('warehouse') },
    { to: '/warehouse/supply', icon: Boxes, label: ar ? 'توريد المخازن' : 'Warehouse Supply', show: can('warehouse') },
    { to: '/maintenance', icon: Wrench, label: ar ? 'لوحة الصيانة' : 'Maintenance', show: can('maintenance') },
    { to: '/maintenance/permits', icon: FileText, label: ar ? 'تصاريح العمل' : 'Work Permits', show: can('maintenance') },
    { to: '/warehouse/parts', icon: PackageSearch, label: ar ? 'كتالوج القطع' : 'Parts Catalog', show: can('warehouse') },
    { to: '/warehouse/stock', icon: PackageSearch, label: ar ? 'المخزون الفعلي' : 'Warehouse Stock', show: can('warehouse') },
    { to: '/warehouse/reports', icon: BarChart2, label: ar ? 'تقارير المخازن' : 'Warehouse Reports', show: can('warehouse') },
    { to: '/maintenance/installed-parts', icon: Wrench, label: ar ? 'القطع المركبة' : 'Installed Parts', show: can('maintenance') },
  ].filter(item => item.show);

  const reportsNav = [
    { to: '/reports/station-readings', icon: BarChart2, label: ar ? 'قراءات المحطات' : 'Station Readings', show: can('reports') },
    { to: '/reports/fuel-transactions', icon: Truck, label: ar ? 'حركات الوقود' : 'Fuel Transactions', show: can('reports') },
    { to: '/reports/oil-transactions', icon: Droplet, label: ar ? 'حركات الزيوت' : 'Oil Transactions', show: can('reports') },
    { to: '/reports/generators', icon: Zap, label: ar ? 'تقرير المولدات' : 'Generators Report', show: can('reports') },
    { to: '/reports/efficiency', icon: BarChart2, label: ar ? 'تقرير الكفاءة' : 'Efficiency Report', show: can('reports') },
    { to: '/reports/periodic-inventory', icon: PackageSearch, label: ar ? 'تقرير الجرد الدوري' : 'Periodic Inventory', show: can('reports') },
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
      <aside className={`sidebar${mobileOpen ? ' open' : ''}`}>
        <div className="sidebar-logo" onClick={() => { navigate('/'); closeMobile(); }} style={{ cursor: 'pointer' }}>
          {branding.logoUrl && <div className="sidebar-logo-icon">
             <img src={branding.logoUrl} alt={branding.name} onError={e => e.target.style.display = 'none'} style={{ width: '100%', height: '100%', objectFit: 'contain' }} />
          </div>}
          <div className="sidebar-logo-text" style={{ flex: 1 }}>
            <div className="sidebar-logo-name">{branding.name}</div>
            <div className="sidebar-logo-sub">{ar ? 'نظام تتبع الوقود والطاقة' : 'Energy Tracking'}</div>
          </div>
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
            <div style={{ position: 'relative' }}>
              <div className="nav-item">
                <Bell size={18} strokeWidth={1.8} />
                <NavLink to="/notifications" onClick={closeMobile} style={{ flex: 1, color: 'inherit', textDecoration: 'none' }}>
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
                      <div style={{ fontWeight: notification.isRead ? 500 : 700, fontSize: 12 }}>{notification.title}</div>
                      <div style={{ color: 'var(--text-muted)', fontSize: 11, marginTop: 3, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{notification.message}</div>
                    </button>
                  ))}
                  <NavLink to="/notifications" onClick={() => { setNotificationMenuOpen(false); closeMobile(); }} style={{ display: 'block', padding: 10, textAlign: 'center', color: 'var(--primary)', fontSize: 12, textDecoration: 'none' }}>{ar ? 'عرض كل الإشعارات' : 'View all notifications'}</NavLink>
                </div>
              )}
            </div>
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
              <button className="nav-item" onClick={() => setReportsOpen(o => !o)}>
                <BarChart2 size={18} strokeWidth={1.8} />
                <span style={{ flex: 1, textAlign: 'start' }}>{ar ? 'التقارير' : 'Reports'}</span>
                <ChevronDown size={14} style={{ transform: reportsOpen ? 'rotate(180deg)' : 'none', transition: 'var(--transition)' }} />
              </button>
              {reportsOpen && (
                <div style={{ paddingInlineStart: 12 }}>
                  {reportsNav.map(({ to, icon: Icon, label }) => (
                    <NavLink key={to} to={to} onClick={closeMobile} className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`} style={{ padding: '8px 14px' }}>
                      <Icon size={16} strokeWidth={1.8} className="text-muted" />
                      <span style={{ fontSize: 'var(--text-sm)' }}>{label}</span>
                    </NavLink>
                  ))}
                </div>
              )}
            </>
          )}

          {setupNav.length > 0 && (
            <>
              <button className="nav-item" onClick={() => setSetupOpen(o => !o)}>
                <Settings size={18} strokeWidth={1.8} />
                <span style={{ flex: 1, textAlign: 'start' }}>{ar ? 'إعداد النظام' : 'System Setup'}</span>
                <ChevronDown size={14} style={{ transform: setupOpen ? 'rotate(180deg)' : 'none', transition: 'var(--transition)' }} />
              </button>
              {setupOpen && (
                <div style={{ paddingInlineStart: 12 }}>
                  {setupNav.map(({ to, icon: Icon, label }) => (
                    <NavLink key={to} to={to} onClick={closeMobile} className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`} style={{ padding: '8px 14px' }}>
                      <Icon size={16} strokeWidth={1.8} className="text-muted" />
                      <span style={{ fontSize: 'var(--text-sm)' }}>{label}</span>
                    </NavLink>
                  ))}
                </div>
              )}
            </>
          )}
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
    </>
  );
}

export default Sidebar;
