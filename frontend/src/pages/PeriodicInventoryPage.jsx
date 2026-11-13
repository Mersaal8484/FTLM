import React, { useMemo, useState, useEffect } from 'react';
import client from '../api/client';
import { 
  PackageSearch, Plus, TrendingUp, CheckCircle, 
  AlertTriangle, Info, FileText,   Droplets, Printer
} from 'lucide-react';
import ConfirmModal from '../components/ConfirmModal';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';
import useTenantBranding from '../hooks/useTenantBranding';

export function canReviewInventory(currentUser, inventory) {
  const role = currentUser?.role;
  const status = inventory?.status || 'draft';
  return (role === 'admin' || role === 'supervisor') && ['draft', 'submitted'].includes(status);
}

export function canSubmitInventory(currentUser, inventory) {
  const status = inventory?.status || 'draft';
  return currentUser?.role === 'inventory_head' && ['draft', 'rejected'].includes(status);
}

export function getInventoryPrintDate(inventory) {
  if (!inventory?.inventoryDate) return '';
  const d = new Date(inventory.inventoryDate);
  return `${d.getFullYear()}-${(d.getMonth() + 1).toString().padStart(2, '0')}-${d.getDate().toString().padStart(2, '0')}`;
}

export function getMissingFuelReason(inventory) {
  return inventory?.missingFuelReason || inventory?.notes || '';
}

export function formatEnergyReading(value, noMeter) {
  const numeric = parseFloat(value);
  if (Number.isFinite(numeric) && numeric > 0) return numeric.toLocaleString('en-US');
  return noMeter ? '-' : '0';
}

export function formatInventoryEfficiency(value) {
  const numeric = parseFloat(value);
  return Number.isFinite(numeric) && numeric > 0 ? `${numeric.toFixed(2)}%` : '—';
}

export function buildInventoryStatusPayload(inventory, status) {
  return {
    ...inventory,
    station: inventory?.stationId || inventory?.station?.id || inventory?.station || '',
    subStation: inventory?.subStationId || inventory?.subStation?.id || inventory?.subStation || null,
    missingFuelReason: getMissingFuelReason(inventory),
    status,
  };
}

