/**
 * ╔══════════════════════════════════════════════════════════════════════════╗
 * ║           صفحة التراجع عن الاعتماد — واجهة الويب (React)             ║
 * ║                                                                          ║
 * ║  متاحة للمسؤول (admin) فقط.                                           ║
 * ║  ملف: frontend/src/pages/RollbackApproval.js                           ║
 * ╚══════════════════════════════════════════════════════════════════════════╝
 */

import React, { useState, useCallback, useEffect } from 'react';
import client from '../api/client';
import { Undo2, Search, AlertTriangle, CheckCircle, XCircle, History, Copy } from 'lucide-react';
import Pagination from '../components/Pagination';
import ConfirmModal from '../components/ConfirmModal';

// ─────────────────────────────────────────────────────────────────────────────
// ثوابت
// ─────────────────────────────────────────────────────────────────────────────
const ENTITY_TYPES = [
  { value: 'generator_reading', label: 'قراءة مولد',    labelEn: 'Generator Reading', icon: '⚡' },
  { value: 'station_reading',   label: 'قراءة موقع',    labelEn: 'Station Reading',   icon: '📡' },
  { value: 'fuel_movement',     label: 'حركة وقود/زيت', labelEn: 'Fuel/Oil Movement', icon: '⛽' },
  { value: 'inventory',         label: 'جرد دوري',      labelEn: 'Periodic Inventory',icon: '📦' },
];

