import React, { useEffect, useState, useCallback, useMemo } from 'react';
import {
  Plus, Search, Edit2, Trash2, Cpu, X, Save,
  Wifi, WifiOff, AlertCircle, CheckCircle,
  Activity, RefreshCw, Settings, Zap, Clock, Shield, Unlink
} from 'lucide-react';
import client from '../api/client';
import socket from '../socket';
import ConfirmModal from '../components/ConfirmModal';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';

const CONTROLLER_MODELS = [
  { value: 'DSE8610', label: 'DSE 8610' },
  { value: 'DSE8920', label: 'DSE 8920' },
];

const REGISTER_TYPES = [
  { value: 'FC03', label: 'FC03 — Holding Registers', label_ar: 'FC03 — سجلات الاحتفاظ' },
  { value: 'FC04', label: 'FC04 — Input Registers',   label_ar: 'FC04 — سجلات الإدخال' },
];

const ENERGY_UNITS = [
  { value: 'kWh', label: 'kWh' },
  { value: 'VAR', label: 'VAR' },
];

const CONN_STATUS = {
  connected:    { label_ar: 'متصلة',         label_en: 'Connected'    },
  disconnected: { label_ar: 'غير متصلة',     label_en: 'Disconnected' },
  error:        { label_ar: 'خطأ في الاتصال', label_en: 'Error'        },
  unknown:      { label_ar: 'غير محددة',     label_en: 'Unknown'      },
};

const INIT_FORM = {
  controllerType: 'DSE8610', serialNumber: '', generatorId: '',
  ipAddress: '', port: 502, modbusUnitId: 1,
  registerType: 'FC03', autoConnect: true,
  energyReading: 0, energyUnit: 'kWh', operatingHours: 0,
  isActive: true,
};

function ConnBadge({ status, ar }) {
  const cfg = CONN_STATUS[status] || CONN_STATUS.unknown;
  const isOn = status === 'connected';
  return (
    <span className={`badge ${isOn ? 'badge-success' : status === 'error' ? 'badge-danger' : 'badge-muted'}`}>
      <span className={`status-dot ${isOn ? 'online' : 'offline'}`} />
      {ar ? cfg.label_ar : cfg.label_en}
    </span>
  );
}

