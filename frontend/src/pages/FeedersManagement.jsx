import React, { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pencil, Plus, Power, RefreshCw } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import { Button, FormField, Input, Modal, PageHeader } from '../components/ui';

const blankForm = { code: '', name: '', meterMultiplier: '1', meterId: '' };

export default function FeedersManagement({ lang, currentUser }) {
  const { t, i18n } = useTranslation();
  const ar = lang === 'ar';
  const [sites, setSites] = useState([]);
  const [meters, setMeters] = useState([]);
  const [feeders, setFeeders] = useState([]);
  const [siteId, setSiteId] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(blankForm);
  const [confirmDeactivate, setConfirmDeactivate] = useState(null);
  const isAdmin = currentUser?.role === 'admin';

  useEffect(() => { if (lang && i18n.language !== lang) i18n.changeLanguage(lang); }, [i18n, lang]);

  const loadSites = useCallback(async () => {
    try {
      const response = await client.get('/api/sub-stations');
      setSites(response.data?.items || response.data || []);
    } catch {
      setError(t('feeders.errors.loadSites'));
    }
  }, [t]);

  const loadFeeders = useCallback(async (selectedSiteId) => {
    if (!selectedSiteId) { setFeeders([]); setMeters([]); return; }
    setLoading(true);
    setError('');
    try {
      const [feedersResponse, metersResponse] = await Promise.all([
        client.get(`/api/operational-feeders/by-site/${selectedSiteId}`),
        client.get('/api/meters', { params: { subStationId: selectedSiteId } }),
      ]);
      setFeeders(feedersResponse.data || []);
      setMeters(metersResponse.data?.items || metersResponse.data || []);
    } catch {
      setError(t('feeders.errors.loadFeeders'));
    } finally {
      setLoading(false);
    }
  }, [t]);

  useEffect(() => { loadSites(); }, [loadSites]);
  useEffect(() => { loadFeeders(siteId); }, [loadFeeders, siteId]);

  const openCreate = () => {
    setEditing(null);
    setForm(blankForm);
    setShowForm(true);
  };

  const openEdit = (feeder) => {
    setEditing(feeder);
    setForm({ code: feeder.code, name: feeder.name, meterMultiplier: String(feeder.meterMultiplier), meterId: feeder.meterId || '' });
    setShowForm(true);
  };

  const save = async (event) => {
    event.preventDefault();
    if (!siteId) return;
    setError('');
    const payload = { name: form.name, meterMultiplier: Number(form.meterMultiplier), meterId: form.meterId ? Number(form.meterId) : null };
    try {
      if (editing) {
        await client.put(`/api/operational-feeders/${editing.id}`, { ...payload, isActive: true });
      } else {
        await client.post('/api/operational-feeders', { subStationId: Number(siteId), code: form.code, ...payload });
      }
      setShowForm(false);
      await loadFeeders(siteId);
    } catch (requestError) {
      setError(requestError.response?.data?.error || t('feeders.errors.save'));
    }
  };

  const deactivate = async () => {
    try {
      await client.post(`/api/operational-feeders/${confirmDeactivate.id}/deactivate`);
      setConfirmDeactivate(null);
      await loadFeeders(siteId);
    } catch (requestError) {
      setError(requestError.response?.data?.error || t('feeders.errors.deactivate'));
    }
  };

  const createDefaults = async () => {
    try {
      const response = await client.post('/api/operational-feeders/create-defaults');
      await loadFeeders(siteId);
      setError(response.data?.created ? '' : t('feeders.defaultsAlreadyExist'));
    } catch (requestError) {
      setError(requestError.response?.data?.error || t('feeders.errors.defaults'));
    }
  };

  return (
    <div>
      <PageHeader
        title={t('feeders.title')}
        subtitle={t('feeders.subtitle')}
        actions={<div style={{ display: 'flex', gap: 8 }}>
          <Button variant="ghost" size="sm" icon={RefreshCw} onClick={() => loadFeeders(siteId)} />
          {isAdmin && <Button variant="secondary" onClick={createDefaults}>{t('feeders.createDefaults')}</Button>}
          <Button icon={Plus} onClick={openCreate} disabled={!siteId}>{t('feeders.add')}</Button>
        </div>}
      />

      <FormField label={t('feeders.site')}>
        <select className="form-control" value={siteId} onChange={event => setSiteId(event.target.value)}>
          <option value="">{t('feeders.selectSite')}</option>
          {sites.map(site => <option key={site.id} value={site.id}>{site.name}</option>)}
        </select>
      </FormField>

      {error && <div className="alert alert-danger" style={{ marginTop: 16 }}>{error}</div>}

      {siteId && (
        <div className="glass-card" style={{ marginTop: 16, padding: 0, overflow: 'hidden' }}>
          {loading ? <div style={{ padding: 24 }}>{t('common.loading')}</div> : feeders.length === 0 ? (
            <div style={{ padding: 24, textAlign: 'center', color: 'var(--text-muted)' }}>{t('feeders.empty')}</div>
          ) : (
            <table className="data-table">
              <thead><tr><th>{t('feeders.code')}</th><th>{t('feeders.name')}</th><th>{t('feeders.multiplier')}</th><th>{t('feeders.meter')}</th><th>{t('feeders.actions')}</th></tr></thead>
              <tbody>{feeders.map(feeder => (
                <tr key={feeder.id}>
                  <td>{feeder.code}</td><td>{feeder.name}</td><td>{feeder.meterMultiplier}</td>
                  <td>{meters.find(meter => meter.id === feeder.meterId)?.name || '—'}</td>
                  <td><div style={{ display: 'flex', gap: 6 }}>
                    <Button size="sm" variant="ghost" icon={Pencil} onClick={() => openEdit(feeder)}>{t('feeders.edit')}</Button>
                    <Button size="sm" variant="danger" icon={Power} onClick={() => setConfirmDeactivate(feeder)}>{t('feeders.deactivate')}</Button>
                  </div></td>
                </tr>
              ))}</tbody>
            </table>
          )}
        </div>
      )}

      {showForm && <Modal title={editing ? t('feeders.edit') : t('feeders.add')} onClose={() => setShowForm(false)}>
        <form onSubmit={save} style={{ display: 'grid', gap: 16 }}>
          {!editing && <FormField label={t('feeders.code')}><Input value={form.code} onChange={event => setForm(current => ({ ...current, code: event.target.value }))} required /></FormField>}
          <FormField label={t('feeders.name')}><Input value={form.name} onChange={event => setForm(current => ({ ...current, name: event.target.value }))} required /></FormField>
          <FormField label={t('feeders.multiplier')}><Input type="number" min="0.0001" step="0.0001" value={form.meterMultiplier} onChange={event => setForm(current => ({ ...current, meterMultiplier: event.target.value }))} required /></FormField>
          <FormField label={t('feeders.meter')}><select className="form-control" value={form.meterId} onChange={event => setForm(current => ({ ...current, meterId: event.target.value }))}><option value="">{t('feeders.noMeter')}</option>{meters.map(meter => <option key={meter.id} value={meter.id}>{meter.name}</option>)}</select></FormField>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}><Button type="button" variant="secondary" onClick={() => setShowForm(false)}>{t('common.cancel')}</Button><Button type="submit">{t('common.save')}</Button></div>
        </form>
      </Modal>}

      <ConfirmModal open={Boolean(confirmDeactivate)} title={t('feeders.deactivate')} message={t('feeders.deactivateConfirm')} confirmText={t('feeders.deactivate')} cancelText={t('common.cancel')} danger onConfirm={deactivate} onCancel={() => setConfirmDeactivate(null)} />
    </div>
  );
}