// ─────────────────────────────────────────────────────────────────────────────
// مكون رئيسي
// ─────────────────────────────────────────────────────────────────────────────
function RollbackApproval({ lang, user }) {
  const ar = lang === 'ar';

  // ── حالة النموذج ──────────────────────────────────────────────────────────
  const [entityType, setEntityType]   = useState('generator_reading');
  const [entityId,   setEntityId]     = useState('');
  const [reason,     setReason]       = useState('');

  // ── حالة العمليات ─────────────────────────────────────────────────────────
  const [checking,    setChecking]    = useState(false);
  const [executing,   setExecuting]   = useState(false);
  const [checkResult, setCheckResult] = useState(null);   // { ok, reason, preview, blockedBy }
  const [error,       setError]       = useState('');
  const [successMsg,  setSuccessMsg]  = useState('');
  const [confirmModal, setConfirmModal] = useState({ open: false, reason: '' });

  // ── حالة السجل ────────────────────────────────────────────────────────────
  const [tab,          setTab]          = useState('form');  // 'form' | 'history'
  const [history,      setHistory]      = useState([]);
  const [historyPage,  setHistoryPage]  = useState(1);
  const [historyLoading, setHistoryLoading] = useState(false);

  // ── حارس الصلاحية ─────────────────────────────────────────────────────────
  const isAdmin = user?.role === 'admin';

  // ── جلب السجل ─────────────────────────────────────────────────────────────
  const HISTORY_PAGE_SIZE = 15;

  const loadHistory = useCallback(async () => {
    setHistoryLoading(true);
    try {
      const res = await client.get('/api/rollback/history');
      setHistory(Array.isArray(res.data) ? res.data : []);
      setHistoryPage(1);
    } catch (e) {
      console.warn('history error', e);
    } finally {
      setHistoryLoading(false);
    }
  }, []);

  useEffect(() => {
    if (tab === 'history') loadHistory();
  }, [tab, loadHistory]);

  const historyTotalPages = Math.ceil(history.length / HISTORY_PAGE_SIZE);
  const historyPaginated = history.slice((historyPage - 1) * HISTORY_PAGE_SIZE, historyPage * HISTORY_PAGE_SIZE);
  const safeHistoryPage = Math.min(historyPage, historyTotalPages || 1);

  // ── فحص إمكانية التراجع ───────────────────────────────────────────────────
  const handleCheck = useCallback(async () => {
    if (!entityId.trim()) { setError(ar ? 'الرجاء إدخال معرّف العملية' : 'Please enter the operation ID'); return; }
    setChecking(true); setError(''); setSuccessMsg(''); setCheckResult(null);
    try {
      const res = await client.post('/api/rollback/check', { entityType, entityId: entityId.trim() });
      setCheckResult(res.data);
      if (!res.data.ok) setError(res.data.reason || (ar ? 'لا يمكن التراجع' : 'Rollback not possible'));
    } catch (e) {
      setError(e.response?.data?.reason || e.response?.data?.msg || e.message);
    } finally {
      setChecking(false);
    }
  }, [entityType, entityId]);

  // ── تنفيذ التراجع ─────────────────────────────────────────────────────────
  const handleExecute = useCallback(async () => {
    if (reason.trim().length < 5) { setError(ar ? 'سبب التراجع يجب أن يكون 5 أحرف على الأقل' : 'Rollback reason must be at least 5 characters'); return; }

    setConfirmModal({
      open: true,
      reason: reason.trim(),
      message: ar
        ? `هل أنت متأكد من التراجع عن هذا الاعتماد؟\n\nالسبب: ${reason}\n\nهذه العملية ستُعيد البيانات إلى ما قبل الاعتماد مباشرةً.`
        : `Are you sure you want to roll back this approval?\n\nReason: ${reason}\n\nThis will revert the data to its pre-approval state.`,
    });
  }, [entityType, entityId, reason, loadHistory]);

  const confirmExecute = async () => {
    const { reason: confirmReason } = confirmModal;
    setConfirmModal({ open: false, reason: '', message: '' });
    setExecuting(true); setError(''); setSuccessMsg('');
    try {
      await client.post('/api/rollback/execute', { entityType, entityId: entityId.trim(), reason: confirmReason });
      setSuccessMsg(ar ? '✓ تم التراجع بنجاح' : '✓ Rollback completed successfully');
      setCheckResult(null);
      setEntityId('');
      setReason('');
      await loadHistory();
    } catch (e) {
      setError(e.response?.data?.msg || e.response?.data?.reason || e.message);
    } finally {
      setExecuting(false);
    }
  };

  // ─────────────────────────────────────────────────────────────────────────
  if (!isAdmin) {
    return (
      <div className="rollback-page">
        <div className="rb-no-permission">
          <span style={{ fontSize: 48 }}>🔒</span>
          <h3>{ar ? 'صلاحية خاصة بمدير النظام' : 'Admin Access Only'}</h3>
          <p>{ar ? 'هذه الميزة متاحة لحساب المسؤول فقط.' : 'This feature is available to the administrator account only.'}</p>
        </div>
      </div>
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  return (
    <div className="rollback-page">

      {/* ─── رأس الصفحة ─────────────────────────────────────────────────── */}
      <div className="rb-header">
        <div className="rb-header-text">
          <Undo2 size={28} color="var(--warning-color, var(--warning))" />
          <div>
            <h1>{ar ? 'التراجع عن الاعتماد' : 'Approval Rollback'}</h1>
            <p>{ar ? 'تراجع آمن ومشروط عن آخر اعتماد — متاح للمسؤول فقط' : 'Safe conditional rollback of the last approval — admin only'}</p>
          </div>
        </div>
        {/* أزرار التبويب */}
        <div className="rb-tabs">
          <button className={`rb-tab ${tab === 'form' ? 'active' : ''}`} onClick={() => setTab('form')}>
            <Undo2 size={15} /> {ar ? 'تنفيذ التراجع' : 'Execute Rollback'}
          </button>
          <button className={`rb-tab ${tab === 'history' ? 'active' : ''}`} onClick={() => setTab('history')}>
            <History size={15} /> {ar ? 'السجل' : 'History'}
          </button>
        </div>
      </div>

      {/* ─── تبويب النموذج ───────────────────────────────────────────────── */}
      {tab === 'form' && (
        <div className="rb-form-wrap">

          {/* تحذير */}
          <div className="rb-warning-banner">
            <AlertTriangle size={18} color="var(--warning)" />
            <p>
              {ar
                ? <>يُتيح هذا القسم التراجع عن <strong>آخر اعتماد فقط</strong> إذا لم تكن هناك سلسلة اعتمادات لاحقة مترتبة عليه. ستُعاد البيانات إلى حالتها قبل الاعتماد مباشرةً، وتُسجَّل كل عملية في سجل التدقيق.</>  
                : <>This section allows rollback of the <strong>last approval only</strong> if no subsequent dependent approvals exist. Data will be reverted to its pre-approval state, and every operation is recorded in the audit log.</>
              }
            </p>
          </div>

          {/* نوع العملية */}
          <div className="rb-section">
            <label className="rb-label">{ar ? 'نوع العملية' : 'Operation Type'}</label>
            <div className="rb-entity-types">
              {ENTITY_TYPES.map(et => (
                <button
                  key={et.value}
                  className={`rb-entity-btn ${entityType === et.value ? 'active' : ''}`}
                  aria-pressed={entityType === et.value}
                  onClick={() => { setEntityType(et.value); setCheckResult(null); setError(''); setSuccessMsg(''); }}
                >
                  <span>{et.icon}</span>
                  <span>{ar ? et.label : et.labelEn}</span>
                </button>
              ))}
            </div>
          </div>

          {/* معرّف العملية */}
          <div className="rb-section">
            <label className="rb-label" htmlFor="rollback-operation-id">{ar ? 'معرّف العملية (ID)' : 'Operation ID'}</label>
            <div className="rb-id-row">
              <input
                id="rollback-operation-id"
                className="rb-input"
                placeholder={ar ? 'الصق هنا معرّف (ID) العملية' : 'Paste the operation ID here'}
                value={entityId}
                onChange={e => { setEntityId(e.target.value); setCheckResult(null); setError(''); setSuccessMsg(''); }}
              />
              <button className="rb-paste-btn" aria-label={ar ? 'لصق من الحافظة' : 'Paste from clipboard'} title={ar ? 'لصق من الحافظة' : 'Paste from clipboard'} onClick={async () => {
                try {
                  const text = await navigator.clipboard.readText();
                  if (text?.trim()) { setEntityId(text.trim()); setCheckResult(null); }
                } catch {}
              }}>
                <Copy size={15} />
              </button>
              {entityId && (
                <button className="rb-clear-btn" onClick={() => { setEntityId(''); setCheckResult(null); setError(''); }}>✕</button>
              )}
            </div>
          </div>

          {/* زر الفحص */}
          <button
            className="rb-check-btn"
            disabled={checking || !entityId.trim()}
            onClick={handleCheck}
          >
            {checking
              ? <><span className="rb-spinner" /> {ar ? 'جارىن الفحص…' : 'Checking…'}</>
              : <><Search size={16} /> {ar ? 'فحص إمكانية التراجع' : 'Check Rollback Eligibility'}</>}
          </button>

          {/* رسالة خطأ */}
          {error && (
            <div className="rb-error-box">
              <XCircle size={17} />
              <span>{error}</span>
            </div>
          )}

          {/* رسالة نجاح */}
          {successMsg && (
            <div className="rb-success-box">
              <CheckCircle size={17} />
              <span>{successMsg}</span>
            </div>
          )}

          {/* نتيجة الفحص */}
          {checkResult && (
            <div className={`rb-result-card ${checkResult.ok ? 'ok' : 'blocked'}`}>
              <div className="rb-result-header">
                {checkResult.ok
                  ? <><CheckCircle size={22} color="var(--success)" /> <span style={{color:'var(--success)', fontWeight:900}}>{ar ? 'يمكن التراجع ✓' : 'Rollback possible ✓'}</span></>
                  : <><XCircle    size={22} color="var(--danger)" /> <span style={{color:'var(--danger)', fontWeight:900}}>{ar ? 'لا يمكن التراجع ✗' : 'Rollback not possible ✗'}</span></>
                }
                <button className="rb-close-result" onClick={() => setCheckResult(null)}>✕</button>
              </div>

              {/* سبب المنع */}
              {!checkResult.ok && (
                <div className="rb-blocked-reason">
                  <p>{checkResult.reason}</p>
                  {checkResult.blockedBy && (
                    <p style={{fontSize:11, color:'var(--text-muted)', fontFamily:'monospace', marginTop:4}}>
                      {ar ? 'معرّف العملية الحاجبة' : 'Blocking operation ID'}: {checkResult.blockedBy.id ?? '—'}
                    </p>
                  )}
              </div>
            )}

              {/* ملخص التأثيرات */}
              {checkResult.ok && checkResult.preview && (
                <>
                  <div className="rb-preview">
                    <p className="rb-preview-summary">{checkResult.preview.summary}</p>
                    <ul className="rb-preview-changes">
                      {checkResult.preview.changes.map((c, i) => (
                        <li key={i}>↩ {c}</li>
                      ))}
                    </ul>
                  </div>

                  {/* حقل سبب التراجع */}
                  <div className="rb-section" style={{marginTop:16}}>
                    <label className="rb-label">{ar ? 'سبب التراجع *' : 'Rollback Reason *'}</label>
                    <textarea
                      className="rb-textarea"
                      placeholder={ar ? 'اكتب هنا سبب التراجع بوضوح…' : 'Write the rollback reason clearly…'}
                      maxLength={250}
                      rows={3}
                      value={reason}
                      onChange={e => { setReason(e.target.value); setError(''); }}
                    />
                    <span className="rb-char-count">{reason.length}/250</span>
                  </div>

                  {/* زر التنفيذ */}
                  <button
                    className="rb-execute-btn"
                    disabled={executing || reason.trim().length < 5}
                    onClick={handleExecute}
                  >
                    {executing
                      ? <><span className="rb-spinner" /> {ar ? 'جارىن التراجع…' : 'Rolling back…'}</>
                      : <><Undo2 size={16} /> {ar ? 'تنفيذ التراجع' : 'Execute Rollback'}</>}
                  </button>
                </>
              )}
            </div>
          )}
        </div>
      )}

      {/* ─── تبويب السجل ────────────────────────────────────────────────── */}
      {tab === 'history' && (
        <div className="rb-history-wrap">
          {historyLoading && history.length === 0 && (
            <div className="rb-loading">{ar ? 'جارىن تحميل السجل…' : 'Loading history…'}</div>
          )}

          {!historyLoading && history.length === 0 && (
            <div className="rb-empty-history">
              <History size={40} color="var(--text-faint)" />
              <p>{ar ? 'لا توجد عمليات تراجع مسجلة' : 'No rollback operations recorded'}</p>
            </div>
          )}

          {historyPaginated.map(log => {
            const dateStr = log.timestamp ? log.timestamp.split('T')[0] : '—';
            const timeStr = log.timestamp?.length > 18 ? log.timestamp.substring(11, 19) : '—';
            return (
              <div key={log.id} className="rb-history-card">
                <div className="rb-history-head">
                  <span className="rb-history-action">↩ {log.action}</span>
                  <span className="rb-history-date">{dateStr} &nbsp; {timeStr}</span>
                </div>
                <div className="rb-history-meta">
                  <span>👤 {log.userName ?? log.userId ?? '—'}</span>
                  <span style={{fontFamily:'monospace', fontSize:11}}>🔑 {log.entityId ?? '—'}</span>
                </div>
              </div>
            )})}

          {historyTotalPages > 1 && (
            <div className="rb-pagination" style={{ padding: '12px 16px' }}>
              <Pagination page={safeHistoryPage} totalPages={historyTotalPages}
                total={history.length} pageSize={HISTORY_PAGE_SIZE} lang={lang}
                onPageChange={setHistoryPage} />
            </div>
          )}
        </div>
      )}

      {/* ─── CSS مدمج ────────────────────────────────────────────────────── */}
      <style>{`
        .rollback-page { padding: 24px; max-width: 780px; margin: 0 auto; font-family: inherit; }
        .rb-no-permission { text-align:center; padding:60px 20px; color: var(--text-muted); }
        .rb-no-permission h3 { margin:12px 0 6px; font-size:1.2rem; }

        /* Header */
        .rb-header { display:flex; justify-content:space-between; align-items:flex-start; flex-wrap:wrap; gap:14px; margin-bottom:26px; }
        .rb-header-text { display:flex; align-items:center; gap:12px; }
        .rb-header-text h1 { margin:0; font-size:1.5rem; font-weight:900; }
        .rb-header-text p  { margin:3px 0 0; font-size:0.88rem; color:var(--text-muted); }
        .rb-tabs { display:flex; gap:8px; }
        .rb-tab  { display:flex; align-items:center; gap:6px; padding:8px 16px; border-radius:10px; border:1px solid var(--border); background:var(--surface-2); color:var(--text-muted); cursor:pointer; font-size:0.88rem; transition:all .2s; }
        .rb-tab:hover  { background:var(--surface-3); color:var(--text); }
        .rb-tab.active { background:var(--primary-subtle); border-color:var(--primary); color:var(--primary); font-weight:700; }

        /* Form */
        .rb-form-wrap  { display:flex; flex-direction:column; gap:0; }
        .rb-section    { margin-bottom:18px; }
        .rb-label      { display:block; font-size:0.85rem; font-weight:600; color:var(--text-muted); margin-bottom:7px; }

        /* Warning */
        .rb-warning-banner { display:flex; align-items:flex-start; gap:10px; padding:13px 15px; background:var(--warning-subtle); border:1px solid var(--warning); border-radius:12px; margin-bottom:22px; }
        .rb-warning-banner p { margin:0; font-size:0.84rem; color:var(--warning); line-height:1.6; }

        /* Entity type buttons */
        .rb-entity-types { display:flex; flex-wrap:wrap; gap:9px; }
        .rb-entity-btn   { display:flex; align-items:center; gap:7px; padding:9px 14px; border-radius:10px; border:1px solid var(--border); background:var(--surface-2); color:var(--text); cursor:pointer; font-size:0.88rem; font-family:inherit; transition:all .2s; }
        .rb-entity-btn:hover  { border-color:var(--primary); background:var(--primary-subtle); }
        .rb-entity-btn.active { border-color:var(--primary); background:var(--primary-subtle); color:var(--primary); font-weight:700; }

        /* Input */
        .rb-id-row    { display:flex; gap:8px; align-items:center; }
        .rb-input     { flex:1; padding:11px 14px; background:var(--surface-2); border:1px solid var(--border); border-radius:10px; color:var(--text); font-size:0.9rem; outline:none; transition:border .2s; }
        .rb-input:focus { border-color:var(--primary); }
        .rb-paste-btn { padding:10px 12px; background:var(--surface-2); border:1px solid var(--border); border-radius:10px; color:var(--text-muted); cursor:pointer; transition:all .2s; }
        .rb-paste-btn:hover { background:var(--surface-3); color:var(--text); }
        .rb-clear-btn { padding:10px 12px; background:var(--danger-subtle); border:1px solid var(--danger); border-radius:10px; color:var(--danger); cursor:pointer; font-size:14px; }

        .rb-textarea { width:100%; box-sizing:border-box; padding:11px 14px; background:var(--surface-2); border:1px solid var(--border); border-radius:10px; color:var(--text); font-size:0.88rem; resize:vertical; outline:none; font-family:inherit; transition:border .2s; }
        .rb-textarea:focus { border-color:var(--warning); }
        .rb-char-count { font-size:11px; color:var(--text-faint); float:left; margin-top:4px; }

        /* Buttons */
        .rb-check-btn   { display:flex; align-items:center; justify-content:center; gap:8px; width:100%; padding:13px; border-radius:12px; border:none; background:var(--primary); color:var(--on-accent); font-size:0.95rem; font-weight:700; cursor:pointer; transition:all .2s; margin-bottom:14px; }
        .rb-check-btn:hover:not(:disabled) { filter:brightness(1.08); }
        .rb-check-btn:disabled { opacity:.5; cursor:not-allowed; }
        .rb-execute-btn { display:flex; align-items:center; justify-content:center; gap:8px; width:100%; padding:13px; border-radius:12px; border:none; background:var(--warning); color:var(--on-accent); font-size:0.95rem; font-weight:700; cursor:pointer; transition:all .2s; margin-top:4px; }
        .rb-execute-btn:hover:not(:disabled) { filter:brightness(1.08); }
        .rb-execute-btn:disabled { opacity:.5; cursor:not-allowed; }

        /* Boxes */
        .rb-error-box   { display:flex; align-items:flex-start; gap:9px; padding:12px 14px; background:var(--danger-subtle); border:1px solid var(--danger); border-radius:10px; color:var(--danger); font-size:0.88rem; margin-bottom:14px; }
        .rb-success-box { display:flex; align-items:center; gap:9px; padding:12px 14px; background:var(--success-subtle); border:1px solid var(--success); border-radius:10px; color:var(--success); font-size:0.88rem; font-weight:700; margin-bottom:14px; }

        /* Result card */
        .rb-result-card { padding:18px; border-radius:14px; border:1.5px solid var(--border); margin-top:4px; }
        .rb-result-card.ok      { background:var(--success-subtle); border-color:var(--success); }
        .rb-result-card.blocked { background:var(--danger-subtle); border-color:var(--danger); }
        .rb-result-header { display:flex; align-items:center; gap:10px; margin-bottom:14px; }
        .rb-result-header > :last-child { margin-inline-end:auto; }
        .rb-close-result { background:none; border:none; color:var(--text-muted); cursor:pointer; font-size:16px; padding:0 4px; }
        .rb-close-result:hover { color:var(--text); }
        .rb-blocked-reason { padding:11px 14px; background:var(--danger-subtle); border-radius:10px; font-size:0.86rem; color:var(--danger); }
        .rb-preview { padding:12px 0; }
        .rb-preview-summary { font-weight:700; margin:0 0 10px; }
        .rb-preview-changes  { padding-inline-end:16px; margin:0; list-style:none; }
        .rb-preview-changes li { padding:4px 0; font-size:0.86rem; color:var(--text); position:relative; }
        .rb-preview-changes li::before { content:''; }

        /* History */
        .rb-history-wrap  { display:flex; flex-direction:column; gap:10px; padding-top:4px; }
        .rb-loading, .rb-empty-history { text-align:center; padding:40px; color:var(--text-faint); }
        .rb-empty-history p { margin:10px 0 0; }
        .rb-history-card  { padding:14px 16px; background:var(--surface-2); border:1px solid var(--border); border-radius:12px; }
        .rb-history-head  { display:flex; justify-content:space-between; align-items:center; margin-bottom:7px; }
        .rb-history-action { font-weight:700; color:var(--warning); font-size:0.88rem; }
        .rb-history-date   { font-size:0.8rem; color:var(--text-faint); }
        .rb-history-meta   { display:flex; gap:16px; font-size:0.82rem; color:var(--text-muted); margin-bottom:5px; }
        .rb-history-reason { font-size:0.82rem; color:var(--text); }
        .rb-pagination { display:flex; align-items:center; justify-content:center; gap:12px; margin-top:14px; }
        .rb-pagination button { padding:7px 12px; background:var(--surface-2); border:1px solid var(--border); border-radius:8px; color:var(--text-muted); cursor:pointer; }
        .rb-pagination button:disabled { opacity:.35; cursor:not-allowed; }
        .rb-pagination span { font-size:0.88rem; color:var(--text-faint); }

        /* Spinner */
        .rb-spinner { width:15px; height:15px; border:2px solid var(--border); border-top-color:currentColor; border-radius:50%; animation:rb-spin .7s linear infinite; display:inline-block; }
        @keyframes rb-spin { to { transform:rotate(360deg); } }

        /* Light mode: keep the form distinct from the page background. */
        .theme-light .rollback-page { max-width: 900px; padding: 32px 24px; }
        .theme-light .rb-header { align-items: center; margin-bottom: 24px; gap: 20px; }
        .theme-light .rb-header-text > svg { flex-shrink: 0; padding: 11px; box-sizing: content-box; border-radius: 15px; background: #fff3df; color: #986014; }
        .theme-light .rb-header-text h1 { color: #1d3047; font-size: 1.5rem; }
        .theme-light .rb-header-text p { color: #53657a; line-height: 1.7; }
        .theme-light .rb-tabs { padding: 4px; border: 1px solid #dce4eb; border-radius: 13px; background: #fff; }
        .theme-light .rb-tab { border-color: transparent; background: transparent; color: #43576b; font-weight: 600; }
        .theme-light .rb-tab.active { background: #e6f1fa; border-color: #c0d9ec; color: #155a91; }
        .theme-light .rb-form-wrap, .theme-light .rb-history-wrap { padding: 28px; background: #fff; border: 1px solid #dce4eb; border-radius: 18px; box-shadow: 0 12px 34px rgba(33, 52, 73, .07); }
        .theme-light .rb-section { margin-bottom: 24px; }
        .theme-light .rb-label { color: #344a60; font-size: .9rem; font-weight: 700; margin-bottom: 10px; }
        .theme-light .rb-warning-banner { background: #fff8eb; border-color: #ead1a7; margin-bottom: 28px; padding: 15px 17px; }
        .theme-light .rb-warning-banner > svg { flex-shrink: 0; margin-top: 2px; }
        .theme-light .rb-warning-banner p { color: #79500f; font-size: .88rem; line-height: 1.8; }
        .theme-light .rb-entity-types { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 10px; }
        .theme-light .rb-entity-btn { min-height: 54px; justify-content: center; background: #f8fafc; border-color: #d5e0e9; color: #344a60; }
        .theme-light .rb-entity-btn.active { background: #e8f3fb; border-color: #2472ad; color: #145a91; box-shadow: inset 0 0 0 1px #2472ad; }
        .theme-light .rb-input, .theme-light .rb-paste-btn, .theme-light .rb-textarea { min-height: 50px; background: #fbfcfe; border-color: #c7d4df; color: #253b50; font-family: inherit; }
        .theme-light .rb-input { min-width: 0; }
        .theme-light .rb-input::placeholder, .theme-light .rb-textarea::placeholder { color: #6b7d8f; opacity: 1; }
        .theme-light .rb-input:focus, .theme-light .rb-textarea:focus { border-color: #2472ad; box-shadow: 0 0 0 3px rgba(36, 114, 173, .14); }
        .theme-light .rb-check-btn { min-height: 52px; margin: 2px 0 0; background: #246ba3; }
        .theme-light .rb-check-btn:disabled { opacity: 1; background: #e1e9ef; color: #53697b; }
        .theme-light .rb-result-card, .theme-light .rb-error-box, .theme-light .rb-success-box { margin-top: 18px; }
        @media (max-width: 700px) {
          .theme-light .rollback-page { padding: 20px 14px; }
          .theme-light .rb-form-wrap, .theme-light .rb-history-wrap { padding: 20px; }
          .theme-light .rb-entity-types { grid-template-columns: repeat(2, minmax(0, 1fr)); }
        }

        @media (max-width:600px) {
          .rb-header { flex-direction:column; }
          .rb-tabs   { width:100%; }
          .rb-tab    { flex:1; justify-content:center; }
          .rb-entity-types { flex-direction:column; }
          .rb-entity-btn   { width:100%; }
        }
      `}</style>

      <ConfirmModal
        open={confirmModal.open}
        title={ar ? 'تأكيد التراجع' : 'Confirm Rollback'}
        message={confirmModal.message}
        confirmText={ar ? 'نعم، تراجع' : 'Yes, Rollback'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        danger
        onConfirm={confirmExecute}
        onCancel={() => setConfirmModal({ open: false, reason: '', message: '' })}
      />
    </div>
  );
}

export default RollbackApproval;
