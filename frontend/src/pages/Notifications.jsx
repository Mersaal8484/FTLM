import React, { useState, useEffect } from 'react';
import client from '../api/client';
import { Bell, CheckCheck, Trash2, AlertTriangle, Info, AlertCircle, Zap, Database, Droplet } from 'lucide-react';
import useAuthStore from '../store/authStore';
import useToastStore from '../store/toastStore';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import socket from '../socket';

const typeConfig = {
  fuel_low:            { icon: Database, color: 'var(--danger)',  label: 'وقود منخفض' },
  oil_low:             { icon: Droplet,  color: 'var(--danger)',  label: 'زيت منخفض' },
  reading_pending:     { icon: Zap,      color: 'var(--warning)', label: 'قراءة بانتظار الموافقة' },
  reading_rejected:    { icon: AlertCircle, color: 'var(--danger)', label: 'قراءة مرفوضة' },
  reading_approved:    { icon: CheckCheck, color: 'var(--secondary)', label: 'قراءة موافق عليها' },
  inventory_submitted: { icon: Bell,     color: 'var(--primary)', label: 'جرد دوري مقدم' },
  high_loss:           { icon: AlertTriangle, color: 'var(--danger)', label: 'فاقد مرتفع' },
  supply_approved:     { icon: CheckCheck, color: 'var(--secondary)', label: 'توريد موافق عليه' },
  part_replacement_due: { icon: AlertTriangle, color: 'var(--warning)', label: 'قطعة غيار تقترب من موعد الاستبدال' },
  part_replacement_overdue: { icon: AlertTriangle, color: 'var(--danger)', label: 'قطعة غيار تجاوزت موعد الاستبدال' },
  service_due_soon:    { icon: AlertTriangle, color: 'var(--warning)', label: 'صيانة دورية مستحقة قريباً' },
  service_due:         { icon: AlertTriangle, color: 'var(--warning)', label: 'صيانة دورية مستحقة الآن' },
  service_overdue:     { icon: AlertTriangle, color: 'var(--danger)',  label: 'صيانة دورية متأخرة' },
  system:              { icon: Info,     color: 'var(--primary)', label: 'إشعار نظام' },
};

const severityStyle = {
  danger:  { background: 'hsla(4,84%,60%,0.08)',  border: 'hsla(4,84%,60%,0.2)' },
  warning: { background: 'hsla(38,92%,55%,0.08)', border: 'hsla(38,92%,55%,0.2)' },
  info:    { background: 'hsla(217,91%,60%,0.06)', border: 'hsla(217,91%,60%,0.15)' },
};

export const getNotificationId = (notification) => notification?.id || notification?._id;