function Controllers({ lang, t }) {
  const ar = lang === 'ar';

  const [items,      setItems]      = useState([]);
  const [generators, setGenerators] = useState([]);
  const [loading,    setLoading]    = useState(true);
  const [saving,     setSaving]     = useState(false);
  const [testing,    setTesting]    = useState(null);
  const [testResult, setTestResult] = useState({});
  const [error,      setError]      = useState(null);
  const [search,     setSearch]     = useState('');
  const [showModal,  setShowModal]  = useState(false);
  const [editId,     setEditId]     = useState(null);
  const [form,       setForm]       = useState(INIT_FORM);
  const [activeTab,  setActiveTab]  = useState('basic');
  const [confirmModal, setConfirmModal] = useState({ open: false, id: null, action: null });

  const load = useCallback(async () => {
    setLoading(true); setError(null);
    try {
      const [ctrlRes, genRes] = await Promise.all([
        client.get('/api/controllers'),
        client.get('/api/generators'),
      ]);
      const cData = ctrlRes.data;
      setItems(Array.isArray(cData) ? cData : (cData.controllers || []));
      const gData = genRes.data;
      setGenerators(Array.isArray(gData) ? gData : (gData.data || []));
    } catch {
      setError(ar ? 'فشل تحميل البيانات' : 'Failed to load data');
    } finally { setLoading(false); }
  }, [ar]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    const onStatusChanged = (data) => setItems(prev => prev.map(i => i.id === data.id ? { ...i, connectionStatus: data.status ?? data.connectionStatus } : i));
    const onCreated = () => load();
    const onUpdated = () => load();
    const onDeleted = (data) => setItems(prev => prev.filter(i => i.id !== data.id));
    socket.on('ControllerConnectionStatusChanged', onStatusChanged);
    socket.on('ControllerCreated', onCreated);
    socket.on('ControllerUpdated', onUpdated);
    socket.on('ControllerDeleted', onDeleted);
    return () => {
      socket.off('ControllerConnectionStatusChanged', onStatusChanged);
      socket.off('ControllerCreated', onCreated);
      socket.off('ControllerUpdated', onUpdated);
      socket.off('ControllerDeleted', onDeleted);
    };
  }, [load]);

  const openAdd  = () => { setForm(INIT_FORM); setEditId(null); setActiveTab('basic'); setError(null); setShowModal(true); };
  const openEdit = (item) => {
    setForm({
      controllerType: item.controllerType || 'DSE8610', serialNumber: item.serialNumber || '',
      generatorId: item.generatorId || '', ipAddress: item.ipAddress || '',
      port: item.port ?? 502, modbusUnitId: item.modbusUnitId ?? 1,
      registerType: item.registerType || 'FC03', autoConnect: item.autoConnect ?? true,
      energyReading: item.energyReading ?? 0, energyUnit: item.energyUnit || 'kWh',
      operatingHours: item.operatingHours ?? 0,
      isActive: item.isActive !== false,
    });
    setEditId(item.id); setActiveTab('basic'); setError(null); setShowModal(true);
  };
  const closeModal = () => { setShowModal(false); setError(null); };
  const fld = (k, v) => setForm(f => ({ ...f, [k]: v }));

  const handleSubmit = async () => {
    if (!form.serialNumber.trim()) { setError(ar ? 'الرقم التسلسلي مطلوب' : 'Serial number is required'); return; }
    if (form.ipAddress && !/^(\d{1,3}\.){3}\d{1,3}$/.test(form.ipAddress.trim())) {
      setError(ar ? 'صيغة IP غير صحيحة' : 'Invalid IP format'); return;
    }
    if (form.generatorId && linkedGeneratorIds[Number(form.generatorId)]) {
      setError(ar ? 'المولد المحدد مرتبطة بشاشة أخرى بالفعل' : 'Selected generator is already linked to another controller');
      return;
    }
    const modelLabel = CONTROLLER_MODELS.find(m => m.value === form.controllerType)?.label || form.controllerType;
    const genName = generators.find(g => g.id === Number(form.generatorId))?.name || '';
    const autoName = genName ? `${genName} — ${modelLabel}` : `${modelLabel} — ${form.serialNumber}`;
    const autoCode = `CTRL-${form.serialNumber.replace(/\s+/g, '-').toUpperCase()}`;
    setSaving(true); setError(null);
    try {
      const payload = {
        name: autoName, code: autoCode, controllerType: form.controllerType,
        generatorId: form.generatorId ? Number(form.generatorId) : null,
        ipAddress: form.ipAddress || null, port: Number(form.port) || 502,
        modbusUnitId: Number(form.modbusUnitId) || 1, serialNumber: form.serialNumber || null,
        firmwareVersion: null, energyReading: Number(form.energyReading) || 0,
        operatingHours: Number(form.operatingHours) || 0, registerType: form.registerType || 'FC03',
        autoConnect: form.autoConnect, energyUnit: form.energyUnit || 'kWh',
        isActive: form.isActive,
      };
      if (editId) await client.put(`/api/controllers/${editId}`, payload);
      else        await client.post('/api/controllers', payload);
      closeModal(); await load();
    } catch (err) {
      setError(err.response?.data?.msg || (ar ? 'حدث خطأ أثناء الحفظ' : 'Error while saving'));
    } finally { setSaving(false); }
  };

  const handleDelete = async (id) => {
    setConfirmModal({ open: true, id, action: 'delete' });
  };
  const confirmDelete = async () => {
    const id = confirmModal.id;
    setConfirmModal({ open: false, id: null, action: null });
    try { await client.delete(`/api/controllers/${id}`); await load(); }
    catch (err) {
      if (err.response?.status === 404) setItems(prev => prev.filter(i => i.id !== id));
      else alert(err.response?.data?.msg || 'Error');
    }
  };

  const handleTest = async (item) => {
    setTesting(item.id); setTestResult(prev => ({ ...prev, [item.id]: null }));
    try {
      await client.post(`/api/controllers/${item.id}/connection-status`, { status: 'unknown' });
      setTestResult(prev => ({
        ...prev, [item.id]: { ok: true,
          msg: ar ? `الشاشة مهيأة على ${item.ipAddress}:${item.port}` : `Configured at ${item.ipAddress}:${item.port}`,
        },
      }));
    } catch (err) {
      setTestResult(prev => ({
        ...prev, [item.id]: { ok: false, msg: err.response?.data?.msg || (ar ? 'فشل الاختبار' : 'Test failed') },
      }));
    } finally { setTesting(null); }
  };

  const filtered = items.filter(i =>
    !search || i.name?.toLowerCase().includes(search.toLowerCase()) ||
    i.code?.toLowerCase().includes(search.toLowerCase()) ||
    i.serialNumber?.toLowerCase().includes(search.toLowerCase()) ||
    i.ipAddress?.includes(search) || i.generatorName?.toLowerCase().includes(search.toLowerCase())
  );

  const total      = items.length;
  const connected  = items.filter(i => i.connectionStatus === 'connected').length;
  const withErrors = items.filter(i => i.connectionStatus === 'error').length;
  const linked     = items.filter(i => i.generatorId).length;

  const linkedGeneratorIds = useMemo(() => {
    const map = {};
    items.forEach(i => {
      if (i.generatorId && i.id !== editId) {
        map[i.generatorId] = i.id;
      }
    });
    return map;
  }, [items, editId]);

  const handleUnlink = async (controllerId) => {
    setConfirmModal({ open: true, id: controllerId, action: 'unlink' });
  };
  const confirmUnlink = async () => {
    const controllerId = confirmModal.id;
    setConfirmModal({ open: false, id: null, action: null });
    try {
      await client.patch(`/api/controllers/${controllerId}/assign-generator`, { generatorId: null });
      await load();
    } catch (err) {
      setError(err.response?.data?.msg || (ar ? 'حدث خطأ أثناء إلغاء الارتباط' : 'Error unlinking'));
    }
  };

  const TABS = [
    { key: 'basic',      icon: <Settings size={14} />, label_ar: 'البيانات الأساسية',  label_en: 'Basic Info' },
    { key: 'connection', icon: <Wifi size={14} />,      label_ar: 'إعدادات الاتصال',    label_en: 'Connection' },
    { key: 'readings',   icon: <Zap size={14} />,       label_ar: 'القراءات اليدوية',   label_en: 'Readings' },
  ];

  return (
    <div>
      <PageHeader
        title={ar ? 'شاشات التحكم' : 'Generator Controllers'}
        subtitle={ar ? 'إدارة شاشات التحكم وإعدادات Modbus TCP للربط التلقائي' : 'Manage controllers and Modbus TCP settings for auto-connect'}
        actions={
          <div style={{ display: 'flex', gap: 8 }}>
            <Button variant="ghost" size="sm" icon={RefreshCw} onClick={load} />
            <Button icon={Plus} onClick={openAdd}>{ar ? 'إضافة شاشة' : 'Add Screen'}</Button>
          </div>
        }
      />

      {/* KPI Cards */}
      <div className="kpi-grid">
        {[
          { label: ar ? 'الإجمالي'     : 'Total',   value: total,      icon: <Cpu size={20} />,        colorClass: 'primary' },
          { label: ar ? 'مرتبطة بمولد'  : 'Linked',  value: linked,     icon: <Activity size={20} />,   colorClass: 'secondary' },
          { label: ar ? 'متصلة الآن'    : 'Online',  value: connected,  icon: <Wifi size={20} />,       colorClass: 'secondary' },
          { label: ar ? 'بها أخطاء'     : 'Errors',  value: withErrors, icon: <AlertCircle size={20} />,colorClass: 'danger' },
        ].map((s, i) => (
          <div key={i} className="kpi-card" style={{ '--kpi-color': `var(--${s.colorClass === 'secondary' ? 'secondary' : s.colorClass === 'danger' ? 'danger' : 'primary'})` }}>
            <div className={`kpi-icon ${s.colorClass}`}>
              {s.icon}
            </div>
            <div>
              <div className="kpi-value">{s.value}</div>
              <div className="kpi-label">{s.label}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Search */}
      <div style={{ marginBottom: 20 }}>
        <div className="search-bar" style={{ maxWidth: 400 }}>
          <Search size={16} />
          <input className="form-control"
            placeholder={ar ? 'بحث بالاسم، الرقم التسلسلي، أو IP...' : 'Search name, serial, or IP...'}
            value={search} onChange={e => setSearch(e.target.value)} />
        </div>
      </div>

      {/* Cards */}
      {loading ? (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      ) : error && !showModal ? (
        <div className="alert-inline alert-inline-danger">{error}</div>
      ) : filtered.length === 0 ? (
        <div className="empty-state"><Cpu size={48} /><h3>{t.noData}</h3></div>
      ) : (
        <div className="grid-auto">
          {filtered.map(item => {
            const tr = testResult[item.id];
            return (
              <div key={item.id} className="ctrl-card">
                {/* Card top */}
                <div className="ctrl-card-top">
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div className="ctrl-card-name">{item.name}</div>
                    <div className="ctrl-card-model">{item.controllerType}</div>
                    {item.serialNumber && (
                      <div className="ctrl-card-sn">SN: {item.serialNumber}</div>
                    )}
                  </div>
                    <div className="ctrl-card-badge-stack">
                      <span className={`status-badge ${item.isActive !== false ? 'status-active' : 'status-inactive'}`}>{item.isActive !== false ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}</span>
                      <ConnBadge status={item.connectionStatus || 'unknown'} ar={ar} />
                    </div>
                </div>

                {/* Card body */}
                <div className="ctrl-card-body">
                  {/* Link badge */}
                  {item.generatorId ? (
                    <div className="ctrl-gen-badge">
                      <Activity size={11} />
                      {item.generatorName || (ar ? 'مرتبطة بمولد' : 'Linked to generator')}
                    </div>
                  ) : (
                    <div className="ctrl-no-gen-badge">
                      <WifiOff size={11} />
                      {ar ? 'غير مرتبطة' : 'Not linked'}
                    </div>
                  )}

                  {/* Stats grid */}
                   <div className="ctrl-card-stats">
                      {/* Energy */}
                      <div className="ctrl-stat">
                        <div className="ctrl-stat-label">{ar ? 'الطاقة' : 'Energy'}</div>
                        <div className="ctrl-stat-value">
                          {item.energyReading > 0
                            ? <>{item.energyReading.toLocaleString()} <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>{item.energyUnit || 'kWh'}</span></>
                            : '—'}
                        </div>
                      </div>
                      {/* Hours */}
                      <div className="ctrl-stat">
                        <div className="ctrl-stat-label">{ar ? 'الساعات' : 'Hours'}</div>
                        <div className="ctrl-stat-value">
                          {item.operatingHours > 0
                            ? <>{item.operatingHours.toLocaleString()} <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>{ar ? 'ساعة' : 'hrs'}</span></>
                            : '—'}
                        </div>
                      </div>
                    </div>

                  {/* Test result */}
                  {tr && (
                    <div className={`ctrl-test-result ${tr.ok ? 'ok' : 'err'}`}>
                      {tr.ok
                        ? <CheckCircle size={13} style={{ flexShrink: 0, marginTop: 1 }} />
                        : <AlertCircle  size={13} style={{ flexShrink: 0, marginTop: 1 }} />}
                      {tr.msg}
                    </div>
                  )}
                </div>

                {/* Card footer */}
                <div className="ctrl-card-footer">
                  {item.ipAddress && (
                    <Button variant="ghost" size="sm" icon={testing === item.id ? null : Wifi} onClick={() => handleTest(item)} disabled={testing === item.id}>
                      {testing === item.id ? <span className="ft-spinner" style={{ width: 12, height: 12 }} /> : null}
                      {ar ? 'اختبار' : 'Test'}
                    </Button>
                  )}
                  {item.generatorId && (
                    <Button variant="ghost" size="sm" icon={Unlink} onClick={() => handleUnlink(item.id)} style={{ color: 'var(--danger)' }}>
                      {ar ? 'إلغاء الارتباط' : 'Unlink'}
                    </Button>
                  )}
                  <Button variant="ghost" size="sm" icon={Edit2} onClick={() => openEdit(item)}>
                    {t.edit}
                  </Button>
                  <Button variant="danger" size="sm" icon={Trash2} onClick={() => handleDelete(item.id)}>
                    {t.delete}
                  </Button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* ══════════════ Modal ══════════════ */}
      {showModal && (
        <Modal
          open={showModal}
          onClose={closeModal}
          title={editId ? (ar ? 'تعديل بيانات الشاشة' : 'Edit Controller') : (ar ? 'إضافة شاشة جديدة' : 'Add New Controller')}
          maxWidth={640}
          footer={
            <>
              <Button variant="ghost" onClick={closeModal}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              <Button variant="primary" onClick={handleSubmit} disabled={saving} icon={Save}>
                {saving
                  ? ar ? 'جاري الحفظ...' : 'Saving...'
                  : editId ? (ar ? 'تحديث الشاشة' : 'Update Screen') : (ar ? 'إضافة شاشة' : 'Add Screen')}
              </Button>
            </>
          }
        >
          {error && <div className="alert-inline alert-inline-danger" style={{ marginBottom: 16 }}>{error}</div>}

          {/* Tab bar */}
          <div className="ctrl-tab-bar">
            {TABS.map(tab => (
              <button key={tab.key} className={`ctrl-tab-btn ${activeTab === tab.key ? 'active' : ''}`}
                onClick={() => setActiveTab(tab.key)}>
                {tab.icon}
                {ar ? tab.label_ar : tab.label_en}
              </button>
            ))}
          </div>

          {/* Tab 1 — Basic Info */}
          <div className={`ctrl-tab-content ${activeTab === 'basic' ? 'active' : ''}`}>
             <div className="ctrl-tab-section-title">{ar ? 'معلومات الجهاز' : 'Device Information'}</div>

            {/* Manufacturer — read-only */}
            <div className="ctrl-readonly-field" style={{ marginBottom: 16 }}>
              <div>
                <div className="ctrl-readonly-label">{ar ? 'الشركة المصنعة' : 'Manufacturer'}</div>
              </div>
              <div className="ctrl-readonly-value">Deep Sea Electronics</div>
            </div>

            {/* Model */}
            <FormField label={ar ? 'الموديل *' : 'Model *'} required>
              <select className="form-select" value={form.controllerType}
                onChange={e => fld('controllerType', e.target.value)}>
                {CONTROLLER_MODELS.map(m => <option key={m.value} value={m.value}>{m.label}</option>)}
              </select>
            </FormField>

            {/* Serial number */}
            <FormField label={ar ? 'الرقم التسلسلي *' : 'Serial Number *'} required error={!form.serialNumber.trim() ? (ar ? 'الرقم التسلسلي مطلوب' : 'Serial number is required') : null}>
              <Input value={form.serialNumber}
                onChange={e => fld('serialNumber', e.target.value)}
                placeholder="e.g. DSE-8610-001" />
            </FormField>

            <div className="ctrl-tab-divider" />
            <div className="ctrl-tab-section-title">{ar ? 'الحالة' : 'Status'}</div>

            <div className="ctrl-toggle-wrap">
              <div>
                <div className="ctrl-toggle-label">{ar ? 'نشط' : 'Active'}</div>
                <div className="ctrl-toggle-sub">
                  {ar ? 'ظهر الشاشة في قائمة المراقبة' : 'Visible in monitoring list'}
                </div>
              </div>
              <label className="ctrl-toggle">
                <input type="checkbox" checked={form.isActive}
                  onChange={e => fld('isActive', e.target.checked)} />
                <span className="slider"></span>
              </label>
            </div>

            <div className="ctrl-tab-divider" />
            <div className="ctrl-tab-section-title">{ar ? 'المولد المرتبط' : 'Assigned Generator'}</div>

            <FormField label={ar ? 'المولد المرتبط' : 'Assigned Generator'}>
              <select className="form-select" value={form.generatorId}
                onChange={e => fld('generatorId', e.target.value)}>
                <option value="">{ar ? '— غير مرتبطة —' : '— Not linked —'}</option>
                {generators.map(g => {
                  const linkedToOther = linkedGeneratorIds[g.id];
                  return (
                    <option key={g.id} value={g.id} disabled={!!linkedToOther}>
                      {g.name} {linkedToOther ? (ar ? '(مرتبطة بشاشة أخرى)' : '(Linked to another)') : ''}
                    </option>
                  );
                })}
              </select>
            </FormField>
             {form.generatorId && linkedGeneratorIds[Number(form.generatorId)] && (
               <div className="form-help" style={{ color: 'var(--danger)', marginTop: 6 }}>
                 {ar ? 'هذا المولد مرتبطة بشاشة أخرى بالفعل' : 'This generator is already linked to another controller'}
               </div>
             )}
          </div>

          {/* Tab 2 — Connection */}
          <div className={`ctrl-tab-content ${activeTab === 'connection' ? 'active' : ''}`}>
            <div className="ctrl-infobox">
              <Shield size={15} />
              <span>
                <span className="ctrl-infobox-strong">Modbus TCP</span>
                {ar ? ' — تأكد من تفعيل Modbus في إعدادات الشاشة' : ' — Enable Modbus in controller settings'}
              </span>
            </div>

            <div className="ctrl-tab-section-title">{ar ? 'عنوان الشبكة' : 'Network Address'}</div>

            <div className="form-row" style={{ marginBottom: 16 }}>
              <FormField label={ar ? 'عنوان IP' : 'IP Address'} error={form.ipAddress && !/^(\d{1,3}\.){3}\d{1,3}$/.test(form.ipAddress.trim()) ? (ar ? 'صيغة IP غير صحيحة' : 'Invalid IP format') : null}>
                <Input dir="ltr" value={form.ipAddress}
                  onChange={e => fld('ipAddress', e.target.value.trim())}
                  placeholder="192.168.1.100" />
              </FormField>
              <FormField label={ar ? 'المنفذ' : 'Port'}>
                <Input type="number" value={form.port}
                  onChange={e => fld('port', parseInt(e.target.value, 10) || 502)}
                  min={1} max={65535} />
              </FormField>
            </div>

            <div className="ctrl-tab-divider" />
            <div className="ctrl-tab-section-title">{ar ? 'إعدادات Modbus' : 'Modbus Settings'}</div>

            <FormField label={
              <span>
                Unit ID
                <span style={{ fontSize: 11, color: 'var(--text-muted)', marginInlineStart: 6 }}>
                  ({ar ? 'عادةً 1' : 'usually 1'})
                </span>
              </span>
            }>
              <Input type="number" value={form.modbusUnitId}
                onChange={e => fld('modbusUnitId', parseInt(e.target.value, 10) || 1)}
                min={1} max={247} />
            </FormField>

            <FormField label={ar ? 'نوع السجلات' : 'Register Type'}>
              <select className="form-select" value={form.registerType}
                onChange={e => fld('registerType', e.target.value)}>
                {REGISTER_TYPES.map(r => (
                  <option key={r.value} value={r.value}>{ar ? r.label_ar : r.label}</option>
                ))}
              </select>
            </FormField>

            <div className="ctrl-tab-divider" />
            <div className="ctrl-tab-section-title">{ar ? 'الاتصال التلقائي' : 'Auto-Connect'}</div>

            <div className="ctrl-toggle-wrap">
              <div>
                <div className="ctrl-toggle-label">{ar ? 'تفعيل الاتصال التلقائي' : 'Enable Auto-connect'}</div>
                <div className="ctrl-toggle-sub">
                  {ar ? 'الخدمة ستتصل تلقائياً بالشاشة عبر Modbus TCP' : 'Service will auto-connect via Modbus TCP'}
                </div>
              </div>
              <label className="ctrl-toggle">
                <input type="checkbox" checked={form.autoConnect}
                  onChange={e => fld('autoConnect', e.target.checked)} />
                <span className="slider"></span>
              </label>
            </div>
          </div>

          {/* Tab 3 — Readings */}
          <div className={`ctrl-tab-content ${activeTab === 'readings' ? 'active' : ''}`}>
            <div className="ctrl-infobox">
              <Clock size={15} />
              {ar
                ? 'أدخل القراءات اليدوية من العداد. يتم تحديثها يدوياً عند قراءة العداد.'
                : 'Enter manual meter readings. Updated manually when reading the meter.'}
            </div>

            <div className="ctrl-tab-section-title">{ar ? 'قراءة عداد الطاقة' : 'Energy Reading'}</div>

            <div className="form-row" style={{ marginBottom: 16 }}>
              <FormField label={ar ? 'القراءة الحالية' : 'Current Reading'}>
                <Input type="number" step="0.01" value={form.energyReading}
                  onChange={e => fld('energyReading', parseFloat(e.target.value) || 0)}
                  placeholder="0" />
              </FormField>
              <FormField label={ar ? 'الوحدة' : 'Unit'}>
                <select className="form-select" value={form.energyUnit}
                  onChange={e => fld('energyUnit', e.target.value)}>
                  {ENERGY_UNITS.map(u => <option key={u.value} value={u.value}>{u.label}</option>)}
                </select>
              </FormField>
            </div>

            <div className="ctrl-tab-divider" />
            <div className="ctrl-tab-section-title">{ar ? 'ساعات التشغيل' : 'Operating Hours'}</div>

            <FormField label={ar ? 'ساعات التشغيل الكلية' : 'Total Operating Hours'}>
              <Input type="number" step="0.01" value={form.operatingHours}
                onChange={e => fld('operatingHours', parseFloat(e.target.value) || 0)}
                placeholder="0" />
            </FormField>
          </div>
        </Modal>
      )}

      <ConfirmModal
        open={confirmModal.open}
        title={ar ? 'تأكيد الحذف' : 'Confirm Delete'}
        message={ar ? 'هل تريد حذف هذه الشاشة؟' : 'Delete this controller?'}
        confirmText={ar ? 'حذف' : 'Delete'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        danger
        onConfirm={confirmDelete}
        onCancel={() => setConfirmModal({ open: false, id: null, action: null })}
      />
      <ConfirmModal
        open={confirmModal.open && confirmModal.action === 'unlink'}
        title={ar ? 'تأكيد إلغاء الارتباط' : 'Confirm Unlink'}
        message={ar ? 'هل تريد إلغاء ارتباط المولد بهذه الشاشة؟' : 'Unlink generator from this controller?'}
        confirmText={ar ? 'إلغاء الارتباط' : 'Unlink'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        onConfirm={confirmUnlink}
        onCancel={() => setConfirmModal({ open: false, id: null, action: null })}
      />
    </div>
  );
}

export default Controllers;