function PeriodicInventoryPage({ lang, currentUser }) {
  const branding = useTenantBranding();
  const ar = lang === 'ar';
  
  // Permission helpers
  const perms = currentUser?.permissions?.inventory || {};
  const canAdd = currentUser?.role === 'admin' || perms.create || perms.add;
  const [inventories, setInventories] = useState([]);
  const [stations, setStations] = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [tanks, setTanks] = useState([]);
  const [oils, setOils] = useState([]);
  const [periodTypes, setPeriodTypes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showNew, setShowNew] = useState(false);
  const [saving, setSaving] = useState(false);
  const [status, setStatus] = useState(null);
  const [printDate, setPrintDate] = useState('');
  const [selectedInventory, setSelectedInventory] = useState(null);
  const [confirmModal, setConfirmModal] = useState({ open: false, inventory: null, newStatus: null });

  const [newForm, setNewForm] = useState(initialForm());

  function initialForm() {
    return {
      station: '',
      subStation: '',
      inventoryDate: new Date().toISOString().slice(0, 10),
      year: new Date().getFullYear(),
      month: new Date().getMonth() + 1,
      periodType: '', // default set after fetch from PeriodType lookup
      team: '',
      notes: '',           // replaces missingFuelReason
      fuelReadings: [],    // [{ tank, tankName, actualQty }]
      oilReadings: [],     // [{ oil, oilName, actualQty }]
    };
  }

  const fetchAll = async () => {
    setLoading(true);
    try {
      const [invRes, statRes, subRes, tankRes, oilRes, ptRes] = await Promise.all([
        client.get('/api/inventory'),
        client.get('/api/stations'),
        client.get('/api/sub-stations'),
        client.get('/api/fuel-tanks'),
        client.get('/api/oils'),
        client.get('/api/lookups/by-category/PeriodType'),
      ]);
      setInventories(invRes.data?.data || []);
      setStations(statRes.data || []);
      setSubStations((subRes.data || []).filter(s => s.isActive !== false));
      setTanks(tankRes.data || []);
      setOils(oilRes.data || []);
      setPeriodTypes(ptRes.data || []);
      // Set default periodType to first lookup if not set
      if (ptRes.data?.length > 0) {
        setNewForm(prev => prev.periodType ? prev : { ...prev, periodType: ptRes.data[0].id });
      }
    } catch {}
    finally { setLoading(false); }
  };

  useEffect(() => { fetchAll(); }, []);

  const getRefId = (value) => value?.id || value?._id || value || '';
  const getLocationLabel = (inv) => inv?.subStation?.name || inv?.station?.name || inv?.station || '—';
  const assignedSubStationIds = useMemo(
    () => new Set((currentUser?.assignedSubStations || []).map(String)),
    [currentUser?.assignedSubStations]
  );
  const canSeeAllSites = currentUser?.role === 'admin' || assignedSubStationIds.size === 0;
  const siteOptions = useMemo(() => subStations.filter(site => {
    const siteId = site.id || site._id;
    return canSeeAllSites || assignedSubStationIds.has(String(siteId));
  }).map(site => {
    const station = stations.find(s =>
      s.id === site.station_id ||
      s._id === site.station_id ||
      String(s.stationId) === String(site.station_id)
    );
    return {
      id: site.id || site._id,
      stationId: station?.id || station?._id || site.station_id,
      label: station?.name ? `${station.name} — ${site.name}` : site.name,
    };
  }), [stations, subStations, canSeeAllSites, assignedSubStationIds]);

  const handleLocationChange = (siteId) => {
    const site = siteOptions.find(s => s.id === siteId);
    setNewForm(f => ({
      ...f,
      station: site?.stationId || '',
      subStation: site?.id || '',
      fuelReadings: [],
      oilReadings: [],
    }));
  };

  // Update actual tank and oil readings structure when station is selected
  useEffect(() => {
    if (newForm.station) {
      const stationTanks = tanks.filter(t => {
        const tankStation = getRefId(t.station);
        const tankSubStation = getRefId(t.subStation);
        return newForm.subStation ? tankSubStation === newForm.subStation : tankStation === newForm.station;
      });
      const stationOils = oils.filter(o => {
        const oilStation = getRefId(o.station);
        const oilSubStation = getRefId(o.subStation);
        return newForm.subStation ? oilSubStation === newForm.subStation : oilStation === newForm.station;
      });
      setNewForm(f => ({
        ...f,
        fuelReadings: stationTanks.map(t => ({ tank: t.id || t._id, tankName: t.name, actualQty: '' })),
        oilReadings: stationOils.map(o => ({ oil: o.id || o._id, oilName: o.name, actualQty: '' }))
      }));
    }
  }, [newForm.station, newForm.subStation, tanks, oils]);

  const createInventory = async () => {
    if (!newForm.station || !newForm.subStation) { setStatus({ type: 'error', msg: ar ? 'يرجى اختيار الموقع' : 'Please select a location' }); return; }
    setSaving(true);
    try {
      await client.post('/api/inventory', {
        subStationId:  parseInt(newForm.subStation, 10),
        year:          parseInt(newForm.year, 10),
        month:         parseInt(newForm.month, 10),
        periodTypeLookupId: parseInt(newForm.periodType, 10),
        inventoryDate: newForm.inventoryDate,
        notes:         newForm.notes,
        fuelReadings:  newForm.fuelReadings.map(fr => ({
          fuelTankId: parseInt(fr.tank, 10) || null,
          fuelLookupId: 0,
          expectedQtyLiters: parseFloat(fr.expectedQty) || 0,
          actualQtyLiters: parseFloat(fr.actualQty) || 0,
        })),
        oilReadings:   newForm.oilReadings.map(or => ({
          oilTankId: parseInt(or.oil, 10) || null,
          oilLookupId: 0,
          expectedQtyLiters: parseFloat(or.expectedQty) || 0,
          actualQtyLiters: parseFloat(or.actualQty) || 0,
        })),
      });
      setShowNew(false);
      setNewForm(initialForm());
      fetchAll();
      setStatus({ type: 'success', msg: ar ? 'تم تسجيل الجرد الميداني بنجاح ✓' : 'Field inventory recorded successfully ✓' });
    } catch (err) { 
      setStatus({ type: 'error', msg: err.response?.data?.msg || 'Error' }); 
    } finally { setSaving(false); }
  };

  const updateActualInForm = (type, idx, val) => {
    const key = type === 'fuel' ? 'fuelReadings' : 'oilReadings';
    const list = [...newForm[key]];
    list[idx].actualQty = val;
    setNewForm({ ...newForm, [key]: list });
  };

  const updateStatus = (inventory, newStatus) => {
    setConfirmModal({ open: true, inventory, newStatus });
  };
  const confirmUpdateStatus = async () => {
    const { inventory, newStatus } = confirmModal;
    setConfirmModal({ open: false, inventory: null, newStatus: null });
    try {
      const id = inventory.id || inventory._id;
      await client.put(`/api/inventory/${id}`, buildInventoryStatusPayload(inventory, newStatus));
      fetchAll();
    } catch(err) { alert('Error updating status'); }
  };

  const months = ar 
    ? ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر']
    : ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

  // Helper to get total actual fuel for display in the table
  const getDisplayTotal = (inv, type) => {
    const arr = type === 'fuel' ? inv.fuelReadings : inv.oilReadings;
    if (!arr || arr.length === 0) return 0;
    return arr.reduce((sum, item) => sum + (parseFloat(item.actualQty) || 0), 0);
  };

  const formatLiters = (value) => {
    const numeric = parseFloat(value);
    return Number.isFinite(numeric) ? numeric.toLocaleString() : '0';
  };

  const getSavedFuelActualQty = (inv) => {
    const stored = parseFloat(inv?.totalFuelActualQty);
    return Number.isFinite(stored) ? stored : getDisplayTotal(inv, 'fuel');
  };

  const getOilInventoryItems = (inv) => {
    if (!Array.isArray(inv?.oilReadings)) return [];
    return inv.oilReadings.map((item, index) => ({
      key: item.oil || item.oilId || `${inv.id || inv._id || 'inventory'}-oil-${index}`,
      name: item.oilName || item.name || item.oilTypeName || item.typeName || '',
      actualQty: item.actualQty,
    }));
  };

  const getFuelInventoryItems = (inv) => {
    if (!Array.isArray(inv?.fuelReadings) || inv.fuelReadings.length === 0) return null;
    return inv.fuelReadings.map((item, index) => {
      const tank = tanks.find(t => (t._id || t.id) === item.tank);
      const fuelType = tank?.fuelType || '';
      const name = item.tankName || item.name || '';
      return {
        key: item.tank || `${inv.id || inv._id || 'inventory'}-fuel-${index}`,
        name,
        fuelType,
        actualQty: item.actualQty,
      };
    });
  };

  const getInventoryNotes = (inv) => inv?.notes || '';

  // Helper to format Date
  const formatDate = (dateStr) => {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return `${d.getFullYear()}/${(d.getMonth() + 1).toString().padStart(2, '0')}/${d.getDate().toString().padStart(2, '0')}`;
  };

  const printableDates = useMemo(() => {
    const dates = inventories
      .filter(inv => inv.status !== 'rejected')
      .map(getInventoryPrintDate)
      .filter(Boolean);
    return [...new Set(dates)].sort().reverse();
  }, [inventories]);

  const reportInventories = useMemo(() => {
    return inventories.filter(inv => !printDate || getInventoryPrintDate(inv) === printDate);
  }, [inventories, printDate]);

  return (
    <div className="periodic-inventory-page" style={{ animation: 'fadeIn 0.5s ease' }}>
      <PageHeader
        title={
          <span style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <PackageSearch size={28} style={{ color: 'var(--primary)' }} />
            {ar ? 'الجرد الدوري' : 'Periodic Inventory'}
          </span>
        }
        subtitle={ar ? 'مقارنة الأرصدة وإدخال الجرد الميداني' : 'Compare balances and enter field inventory'}
        actions={
          <div style={{ display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap' }}>
            <select className="form-select no-print" value={printDate} onChange={e => setPrintDate(e.target.value)} style={{ width: 190 }}>
              <option value="">{ar ? 'كل تواريخ الجرد' : 'All inventory dates'}</option>
              {printableDates.map(date => (
                <option key={date} value={date}>{date.replaceAll('-', '/')}</option>
              ))}
            </select>
            <Button
              variant="outline"
              icon={Printer}
              onClick={() => {
                const appRoot = document.querySelector('.app-root');
                const isLight = appRoot?.classList.contains('theme-light');
                if (isLight) appRoot.classList.remove('theme-light');
                setTimeout(() => { window.print(); if (isLight) appRoot.classList.add('theme-light'); }, 100);
              }}
            >
              {ar ? 'طباعة' : 'Print'}
            </Button>
            {canAdd && (
              <Button
                variant="primary"
                icon={Plus}
                onClick={() => { setShowNew(true); setStatus(null); }}
              >
                {ar ? 'جديد' : 'New'}
              </Button>
            )}
          </div>
        }
      />

      {status && (
        <div className={`alert ${status.type === 'success' ? 'alert-success' : 'alert-danger'}`}
          style={{ display: 'flex', gap: 10, alignItems: 'center', marginBottom: 20 }}>
          {status.type === 'success' ? <CheckCircle size={18} /> : <AlertTriangle size={18} />}
          <span style={{ fontWeight: 600 }}>{status.msg}</span>
        </div>
      )}

      {/* ── NEW INVENTORY MODAL ── */}
      {showNew && (
        <Modal
          open={showNew}
          onClose={() => setShowNew(false)}
          title={
            <span style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <FileText size={20} style={{ color: 'var(--primary)' }} />
              {ar ? 'بيانات الجرد الميداني الجديد' : 'New Field Inventory Data'}
            </span>
          }
          maxWidth={900}
          footer={
            <>
              <Button variant="ghost" onClick={() => setShowNew(false)}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              <Button variant="primary" onClick={createInventory} disabled={saving}>
                {saving ? '...' : (ar ? 'إنشاء مسودة الجرد' : 'Create Draft')}
              </Button>
            </>
          }
        >
          <div className="form-2col" style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 30 }}>
            
            {/* ── COL 1: Basic & Field Inventory ── */}
            <div>
              <div className="form-section-header"><Info size={14}/> {ar ? 'المعلومات الأساسية' : 'Basic Information'}</div>
              <FormField label={ar ? 'اسم الموقع *' : 'Location Name *'} required>
                <select className="form-select" value={newForm.subStation} onChange={e => handleLocationChange(e.target.value)}>
                  <option value="">{ar ? '— اختر الموقع —' : '— Select Location —'}</option>
                  {siteOptions.map(s => <option key={s.id} value={s.id}>{s.label}</option>)}
                </select>
              </FormField>
              <div className="form-row">
                <FormField label={ar ? 'تاريخ الجرد' : 'Inventory Date'}>
                  <Input type="date" value={newForm.inventoryDate} onChange={e => {
                    const newDate = e.target.value;
                    const yr = new Date(newDate).getFullYear();
                    setNewForm({...newForm, inventoryDate: newDate, year: yr});
                  }} />
                </FormField>
                <FormField label={ar ? 'فريق الجرد' : 'Inventory Team'}>
                  <Input value={newForm.team} onChange={e => setNewForm({...newForm, team: e.target.value})} placeholder={ar ? 'أسماء الفريق...' : 'Team names...'} />
                </FormField>
              </div>
              <div className="form-row">
                <FormField label={ar ? 'حدد الشهر' : 'Select Month'}>
                  <select className="form-select" value={newForm.month} onChange={e => setNewForm({...newForm, month: parseInt(e.target.value, 10)})}>
                    <option value={0}>{ar ? '— اختر الشهر —' : '— Select Month —'}</option>
                    {months.map((m, i) => <option key={i + 1} value={i + 1}>{m}</option>)}
                  </select>
                </FormField>
                <FormField label={ar ? 'حدد النصف' : 'Select Half'}>
                  <select className="form-select" value={newForm.periodType} onChange={e => setNewForm({...newForm, periodType: parseInt(e.target.value, 10)})}>
                    {periodTypes.map(pt => (
                      <option key={pt.id} value={pt.id}>{pt.name}</option>
                    ))}
                  </select>
                </FormField>
              </div>

              <div className="form-section-header"><Droplets size={14}/> {ar ? 'مخزون الوقود الفعلي (ميداني)' : 'Physical Fuel Stock'}</div>
              {!newForm.station ? (
                <div style={{ padding: 20, textAlign: 'center', color: 'var(--text-faint)', fontSize: 13, background: 'var(--surface-2)', borderRadius: 8 }}>{ar ? 'يرجى اختيار الموقع أولاً لإظهار الخزانات' : 'Select location first to show tanks'}</div>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                  {newForm.fuelReadings.length === 0 ? (
                    <div style={{ padding: 14, textAlign: 'center', color: 'var(--text-faint)', fontSize: 12, background: 'var(--surface-2)', borderRadius: 8 }}>
                      {ar ? 'لا توجد خزانات وقود مرتبطة بهذا الموقع' : 'No fuel tanks linked to this location'}
                    </div>
                  ) : newForm.fuelReadings.map((r, i) => (
                    <div key={i} className="form-group" style={{ margin: 0 }}>
                      <label className="form-label" style={{ fontSize: 11, marginBottom: 4 }}>{r.tankName}</label>
                      <Input type="number" placeholder={ar ? 'الكمية الفعلية باللتر...' : 'Actual Liters...'} 
                        value={r.actualQty} onChange={e => updateActualInForm('fuel', i, e.target.value)} />
                    </div>
                  ))}
                </div>
              )}

              <div className="form-section-header"><TrendingUp size={14}/> {ar ? 'مخزون الزيت الفعلي (ميداني)' : 'Physical Oil Stock'}</div>
              {!newForm.station ? (
                <div style={{ padding: 20, textAlign: 'center', color: 'var(--text-faint)', fontSize: 13, background: 'var(--surface-2)', borderRadius: 8 }}>
                  {ar ? 'يرجى اختيار الموقع أولاً لإظهار الزيوت' : 'Select location first to show oils'}
                </div>
              ) : (
                <div className="form-2col" style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                  {newForm.oilReadings.length === 0 ? (
                    <div style={{ gridColumn: '1 / -1', padding: 14, textAlign: 'center', color: 'var(--text-faint)', fontSize: 12, background: 'var(--surface-2)', borderRadius: 8 }}>
                      {ar ? 'لا توجد زيوت مرتبطة بهذا الموقع' : 'No oils linked to this location'}
                    </div>
                  ) : newForm.oilReadings.map((r, i) => (
                    <div key={i} className="form-group" style={{ margin: 0 }}>
                      <label className="form-label" style={{ fontSize: 11, marginBottom: 4 }}>{r.oilName}</label>
                      <Input type="number" placeholder="L" 
                        value={r.actualQty} onChange={e => updateActualInForm('oil', i, e.target.value)} />
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* ── COL 2: Energy & Details ── */}
            <div style={{ background: 'hsla(217,91%,60%,0.02)', padding: '20px', borderRadius: 16, border: '1px solid var(--border)' }}>
              <div className="form-section-header"><FileText size={14}/> {ar ? 'ملاحظات' : 'Notes'}</div>
              <FormField label={ar ? 'ملاحظات (اختياري)' : 'Notes (optional)'}>
                <textarea
                  className="form-control"
                  rows={8}
                  placeholder={ar ? 'ملاحظات الجرد الميداني...' : 'Field inventory notes...'}
                  value={newForm.notes || ''}
                  onChange={e => setNewForm({ ...newForm, notes: e.target.value })}
                  style={{ minHeight: 180, resize: 'vertical', lineHeight: 1.7 }}
                />
              </FormField>
              <div style={{ marginTop: 16, padding: 14, background: 'var(--primary-subtle)', borderRadius: 10, border: '1px solid var(--primary)' }}>
                <p style={{ margin: 0, fontSize: 12, color: 'var(--text-muted)', lineHeight: 1.7 }}>
                  {ar
                    ? '✓ يُحتسب تلقائياً من الإدخال اليومي المعتمد: الطاقة المنتجة، ساعات التشغيل، الوقود المستهلك، الوقود الوارد، الوقود المنقول، الفاقد.'
                    : '✓ Auto-calculated from approved daily readings: energy produced, operating hours, fuel consumed, fuel received, transfers, losses.'}
                </p>
              </div>
            </div>
          </div>
        </Modal>
      )}

      {/* ── REPORT VIEW (Based on provided images) ── */}
      <div className="section-card periodic-print-card" style={{ padding: 0, overflow: 'hidden' }}>

        {/* PRINT HEADER */}
        <div className="print-wrapper">
        <div className="print-only print-header">
          <div className="print-logo" style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-start', gap: '15px' }}>
            {branding.logoUrl && <img src={branding.logoUrl} alt={branding.name} onError={(e) => e.target.style.display='none'} style={{ width: 80, height: 80, objectFit: 'contain' }} />}
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: '16px', fontWeight: '900', color: '#000', lineHeight: 1.2, whiteSpace: 'nowrap' }}>Fuel & Energy Tracking</div>
              <div style={{ fontSize: '12px', color: '#555', fontWeight: '600', textAlign: 'center' }}>نظام تتبع الوقود والطاقة</div>
            </div>
          </div>
          <div className="print-title">
            <div style={{ fontSize: '16px', fontWeight: 'bold', color: '#000', marginBottom: '8px' }}>
              {ar ? 'تقرير الجرد الدوري' : 'Periodic Inventory Report'}
            </div>
            <div style={{ fontSize: '13px', fontWeight: 'bold', color: '#000' }}>
              {ar ? `تاريخ التقرير: ${new Date().toLocaleDateString('ar-EG')}` : `Report Date: ${new Date().toLocaleDateString()}`}
            </div>
          </div>
          <div style={{ textAlign: 'start' }}></div>
        </div>

        <div style={{ overflowX: 'auto' }}>
          <style>{`.data-table th, .data-table td { text-align: center !important; }`}</style>
          <table className="data-table" style={{ width: '100%', minWidth: '760px', margin: 0, borderBottom: '2px solid var(--border)' }}>
            <thead style={{ background: 'rgba(99,179,237,0.1)' }}>
              <tr>
                <th>{ar ? 'اسم الموقع' : 'Location'}</th>
                <th>{ar ? 'مخزون الوقود الفعلي' : 'Actual Fuel Stock'}</th>
                <th>{ar ? 'مخزون الزيت الفعلي' : 'Actual Oil Stock'}</th>
                <th>{ar ? 'الفترة' : 'Period'}</th>
                <th>{ar ? 'الشهر' : 'Month'}</th>
                <th>{ar ? 'تاريخ الجرد' : 'Inv. Date'}</th>
                <th>{ar ? 'الحالة' : 'Status'}</th>
                <th className="no-print">{ar ? 'الإجراءات' : 'Actions'}</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan="8" style={{ textAlign: 'center', padding: '40px' }}><div className="ft-spinner" style={{ margin: 'auto' }}></div></td></tr>
              ) : reportInventories.length === 0 ? (
                <tr><td colSpan="8" style={{ textAlign: 'center', padding: '40px' }}>{ar ? 'لا يوجد بيانات' : 'No data'}</td></tr>
              ) : (
                reportInventories.map((inv, idx) => {
                  const fuelItems = getFuelInventoryItems(inv);
                  const fuelStr = `${formatLiters(getSavedFuelActualQty(inv))} ${ar ? 'لتر' : 'L'}`;
                  const oilItems = getOilInventoryItems(inv);
                  return (
                    <tr key={`main-${inv.id || inv._id}`} className={inv.status === 'rejected' ? 'print-excluded' : ''} style={{ background: idx % 2 === 0 ? 'rgba(255,100,50,0.1)' : 'rgba(50,200,100,0.1)', cursor: 'pointer' }} onClick={() => setSelectedInventory(inv)}>
                      <td style={{ fontWeight: 'bold' }}>{getLocationLabel(inv)}</td>
                      <td>
                        {fuelItems && fuelItems.length > 0 ? (
                          <div className="oil-inventory-list">
                            {fuelItems.map(item => (
                              <div key={item.key}>
                                <span>{item.name}{item.fuelType ? ` (${item.fuelType})` : ''}</span>
                                <span> : {formatLiters(item.actualQty)} {ar ? 'لتر' : 'L'}</span>
                              </div>
                            ))}
                          </div>
                        ) : fuelStr}
                      </td>
                      <td>
                        {oilItems.length === 0 ? '—' : (
                          <div className="oil-inventory-list">
                            {oilItems.map(item => (
                              <div key={item.key}>
                                <span>{item.name}</span>
                                <span> : {formatLiters(item.actualQty)} {ar ? 'لتر' : 'L'}</span>
                              </div>
                            ))}
                          </div>
                        )}
                      </td>
                      <td>{inv.periodType}</td>
                      <td>{months[(inv.month || 1) - 1] || inv.month}</td>
                      <td style={{ direction: 'ltr' }}>{formatDate(inv.inventoryDate)}</td>
                      <td>
                        {inv.status === 'approved' && <span className="badge badge-success">{ar ? 'معتمد' : 'Approved'}</span>}
                        {inv.status === 'rejected' && <span className="badge badge-danger">{ar ? 'مرفوض' : 'Rejected'}</span>}
                        {inv.status === 'submitted' && <span className="badge badge-warning">{ar ? 'قيد المراجعة' : 'Pending'}</span>}
                        {(inv.status === 'draft' || !inv.status) && <span className="badge badge-muted">{ar ? 'مسودة' : 'Draft'}</span>}
                      </td>
                      <td className="no-print" style={{ display: 'flex', gap: 6 }}>
                        {canReviewInventory(currentUser, inv) && (
                          <>
                            <button className="btn btn-sm btn-primary" onClick={() => updateStatus(inv, 'approved')}>{ar ? 'اعتماد' : 'Approve'}</button>
                            <button className="btn btn-sm btn-danger" onClick={() => updateStatus(inv, 'rejected')}>{ar ? 'رفض' : 'Reject'}</button>
                          </>
                        )}
                        {canSubmitInventory(currentUser, inv) && (
                          <button className="btn btn-sm btn-primary" onClick={() => updateStatus(inv, 'submitted')}>{ar ? 'تقديم للاعتماد' : 'Submit'}</button>
                        )}
                      </td>
                    </tr>
                  )
                })
              )}
            </tbody>
          </table>
        </div>

        {/* INVENTORY NOTES */}
        <div style={{ background: 'var(--surface-2)', padding: '8px 16px', borderBottom: '1px solid var(--border)', textAlign: 'center', fontSize: '0.85rem', fontWeight: 'bold', color: 'var(--text-faint)' }}>
          — {ar ? 'ملاحظات الجرد' : 'Inventory Notes'} —
        </div>

        <div style={{ overflowX: 'auto' }}>
          <table className="data-table inventory-notes-table" style={{ width: '100%', minWidth: '760px', margin: 0 }}>
            <thead style={{ background: 'rgba(99,179,237,0.05)' }}>
              <tr>
                <th>{ar ? 'اسم الموقع' : 'Location'}</th>
                <th>{ar ? 'ملاحظات الجرد' : 'Inventory Notes'}</th>
                <th>{ar ? 'التاريخ' : 'Date'}</th>
              </tr>
            </thead>
            <tbody>
              {loading ? null : reportInventories.length === 0 ? null : (
                reportInventories.map((inv) => (
                  <tr key={`notes-${inv.id || inv._id}`} className={inv.status === 'rejected' ? 'print-excluded' : ''}>
                    <td style={{ fontWeight: 'bold' }}>{getLocationLabel(inv)}</td>
                    <td className="inventory-notes-cell">{getInventoryNotes(inv)}</td>
                    <td style={{ direction: 'ltr' }}>{formatDate(inv.inventoryDate)}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* PRINT FOOTER (SIGNATURES) */}
        <div className="print-only print-footer">
          <div className="print-signatures">
            <div className="signature-box">
              <p>{ar ? 'توقيع أعضاء لجنة الجرد' : 'Committee Members Signature'}</p>
              <div className="line"></div>
            </div>
            <div className="signature-box">
              <p>{ar ? 'توقيع رئيس لجنة الجرد' : 'Inventory Committee Head Signature'}</p>
              <div className="line"></div>
            </div>
          </div>
          <div className="print-footer-info">
            <span>{ar ? 'تاريخ ووقت الطباعة:' : 'Print Date:'} {new Date().toLocaleString(ar ? 'ar-EG' : 'en-US')}</span>
            <span>{ar ? 'بواسطة المستخدم:' : 'Printed by:'} {currentUser?.name || 'Unknown'}</span>
          </div>
        </div>
        </div>{/* end print-wrapper */}

      </div>

      {/* ── INVENTORY DETAIL MODAL ── */}
      {selectedInventory && (() => {
        const inv = selectedInventory;
        const fuelItems = getFuelInventoryItems(inv);
        const oilItems = getOilInventoryItems(inv);
        const statusColors = { approved: 'var(--success)', rejected: 'var(--danger)', submitted: 'var(--warning)', draft: 'var(--text-muted)' };
        const statusLabels = { approved: ar ? 'معتمد' : 'Approved', rejected: ar ? 'مرفوض' : 'Rejected', submitted: ar ? 'قيد المراجعة' : 'Pending Review', draft: ar ? 'مسودة' : 'Draft' };
        return (
          <Modal
            open={!!selectedInventory}
            onClose={() => setSelectedInventory(null)}
            title={
              <span style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <PackageSearch size={20} style={{ color: 'var(--primary)' }} />
                {ar ? 'تفاصيل الجرد الدوري' : 'Inventory Details'}
              </span>
            }
            maxWidth={800}
          >
            {/* Header Info */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 14, marginBottom: 22 }}>
              <div style={{ background: 'var(--surface-2)', borderRadius: 10, padding: 14, textAlign: 'center', border: '1px solid var(--border)' }}>
                <div style={{ fontSize: 11, color: 'var(--text-muted)', marginBottom: 4 }}>{ar ? 'الموقع' : 'Location'}</div>
                <div style={{ fontSize: 15, fontWeight: 700, color: 'var(--text-main)' }}>{getLocationLabel(inv)}</div>
              </div>
              <div style={{ background: 'var(--surface-2)', borderRadius: 10, padding: 14, textAlign: 'center', border: '1px solid var(--border)' }}>
                <div style={{ fontSize: 11, color: 'var(--text-muted)', marginBottom: 4 }}>{ar ? 'الفترة' : 'Period'}</div>
                <div style={{ fontSize: 15, fontWeight: 700, color: 'var(--text-main)' }}>{inv.periodType || '—'}</div>
                <div style={{ fontSize: 13, color: 'var(--text-muted)' }}>{months[(inv.month || 1) - 1]} {inv.year || ''}</div>
              </div>
              <div style={{ background: 'var(--surface-2)', borderRadius: 10, padding: 14, textAlign: 'center', border: '1px solid var(--border)' }}>
                <div style={{ fontSize: 11, color: 'var(--text-muted)', marginBottom: 4 }}>{ar ? 'الحالة' : 'Status'}</div>
                <span className="badge" style={{ background: statusColors[inv.status] || 'var(--text-muted)', color: 'var(--on-accent)', fontSize: 13, fontWeight: 600, padding: '5px 14px' }}>
                  {statusLabels[inv.status] || inv.status || '—'}
                </span>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, marginBottom: 22 }}>
              <div style={{ fontSize: 13, color: 'var(--text-muted)' }}>
                <span style={{ fontWeight: 600 }}>{ar ? 'تاريخ الجرد: ' : 'Inventory Date: '}</span>
                <span style={{ direction: 'ltr', display: 'inline-block' }}>{formatDate(inv.inventoryDate)}</span>
              </div>
              <div style={{ fontSize: 13, color: 'var(--text-muted)' }}>
                <span style={{ fontWeight: 600 }}>{ar ? 'الفريق: ' : 'Team: '}</span>
                <span>{inv.team || '—'}</span>
              </div>
            </div>

            {/* Fuel Readings */}
            <div style={{ marginBottom: 22 }}>
              <div style={{ fontSize: 14, fontWeight: 600, color: 'var(--text-main)', marginBottom: 10, display: 'flex', alignItems: 'center', gap: 8 }}>
                <Droplets size={15} style={{ color: 'var(--primary)' }} />
                {ar ? 'مخزون الوقود' : 'Fuel Stock'}
              </div>
              {fuelItems && fuelItems.length > 0 ? (
                <div style={{ overflowX: 'auto' }}>
                  <table className="data-table" style={{ width: '100%', fontSize: 13 }}>
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>{ar ? 'الخزان' : 'Tank'}</th>
                        <th>{ar ? 'النوع' : 'Type'}</th>
                        <th>{ar ? 'المخزون الفعلي' : 'Actual Stock'}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {fuelItems.map((item, i) => (
                        <tr key={item.key || i}>
                          <td style={{ color: 'var(--text-faint)' }}>{i + 1}</td>
                          <td style={{ fontWeight: 600 }}>{item.name || '—'}</td>
                          <td>{item.fuelType || '—'}</td>
                          <td style={{ fontWeight: 700 }}>{formatLiters(item.actualQty)} L</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div style={{ padding: 14, textAlign: 'center', color: 'var(--text-faint)', fontSize: 13, background: 'var(--surface-2)', borderRadius: 10 }}>
                  {ar ? 'لا توجد بيانات وقود' : 'No fuel data'}
                </div>
              )}
            </div>

            {/* Oil Readings */}
            <div style={{ marginBottom: 22 }}>
              <div style={{ fontSize: 14, fontWeight: 600, color: 'var(--text-main)', marginBottom: 10, display: 'flex', alignItems: 'center', gap: 8 }}>
                <TrendingUp size={15} style={{ color: 'var(--warning)' }} />
                {ar ? 'مخزون الزيوت' : 'Oil Stock'}
              </div>
              {oilItems.length > 0 ? (
                <div style={{ overflowX: 'auto' }}>
                  <table className="data-table" style={{ width: '100%', fontSize: 13 }}>
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>{ar ? 'نوع الزيت' : 'Oil Type'}</th>
                        <th>{ar ? 'المخزون الفعلي' : 'Actual Stock'}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {oilItems.map((item, i) => (
                        <tr key={item.key || i}>
                          <td style={{ color: 'var(--text-faint)' }}>{i + 1}</td>
                          <td style={{ fontWeight: 600 }}>{item.name || '—'}</td>
                          <td style={{ fontWeight: 700 }}>{formatLiters(item.actualQty)} L</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div style={{ padding: 14, textAlign: 'center', color: 'var(--text-faint)', fontSize: 13, background: 'var(--surface-2)', borderRadius: 10 }}>
                  {ar ? 'لا توجد بيانات زيوت' : 'No oil data'}
                </div>
              )}
            </div>

            {/* Notes */}
            {inv.notes && (
              <div>
                <div style={{ fontSize: 14, fontWeight: 600, color: 'var(--text-main)', marginBottom: 8, display: 'flex', alignItems: 'center', gap: 8 }}>
                  <FileText size={15} style={{ color: 'var(--primary)' }} />
                  {ar ? 'ملاحظات' : 'Notes'}
                </div>
                <div style={{ padding: 14, background: 'var(--surface-2)', borderRadius: 10, fontSize: 14, lineHeight: 1.7, whiteSpace: 'pre-wrap', color: 'var(--text-muted)' }}>
                  {inv.notes}
                </div>
              </div>
            )}
          </Modal>
        );
      })()}

      <style>{`
        .print-only { display: none; }
        .oil-inventory-list {
          display: flex;
          flex-direction: column;
          gap: 4px;
          align-items: stretch;
          white-space: normal;
          line-height: 1.6;
        }
        .oil-inventory-list > div {
          display: flex;
          justify-content: center;
          gap: 3px;
          flex-wrap: wrap;
        }
        .inventory-notes-cell {
          white-space: pre-wrap;
          word-break: break-word;
          line-height: 1.7;
          text-align: start !important;
          min-width: 260px;
        }
        @media print {
          /* إخفاء headers/footers المتصفح */
          @page { size: landscape; margin: 0; }

          html, body, #root, .app-root {
            height: auto !important;
            min-height: 0 !important;
            margin: 0 !important;
            padding: 0 !important;
            overflow: visible !important;
            box-sizing: border-box !important;
          }
          body { background: #fff !important; color: #000 !important; font-family: 'Arial', sans-serif !important; margin: 0 !important; padding: 0 !important; }

          .sidebar, .page-header, .alert, .modal-overlay { display: none !important; }
          .print-excluded { display: none !important; }
          .main-content {
            margin: 0 !important;
            padding: 0 !important;
            width: 100% !important;
            max-width: 100% !important;
            height: auto !important;
            min-height: 0 !important;
            overflow: visible !important;
          }
          .page-wrapper {
            padding: 0 !important;
            margin: 0 !important;
            max-width: none !important;
            width: 100% !important;
          }
          .periodic-inventory-page {
            animation: none !important;
            margin: 0 !important;
            padding: 0 !important;
            height: auto !important;
            min-height: 0 !important;
            overflow: visible !important;
          }
          .periodic-inventory-page > :not(.periodic-print-card) { display: none !important; }
          .periodic-print-card { display: block !important; height: auto !important; min-height: 0 !important; overflow: visible !important; }
          .section-card { border: none !important; box-shadow: none !important; background: transparent !important; margin: 0 !important; padding: 0 !important; }

          .print-only { display: block !important; }
          .no-print { display: none !important; }

          /* wrapper يحل محل padding على body */
          .print-wrapper {
            display: block !important;
            padding: 8mm 10mm !important;
            margin: 0 !important;
            width: 100% !important;
            height: auto !important;
            min-height: 0 !important;
            break-before: auto !important;
            page-break-before: auto !important;
            break-after: auto !important;
            page-break-after: auto !important;
          }

          /* Print Header */
          .print-header { display: grid !important; grid-template-columns: 1fr 2fr 1fr !important; align-items: center !important; border-bottom: 2px solid #000; padding-bottom: 8px; margin-bottom: 12px; }
          .print-title { text-align: center !important; }
          .print-title h1 { font-size: 20px !important; margin: 0 0 6px 0 !important; color: #000 !important; }
          .print-title p { margin: 0 !important; font-size: 13px !important; color: #000 !important; }

          /* Enhanced Table Borders */
          .data-table { border: 2px solid #000 !important; border-collapse: collapse !important; width: 100% !important; min-width: 100% !important; margin: 0 !important; page-break-after: avoid !important; }
          .data-table th, .data-table td { border: 1px solid #000 !important; padding: 5px 4px !important; color: #000 !important; font-size: 12px !important; background: transparent !important; text-align: center !important; }
          .data-table th { background-color: #f1f5f9 !important; -webkit-print-color-adjust: exact; print-color-adjust: exact; font-weight: bold !important; color: #000 !important; }
          .oil-inventory-list { gap: 2px !important; line-height: 1.45 !important; }
          .inventory-notes-cell { white-space: pre-wrap !important; word-break: break-word !important; text-align: start !important; }

          /* Print Footer — يمنع الانزلاق لصفحة جديدة */
          .print-footer { margin-top: 14px !important; page-break-inside: avoid !important; page-break-before: avoid !important; }
          .print-signatures { display: flex !important; justify-content: space-between !important; margin-top: 14px !important; padding: 0 40px !important; }
          .signature-box { text-align: center !important; width: 220px !important; }
          .signature-box p { font-size: 15px !important; font-weight: bold !important; margin-bottom: 28px !important; }
          .signature-box .line { border-top: 1px solid #000 !important; width: 100% !important; }

          .print-footer-info { display: flex !important; justify-content: space-between !important; font-size: 11px !important; color: #000 !important; border-top: 1px solid #000 !important; padding-top: 8px !important; margin-top: 18px !important; }
        }
      `}</style>

      <ConfirmModal
        open={confirmModal.open}
        title={ar ? 'تأكيد تحديث الحالة' : 'Confirm Status Update'}
        message={ar ? 'تأكيد العملية؟' : 'Confirm action?'}
        confirmText={ar ? 'تأكيد' : 'Confirm'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        onConfirm={confirmUpdateStatus}
        onCancel={() => setConfirmModal({ open: false, inventory: null, newStatus: null })}
      />
    </div>
  );
}

export default PeriodicInventoryPage;