function Notifications({ lang, onRead }) {
  const { t } = useTranslation();
  const currentUser = useAuthStore((s) => s.user);
  const navigate = useNavigate();
  const ar = lang === 'ar';
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filter, setFilter] = useState('all'); // all | unread | danger | warning
  const showToast = useToastStore((s) => s.showToast);

  const fetchNotifs = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await client.get('/api/notifications');
      setNotifications(res.data.notifications || []);
      setUnreadCount(res.data.unreadCount || 0);
    } catch (err) {
      setError(err.response?.data?.msg || (ar ? 'فشل تحميل الإشعارات' : 'Failed to load notifications'));
      showToast(err.response?.data?.msg || (ar ? 'فشل تحميل الإشعارات' : 'Failed to load notifications'), 'error');
    } finally { setLoading(false); }
  };

  useEffect(() => { fetchNotifs(); }, []);

  useEffect(() => {
    const onCreated = (notification) => {
      setNotifications(prev => [notification, ...prev.filter(n => getNotificationId(n) !== getNotificationId(notification))]);
      setUnreadCount(count => count + 1);
    };
    const onRead = ({ id }) => setNotifications(prev => prev.map(n => getNotificationId(n) === id ? { ...n, isRead: true } : n));
    socket.on('NotificationCreated', onCreated);
    socket.on('NotificationRead', onRead);
    socket.on('AllNotificationsRead', fetchNotifs);
    return () => {
      socket.off('NotificationCreated', onCreated);
      socket.off('NotificationRead', onRead);
      socket.off('AllNotificationsRead', fetchNotifs);
    };
  }, []);

  const userId = currentUser?.id;

  const markRead = async (id) => {
    if (!id) return;
    try {
      await client.patch(`/api/notifications/${id}/read`);
      setNotifications(prev => prev.map(n => getNotificationId(n) === id ? { ...n, isRead: true } : n));
      setUnreadCount(c => Math.max(0, c - 1));
      onRead?.();
    } catch (err) {
      if (err.response?.status === 404) {
        setNotifications(prev => prev.filter(n => getNotificationId(n) !== id));
        setUnreadCount(c => Math.max(0, c - 1));
        return;
      }
      throw err;
    }
  };

  const markAllRead = async () => {
    await client.patch('/api/notifications/read-all');
    fetchNotifs();
  };

  const deleteNotif = async (id) => {
    if (!id) return;
    try {
      await client.delete(`/api/notifications/${id}`);
    } catch (err) {
      if (err.response?.status !== 404) throw err;
    }
    setNotifications(prev => prev.filter(n => getNotificationId(n) !== id));
  };

  const isUnread = (n) => !n.isRead;

  const openRelatedEntity = (notification) => {
    const routes = {
      FuelTank: '/tanks',
      Oil: '/oils',
      InstalledPart: '/maintenance/installed-parts',
      Generator: '/maintenance/service',
    };
    const route = routes[notification.relatedEntityType];
    if (route) navigate(route);
  };

  const filtered = notifications.filter(n => {
    if (filter === 'unread') return isUnread(n);
    if (filter === 'danger') return n.severity === 'danger';
    if (filter === 'warning') return n.severity === 'warning';
    return true;
  });

  const dangerCount = notifications.filter(n => n.severity === 'danger' && isUnread(n)).length;

  const formatTime = (date) => {
    const d = new Date(date);
    const now = new Date();
    const diff = Math.floor((now - d) / 1000);
    if (diff < 60) return 'الآن';
    if (diff < 3600) return `منذ ${Math.floor(diff / 60)} دقيقة`;
    if (diff < 86400) return `منذ ${Math.floor(diff / 3600)} ساعة`;
    return d.toLocaleDateString('ar-YE-u-nu-latn');
  };

  return (
    <div>
      {/* Header */}
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <Bell size={22} style={{ color: unreadCount > 0 ? 'var(--danger)' : 'var(--primary)' }} />
              {ar ? 'الإشعارات' : 'Notifications'}
              {unreadCount > 0 && (
                <span style={{
                  background: 'var(--danger)', color: '#fff',
                  fontSize: 12, fontWeight: 700, padding: '2px 10px', borderRadius: 99,
                }}>
                  {unreadCount} {ar ? 'جديد' : 'new'}
                </span>
              )}
            </h1>
            <p>{ar ? 'تنبيهات ومستجدات النظام' : 'System alerts and updates'}</p>
          </div>
          {unreadCount > 0 && (
            <button className="btn btn-ghost btn-sm" onClick={markAllRead}>
              <CheckCheck size={15} />
              {ar ? 'تعليم الكل كمقروء' : 'Mark all read'}
            </button>
          )}
        </div>
      </div>

      {/* KPI */}
      {dangerCount > 0 && (
        <div style={{
          background: 'var(--danger-glow)', border: '1px solid hsla(4,84%,60%,0.3)',
          borderRadius: 'var(--radius)', padding: '14px 20px', marginBottom: 20,
          display: 'flex', alignItems: 'center', gap: 12,
        }}>
          <AlertTriangle size={20} style={{ color: 'var(--danger)', flexShrink: 0 }} />
          <div>
            <div style={{ fontWeight: 700, color: 'var(--danger)' }}>
              {dangerCount} {ar ? 'تنبيه عاجل يتطلب اهتمامك' : 'urgent alerts require your attention'}
            </div>
            <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 2 }}>
              {ar ? 'يشمل تنبيهات نقص الوقود والزيوت وقراءات مرفوضة' : 'Includes low fuel, oil alerts and rejected readings'}
            </div>
          </div>
        </div>
      )}

      {/* Filters */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 20, flexWrap: 'wrap' }}>
        {[
          { key: 'all', label: ar ? 'الكل' : 'All' },
          { key: 'unread', label: ar ? 'غير مقروء' : 'Unread' },
          { key: 'danger', label: ar ? 'تنبيهات عاجلة' : 'Urgent' },
          { key: 'warning', label: ar ? 'تحذيرات' : 'Warnings' },
        ].map(f => (
          <button key={f.key} onClick={() => setFilter(f.key)}
            className={`btn btn-sm ${filter === f.key ? 'btn-primary' : 'btn-ghost'}`}>
            {f.label}
          </button>
        ))}
      </div>

      {/* List */}
      <div className="section-card">
        {error && (
          <div className="empty-state">
            <AlertTriangle size={48} style={{ color: 'var(--danger)' }} />
            <h3>{error}</h3>
            <button className="btn btn-sm" onClick={fetchNotifs}>{ar ? 'إعادة المحاولة' : 'Retry'}</button>
          </div>
        )}
        {!error && loading ? (
          <div className="spinner-wrap"><div className="ft-spinner" /></div>
        ) : filtered.length === 0 ? (
          <div className="empty-state">
            <Bell size={48} />
            <h3>{ar ? 'لا توجد إشعارات' : 'No notifications'}</h3>
          </div>
        ) : (
          <div>
            {filtered.map((notif, idx) => {
              const cfg = typeConfig[notif.notificationType] || typeConfig.system;
              const Icon = cfg.icon;
              const unread = isUnread(notif);
              const sStyle = severityStyle[notif.severity] || severityStyle.info;

              return (
                <div key={getNotificationId(notif)} onClick={() => unread && markRead(getNotificationId(notif))}
                  style={{
                    display: 'flex', alignItems: 'flex-start', gap: 14,
                    padding: '16px 20px',
                    borderBottom: idx < filtered.length - 1 ? '1px solid var(--border)' : 'none',
                    background: unread ? sStyle.background : 'transparent',
                    borderRight: unread && ar ? `3px solid ${cfg.color}` : 'none',
                    borderLeft: unread && !ar ? `3px solid ${cfg.color}` : 'none',
                    cursor: unread ? 'pointer' : 'default',
                    transition: 'background 0.2s',
                  }}>
                  {/* Icon */}
                  <div style={{
                    width: 38, height: 38, borderRadius: 10, flexShrink: 0,
                    background: `${cfg.color}20`,
                    display: 'flex', alignItems: 'center', justifyContent: 'center',
                  }}>
                    <Icon size={18} style={{ color: cfg.color }} />
                  </div>

                  {/* Content */}
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 3 }}>
                      <span style={{ fontWeight: unread ? 700 : 500, color: 'var(--text)', fontSize: 14 }}>
                        {isTranslationKey(notif.title) ? t(notif.title) : notif.title}
                      </span>
                      {unread && (
                        <span style={{
                          width: 8, height: 8, borderRadius: '50%',
                          background: cfg.color, flexShrink: 0,
                        }} />
                      )}
                    </div>
                    <div style={{ fontSize: 13, color: 'var(--text-muted)', lineHeight: 1.5 }}>
                      {isTranslationKey(notif.message) ? t(notif.message) : notif.message}
                    </div>
                    <div style={{ fontSize: 11, color: 'var(--text-faint)', marginTop: 6 }}>
                      {formatTime(notif.createdAt)}
                      {notif.relatedStation && (
                         <span style={{ marginInlineStart: 8 }}>
                          · {notif.relatedStation.name}
                        </span>
                      )}
                    </div>
                    {notif.relatedEntityType && (
                      <button className="btn btn-ghost btn-sm" onClick={e => { e.stopPropagation(); openRelatedEntity(notif); }} style={{ marginTop: 8 }}>
                        {ar ? 'فتح العنصر المرتبط' : 'Open related item'}
                      </button>
                    )}
                  </div>

                  {/* Delete */}
                  <button onClick={e => { e.stopPropagation(); deleteNotif(getNotificationId(notif)); }}
                    className="btn btn-ghost btn-sm btn-icon"
                    style={{ flexShrink: 0, opacity: 0.5 }}
                    title={ar ? 'حذف' : 'Delete'}>
                    <Trash2 size={14} />
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

const isTranslationKey = (value) => typeof value === 'string' &&
  (value.startsWith('operational.') || value.startsWith('maintenanceService.'));

export default Notifications;
