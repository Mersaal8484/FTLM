import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Settings, Plus, Edit2, Trash2, ArrowLeft, ArrowRight, Check } from 'lucide-react';
import client from '../api/client';
import { Button, Modal, FormField, Input } from '../components/ui';
import useCommercialMode from '../hooks/useCommercialMode';

export default function ServiceDefinitionsAdmin({ lang }) {
  const ar = lang === 'ar';
  const navigate = useNavigate();
  const { isCommercialMode, modeResolved } = useCommercialMode();

  const [definitions, setDefinitions] = useState([]);
  const [serviceItems, setServiceItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedModel, setSelectedModel] = useState('');

  // Definition modal state
  const [defModalOpen, setDefModalOpen] = useState(false);
  const [defModelKey, setDefModelKey] = useState('');
  const [defItemId, setDefItemId] = useState('');
  const [defInterval, setDefInterval] = useState('');
  const [defImportance, setDefImportance] = useState('1');
  const [defWarningLead, setDefWarningLead] = useState('');
  const [defNotes, setDefNotes] = useState('');
  const [submittingDef, setSubmittingDef] = useState(false);
  const [defError, setDefError] = useState('');

  // Item modal state
  const [itemModalOpen, setItemModalOpen] = useState(false);
  const [itemCode, setItemCode] = useState('');
  const [itemNameAr, setItemNameAr] = useState('');
  const [itemNameEn, setItemNameEn] = useState('');
  const [itemSortOrder, setItemSortOrder] = useState('0');
  const [itemSuggestedHours, setItemSuggestedHours] = useState('');
  const [submittingItem, setSubmittingItem] = useState(false);
  const [itemError, setItemError] = useState('');

  const [maintenanceOilConfigs, setMaintenanceOilConfigs] = useState([]);
  const [generators, setGenerators] = useState([]);
  const [oils, setOils] = useState([]);
  const [oilConfigModalOpen, setOilConfigModalOpen] = useState(false);
  const [oilConfigGeneratorId, setOilConfigGeneratorId] = useState('');
  const [oilConfigServiceItemId, setOilConfigServiceItemId] = useState('');
  const [oilConfigOilId, setOilConfigOilId] = useState('');
  const [oilConfigQuantity, setOilConfigQuantity] = useState('');
  const [oilConfigError, setOilConfigError] = useState('');
  const [submittingOilConfig, setSubmittingOilConfig] = useState(false);

  const loadData = async () => {
    setLoading(true);
    try {
      const [defRes, itemsRes, oilConfigRes, generatorRes, oilsRes] = await Promise.all([
        client.get('/api/service-interval-definitions'),
        client.get('/api/service-items?includeInactive=true'),
        isCommercialMode ? client.get('/api/generator-maintenance-oil-configurations') : Promise.resolve({ data: [] }),
        isCommercialMode ? client.get('/api/generators') : Promise.resolve({ data: [] }),
        isCommercialMode ? client.get('/api/oils') : Promise.resolve({ data: [] }),
      ]);
      setDefinitions(defRes.data || []);
      setServiceItems(itemsRes.data || []);
      setMaintenanceOilConfigs(oilConfigRes.data || []);
      setGenerators(generatorRes.data?.items || generatorRes.data || []);
      setOils(oilsRes.data?.items || oilsRes.data || []);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (modeResolved) loadData();
  }, [isCommercialMode, modeResolved]);

  const openAddDefModal = () => {
    setDefModelKey(selectedModel || '');
    setDefItemId(serviceItems.length > 0 ? String(serviceItems[0].id) : '');
    setDefInterval('250');
    setDefImportance('1');
    setDefWarningLead('');
    setDefNotes('');
    setDefError('');
    setDefModalOpen(true);
  };

  const handleSaveDefinition = async (e) => {
    e.preventDefault();
    if (!defModelKey.trim() || !defItemId || !defInterval || parseFloat(defInterval) <= 0) {
      setDefError(ar ? 'يرجى ملء جميع الحقول الإلزامية بفترات صحيحة' : 'Please fill all required fields correctly');
      return;
    }

    setSubmittingDef(true);
    setDefError('');
    try {
      await client.post('/api/service-interval-definitions', {
        generatorModelKey: defModelKey.trim(),
        serviceItemId: parseInt(defItemId),
        intervalHours: parseFloat(defInterval),
        importance: parseInt(defImportance),
        warningLeadHours: defWarningLead ? parseFloat(defWarningLead) : null,
        notes: defNotes.trim() || null,
      });
      setDefModalOpen(false);
      await loadData();
    } catch (err) {
      setDefError(err.response?.data?.error || (ar ? 'فشل حفظ التعريف' : 'Failed to save definition'));
    } finally {
      setSubmittingDef(false);
    }
  };

  const handleDeleteDefinition = async (id) => {
    if (!window.confirm(ar ? 'هل أنت متأكد من حذف هذا التعريف؟' : 'Are you sure you want to delete this definition?')) return;
    try {
      await client.delete(`/api/service-interval-definitions/${id}`);
      await loadData();
    } catch (err) {
      alert(err.response?.data?.error || (ar ? 'فشل الحذف' : 'Failed to delete'));
    }
  };

  const handleSaveItem = async (e) => {
    e.preventDefault();
    if (!itemCode.trim() || !itemNameAr.trim() || !itemNameEn.trim()) {
      setItemError(ar ? 'يرجى ملء الكود والاسم باللغتين' : 'Please fill code and names in both languages');
      return;
    }

    setSubmittingItem(true);
    setItemError('');
    try {
      await client.post('/api/service-items', {
        code: itemCode.trim(),
        nameAr: itemNameAr.trim(),
        nameEn: itemNameEn.trim(),
        sortOrder: parseInt(itemSortOrder) || 0,
        suggestedIntervalHours: itemSuggestedHours ? parseFloat(itemSuggestedHours) : null,
        isActive: true,
      });
      setItemModalOpen(false);
      await loadData();
    } catch (err) {
      setItemError(err.response?.data?.error || (ar ? 'فشل إضافة بند الخدمة' : 'Failed to add service item'));
    } finally {
      setSubmittingItem(false);
    }
  };

  const openOilConfigModal = () => {
    setOilConfigGeneratorId(generators.length ? String(generators[0].id) : '');
    setOilConfigServiceItemId(serviceItems.length ? String(serviceItems[0].id) : '');
    setOilConfigOilId(oils.length ? String(oils[0].id) : '');
    setOilConfigQuantity('');
    setOilConfigError('');
    setOilConfigModalOpen(true);
  };

  const handleSaveOilConfig = async (event) => {
    event.preventDefault();
    if (!oilConfigGeneratorId || !oilConfigServiceItemId || !oilConfigOilId || Number(oilConfigQuantity) <= 0) {
      setOilConfigError(ar ? 'اختر المولد وبند الصيانة ونوع الزيت وأدخل كمية موجبة' : 'Select generator, service item and oil, then enter a positive quantity');
      return;
    }
    setSubmittingOilConfig(true);
    setOilConfigError('');
    try {
      await client.post('/api/generator-maintenance-oil-configurations', {
        generatorId: Number(oilConfigGeneratorId),
        serviceItemId: Number(oilConfigServiceItemId),
        oilId: Number(oilConfigOilId),
        quantityLiters: Number(oilConfigQuantity),
      });
      setOilConfigModalOpen(false);
      await loadData();
    } catch (error) {
      setOilConfigError(error.response?.data?.error || (ar ? 'فشل حفظ ربط زيت الصيانة' : 'Failed to save maintenance oil configuration'));
    } finally {
      setSubmittingOilConfig(false);
    }
  };

  const handleDeleteOilConfig = async (id) => {
    if (!window.confirm(ar ? 'هل تريد إزالة ربط زيت الصيانة؟' : 'Remove this maintenance-oil configuration?')) return;
    await client.delete(`/api/generator-maintenance-oil-configurations/${id}`);
    await loadData();
  };

  const models = Array.from(new Set(definitions.map(d => d.generatorModelKey))).sort();
  const filteredDefs = selectedModel
    ? definitions.filter(d => d.generatorModelKey === selectedModel)
    : definitions;

  return (
    <div style={{ animation: 'fadeIn 0.3s ease', maxWidth: 1000, margin: '0 auto', paddingBottom: 40 }}>
      <div style={{ marginBottom: 20 }}>
        <button
          className="btn btn-secondary btn-sm"
          onClick={() => navigate('/maintenance/service')}
          style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 12 }}
        >
          {ar ? <ArrowRight size={16} /> : <ArrowLeft size={16} />}
          {ar ? 'العودة لجداول الصيانة' : 'Back to Service Intervals'}
        </button>

        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 16 }}>
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10, margin: 0, fontSize: '1.4rem' }}>
              <Settings size={22} style={{ color: 'var(--primary)' }} />
              {ar ? 'إدارة فترات الصيانة وبنود الخدمة' : 'Service Interval Definitions & Items'}
            </h1>
            <p style={{ opacity: 0.8, fontSize: '0.875rem', marginTop: 4 }}>
              {ar ? 'تحديد فترات الصيانة بالساعات لكل طراز مولد، وإدارة بنود الصيانة المتاحة' : 'Configure service interval hours per generator model key and manage service items'}
            </p>
          </div>
          <div style={{ display: 'flex', gap: 10 }}>
            <Button variant="secondary" onClick={() => setItemModalOpen(true)} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <Plus size={16} />
              {ar ? 'إضافة بند صيانة' : 'New Service Item'}
            </Button>
            <Button variant="primary" onClick={openAddDefModal} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <Plus size={16} />
              {ar ? 'تعريف فترة لطراز' : 'New Model Definition'}
            </Button>
          </div>
        </div>
      </div>

      {/* Filter by Model */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 16, alignItems: 'center' }}>
        <label style={{ fontSize: '0.9rem', fontWeight: 500 }}>{ar ? 'تصفية حسب الطراز:' : 'Filter by model:'}</label>
        <select
          className="ft-input"
          value={selectedModel}
          onChange={(e) => setSelectedModel(e.target.value)}
          style={{ minWidth: 200, height: 36 }}
        >
          <option value="">{ar ? 'جميع الطرازات' : 'All Models'}</option>
          {models.map(m => (
            <option key={m} value={m}>{m}</option>
          ))}
        </select>
      </div>

      {/* Definitions Table */}
      <div className="panel" style={{ padding: 20, borderRadius: 'var(--radius-xs)', border: '1px solid var(--border)', marginBottom: 30 }}>
        <h3 style={{ margin: '0 0 16px 0', fontSize: '1.1rem' }}>
          {ar ? 'فترات الصيانة المعرفة حسب الطراز' : 'Model Service Interval Definitions'}
        </h3>

        {loading ? (
          <div className="ft-spinner" style={{ width: 30, height: 30, margin: '40px auto' }} />
        ) : filteredDefs.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 24, color: 'var(--text-muted)' }}>
            {ar ? 'لا توجد تعريفات مسجلة' : 'No service interval definitions registered'}
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table className="table" style={{ width: '100%', fontSize: '0.9rem', textAlign: ar ? 'right' : 'left' }}>
              <thead>
                <tr style={{ background: 'var(--surface-2)', borderBottom: '2px solid var(--border)' }}>
                  <th style={{ padding: '10px 12px' }}>{ar ? 'طراز المولد (Model Key)' : 'Model Key'}</th>
                  <th style={{ padding: '10px 12px' }}>{ar ? 'بند الصيانة' : 'Service Item'}</th>
                  <th style={{ padding: '10px 12px' }}>{ar ? 'فترة الصيانة' : 'Interval'}</th>
                  <th style={{ padding: '10px 12px' }}>{ar ? 'التحذير المسبق' : 'Lead Hours'}</th>
                  <th style={{ padding: '10px 12px' }}>{ar ? 'الأهمية' : 'Importance'}</th>
                  <th style={{ padding: '10px 12px', textAlign: 'center' }}>{ar ? 'إجراءات' : 'Actions'}</th>
                </tr>
              </thead>
              <tbody>
                {filteredDefs.map(d => (
                  <tr key={d.id} style={{ borderBottom: '1px solid var(--border)' }}>
                    <td style={{ padding: '10px 12px', fontWeight: 600 }}>{d.generatorModelKey}</td>
                    <td style={{ padding: '10px 12px' }}>{ar ? d.serviceItemNameAr : d.serviceItemNameEn}</td>
                    <td style={{ padding: '10px 12px' }}>{Number(d.intervalHours).toLocaleString('en-US')} h</td>
                    <td style={{ padding: '10px 12px' }}>{Number(d.effectiveLeadHours).toLocaleString('en-US')} h</td>
                    <td style={{ padding: '10px 12px' }}>
                      <span style={{ fontSize: '0.8rem', padding: '2px 8px', borderRadius: 'var(--radius-xs)', background: d.importance === 3 ? 'var(--danger-subtle)' : 'var(--surface-2)', color: d.importance === 3 ? 'var(--danger)' : 'var(--text)' }}>
                        {d.importance === 3 ? (ar ? 'حرجة' : 'Critical') : d.importance === 2 ? (ar ? 'عالية' : 'High') : (ar ? 'عادية' : 'Normal')}
                      </span>
                    </td>
                    <td style={{ padding: '10px 12px', textAlign: 'center' }}>
                      <button
                        className="btn btn-sm btn-danger"
                        onClick={() => handleDeleteDefinition(d.id)}
                        style={{ padding: '4px 8px', fontSize: '0.8rem' }}
                      >
                        <Trash2 size={14} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Available Service Items Section */}
      <div className="panel" style={{ padding: 20, borderRadius: 'var(--radius-xs)', border: '1px solid var(--border)' }}>
        <h3 style={{ margin: '0 0 16px 0', fontSize: '1.1rem' }}>
          {ar ? 'بنود الصيانة المعرفة في النظام' : 'Registered Service Items'}
        </h3>
        <div style={{ overflowX: 'auto' }}>
          <table className="table" style={{ width: '100%', fontSize: '0.85rem', textAlign: ar ? 'right' : 'left' }}>
            <thead>
              <tr style={{ background: 'var(--surface-2)' }}>
                <th style={{ padding: '8px 10px' }}>{ar ? 'الرمز' : 'Code'}</th>
                <th style={{ padding: '8px 10px' }}>{ar ? 'الاسم بالعربية' : 'Arabic Name'}</th>
                <th style={{ padding: '8px 10px' }}>{ar ? 'الاسم بالإنجليزية' : 'English Name'}</th>
                <th style={{ padding: '8px 10px' }}>{ar ? 'الفترة الافتراضية المقترحة' : 'Suggested Interval'}</th>
                <th style={{ padding: '8px 10px' }}>{ar ? 'الترتيب' : 'Sort Order'}</th>
              </tr>
            </thead>
            <tbody>
              {serviceItems.map(i => (
                <tr key={i.id} style={{ borderBottom: '1px solid var(--border)' }}>
                  <td style={{ padding: '8px 10px', fontWeight: 600 }}>{i.code}</td>
                  <td style={{ padding: '8px 10px' }}>{i.nameAr}</td>
                  <td style={{ padding: '8px 10px' }}>{i.nameEn}</td>
                  <td style={{ padding: '8px 10px' }}>{i.suggestedIntervalHours != null ? `${i.suggestedIntervalHours} h` : '—'}</td>
                  <td style={{ padding: '8px 10px' }}>{i.sortOrder}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {isCommercialMode && (
        <div className="panel" style={{ padding: 20, borderRadius: 'var(--radius-xs)', border: '1px solid var(--border)', marginTop: 30 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12, flexWrap: 'wrap', marginBottom: 16 }}>
            <div>
              <h3 style={{ margin: 0, fontSize: '1.1rem' }}>{ar ? 'زيت بنود صيانة المولدات' : 'Generator Maintenance Oils'}</h3>
              <p style={{ margin: '4px 0 0', color: 'var(--text-muted)', fontSize: '0.875rem' }}>
                {ar ? 'حدّد الزيت والكمية لكل مولد وبند صيانة. لا يتم افتراض أي زيت أو خصمه إن لم يوجد ربط.' : 'Choose the stocked oil and quantity for each generator/service item. No oil is assumed or deducted without a configuration.'}
              </p>
            </div>
            <Button variant="primary" onClick={openOilConfigModal} disabled={!generators.length || !oils.length || !serviceItems.length}>
              <Plus size={16} /> {ar ? 'ربط زيت صيانة' : 'Configure Maintenance Oil'}
            </Button>
          </div>
          {maintenanceOilConfigs.length === 0 ? (
            <div style={{ padding: 18, color: 'var(--text-muted)', textAlign: 'center' }}>
              {ar ? 'لا توجد روابط زيت صيانة مضبوطة بعد.' : 'No maintenance-oil configurations have been set.'}
            </div>
          ) : (
            <div style={{ overflowX: 'auto' }}>
              <table className="table" style={{ width: '100%', fontSize: '0.9rem', textAlign: ar ? 'right' : 'left' }}>
                <thead><tr style={{ background: 'var(--surface-2)' }}>
                  <th style={{ padding: '8px 10px' }}>{ar ? 'المولد' : 'Generator'}</th>
                  <th style={{ padding: '8px 10px' }}>{ar ? 'بند الصيانة' : 'Service item'}</th>
                  <th style={{ padding: '8px 10px' }}>{ar ? 'الزيت' : 'Oil'}</th>
                  <th style={{ padding: '8px 10px' }}>{ar ? 'الكمية (لتر)' : 'Quantity (L)'}</th>
                  <th style={{ padding: '8px 10px' }}>{ar ? 'إجراء' : 'Action'}</th>
                </tr></thead>
                <tbody>{maintenanceOilConfigs.map(config => <tr key={config.id}>
                  <td style={{ padding: '8px 10px' }}>{config.generatorName}</td>
                  <td style={{ padding: '8px 10px' }}>{ar ? config.serviceItemNameAr : config.serviceItemNameEn}</td>
                  <td style={{ padding: '8px 10px' }}>{config.oilLookupName}</td>
                  <td style={{ padding: '8px 10px' }}>{Number(config.quantityLiters).toLocaleString('en-US', { maximumFractionDigits: 3 })}</td>
                  <td style={{ padding: '8px 10px' }}><button className="btn btn-sm btn-danger" onClick={() => handleDeleteOilConfig(config.id)}><Trash2 size={14} /></button></td>
                </tr>)}</tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {oilConfigModalOpen && (
        <Modal open={oilConfigModalOpen} onClose={() => setOilConfigModalOpen(false)} title={ar ? 'إعداد زيت بند صيانة' : 'Configure Maintenance Oil'}>
          <form onSubmit={handleSaveOilConfig}>
            {oilConfigError && <div style={{ background: 'var(--danger-subtle)', color: 'var(--danger)', padding: '10px 14px', borderRadius: 'var(--radius-xs)', marginBottom: 14 }}>{oilConfigError}</div>}
            <FormField label={ar ? 'المولد' : 'Generator'}>
              <select className="ft-input" required value={oilConfigGeneratorId} onChange={event => setOilConfigGeneratorId(event.target.value)} style={{ width: '100%', height: 38 }}>
                {generators.map(generator => <option key={generator.id} value={generator.id}>{generator.name}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'بند الصيانة' : 'Service item'}>
              <select className="ft-input" required value={oilConfigServiceItemId} onChange={event => setOilConfigServiceItemId(event.target.value)} style={{ width: '100%', height: 38 }}>
                {serviceItems.filter(item => item.isActive !== false).map(item => <option key={item.id} value={item.id}>{ar ? item.nameAr : item.nameEn}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'الزيت من المخزون' : 'Stocked oil'}>
              <select className="ft-input" required value={oilConfigOilId} onChange={event => setOilConfigOilId(event.target.value)} style={{ width: '100%', height: 38 }}>
                {oils.map(oil => <option key={oil.id} value={oil.id}>{oil.oilLookupName || oil.name}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'الكمية لكل صيانة (لتر)' : 'Quantity per service (L)'}>
              <Input type="number" min="0.001" step="0.001" required value={oilConfigQuantity} onChange={event => setOilConfigQuantity(event.target.value)} />
            </FormField>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 20 }}>
              <Button type="button" variant="secondary" onClick={() => setOilConfigModalOpen(false)}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              <Button type="submit" disabled={submittingOilConfig}>{submittingOilConfig ? (ar ? 'جارٍ الحفظ...' : 'Saving...') : (ar ? 'حفظ الربط' : 'Save configuration')}</Button>
            </div>
          </form>
        </Modal>
      )}

      {/* Add Definition Modal */}
      {defModalOpen && (
        <Modal
          open={defModalOpen}
          onClose={() => setDefModalOpen(false)}
          title={ar ? 'تعريف فترة صيانة لطراز مولد' : 'Define Service Interval for Model'}
        >
          <form onSubmit={handleSaveDefinition}>
            {defError && (
              <div style={{ background: 'var(--danger-subtle)', color: 'var(--danger)', padding: '10px 14px', borderRadius: 'var(--radius-xs)', marginBottom: 14, fontSize: '0.875rem' }}>
                {defError}
              </div>
            )}

            <FormField label={ar ? 'طراز المولد (مثال: Perkins 4012 أو QSK60)' : 'Generator Model (e.g. Perkins 4012, QSK60)'}>
              <Input
                type="text"
                required
                value={defModelKey}
                onChange={(e) => setDefModelKey(e.target.value)}
                placeholder="e.g. PERKINS 4012"
              />
            </FormField>

            <FormField label={ar ? 'بند الصيانة' : 'Service Item'}>
              <select
                className="ft-input"
                required
                value={defItemId}
                onChange={(e) => {
                  setDefItemId(e.target.value);
                  const found = serviceItems.find(i => String(i.id) === e.target.value);
                  if (found && found.suggestedIntervalHours) {
                    setDefInterval(String(found.suggestedIntervalHours));
                  }
                }}
                style={{ width: '100%', height: 38 }}
              >
                {serviceItems.map(i => (
                  <option key={i.id} value={i.id}>
                    {ar ? i.nameAr : i.nameEn} ({i.code})
                  </option>
                ))}
              </select>
            </FormField>

            <FormField label={ar ? 'فترة الصيانة (بالساعات)' : 'Interval Hours'}>
              <Input
                type="number"
                step="0.1"
                required
                value={defInterval}
                onChange={(e) => setDefInterval(e.target.value)}
                placeholder="e.g. 500"
              />
            </FormField>

            <FormField label={ar ? 'ساعات التحذير المسبق (اختياري، الافتراضي max(50, 10%))' : 'Warning Lead Hours (Optional, default max(50, 10%))'}>
              <Input
                type="number"
                step="0.1"
                value={defWarningLead}
                onChange={(e) => setDefWarningLead(e.target.value)}
                placeholder="e.g. 50"
              />
            </FormField>

            <FormField label={ar ? 'الأهمية' : 'Importance'}>
              <select
                className="ft-input"
                value={defImportance}
                onChange={(e) => setDefImportance(e.target.value)}
                style={{ width: '100%', height: 38 }}
              >
                <option value="1">{ar ? 'عادية (Normal)' : 'Normal'}</option>
                <option value="2">{ar ? 'عالية (High)' : 'High'}</option>
                <option value="3">{ar ? 'حرجة (Critical)' : 'Critical'}</option>
              </select>
            </FormField>

            <FormField label={ar ? 'ملاحظات' : 'Notes'}>
              <textarea
                className="ft-input"
                rows="2"
                value={defNotes}
                onChange={(e) => setDefNotes(e.target.value)}
                style={{ width: '100%' }}
              />
            </FormField>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 20 }}>
              <Button variant="secondary" type="button" onClick={() => setDefModalOpen(false)}>
                {ar ? 'إلغاء' : 'Cancel'}
              </Button>
              <Button variant="primary" type="submit" disabled={submittingDef}>
                {submittingDef ? (ar ? 'جاري الحفظ...' : 'Saving...') : (ar ? 'حفظ التعريف' : 'Save Definition')}
              </Button>
            </div>
          </form>
        </Modal>
      )}

      {/* Add Item Modal */}
      {itemModalOpen && (
        <Modal
          open={itemModalOpen}
          onClose={() => setItemModalOpen(false)}
          title={ar ? 'إضافة بند صيانة جديد' : 'Add New Service Item'}
        >
          <form onSubmit={handleSaveItem}>
            {itemError && (
              <div style={{ background: 'var(--danger-subtle)', color: 'var(--danger)', padding: '10px 14px', borderRadius: 'var(--radius-xs)', marginBottom: 14, fontSize: '0.875rem' }}>
                {itemError}
              </div>
            )}

            <FormField label={ar ? 'رمز البند (إنجليزي فريد، مثال: CoolantFilter)' : 'Item Code (Unique, e.g. CoolantFilter)'}>
              <Input
                type="text"
                required
                value={itemCode}
                onChange={(e) => setItemCode(e.target.value)}
                placeholder="e.g. CoolantFilter"
              />
            </FormField>

            <FormField label={ar ? 'الاسم بالعربية' : 'Arabic Name'}>
              <Input
                type="text"
                required
                value={itemNameAr}
                onChange={(e) => setItemNameAr(e.target.value)}
                placeholder="مثال: فلتر ماء التبريد"
              />
            </FormField>

            <FormField label={ar ? 'الاسم بالإنجليزية' : 'English Name'}>
              <Input
                type="text"
                required
                value={itemNameEn}
                onChange={(e) => setItemNameEn(e.target.value)}
                placeholder="e.g. Coolant Filter"
              />
            </FormField>

            <FormField label={ar ? 'الفترة المقترحة بالساعات (اختياري)' : 'Suggested Interval Hours (Optional)'}>
              <Input
                type="number"
                step="0.1"
                value={itemSuggestedHours}
                onChange={(e) => setItemSuggestedHours(e.target.value)}
                placeholder="e.g. 1000"
              />
            </FormField>

            <FormField label={ar ? 'ترتيب العرض' : 'Sort Order'}>
              <Input
                type="number"
                value={itemSortOrder}
                onChange={(e) => setItemSortOrder(e.target.value)}
              />
            </FormField>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 20 }}>
              <Button variant="secondary" type="button" onClick={() => setItemModalOpen(false)}>
                {ar ? 'إلغاء' : 'Cancel'}
              </Button>
              <Button variant="primary" type="submit" disabled={submittingItem}>
                {submittingItem ? (ar ? 'جاري الحفظ...' : 'Saving...') : (ar ? 'إضافة البند' : 'Add Item')}
              </Button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
