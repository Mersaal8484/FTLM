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
            <label className="rb-label">{ar ? 'معرّف العملية (ID)' : 'Operation ID'}</label>
            <div className="rb-id-row">
              <input
                className="rb-input"
                placeholder={ar ? 'الصق هنا معرّف (ID) العملية' : 'Paste the operation ID here'}
                value={entityId}
                onChange={e => { setEntityId(e.target.value); setCheckResult(null); setError(''); setSuccessMsg(''); }}
                style={{ fontFamily: 'monospace' }}
              />
              <button className="rb-paste-btn" title={ar ? 'لصق من الحافظة' : 'Paste from clipboard'} onClick={async () => {
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
                  ? <><CheckCircle size={22} color="#10b981" /> <span style={{color:'#10b981', fontWeight:900}}>{ar ? 'يمكن التراجع ✓' : 'Rollback possible ✓'}</span></>
                  : <><XCircle    size={22} color="#ef4444" /> <span style={{color:'#ef4444', fontWeight:900}}>{ar ? 'لا يمكن التراجع ✗' : 'Rollback not possible ✗'}</span></>
                }
                <button className="rb-close-result" onClick={() => setCheckResult(null)}>✕</button>
              </div>

              {/* سبب المنع */}
              {!checkResult.ok && (
                <div className="rb-blocked-reason">
                  <p>{checkResult.reason}</p>
                  {checkResult.blockedBy && (
                    <p style={{fontSize:11, color:'#94a3b8', fontFamily:'monospace', marginTop:4}}>
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
              <History size={40} color="#475569" />
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
        .rb-header-text p  { margin:3px 0 0; font-size:0.88rem; color: var(--text-muted,#94a3b8); }
        .rb-tabs { display:flex; gap:8px; }
        .rb-tab  { display:flex; align-items:center; gap:6px; padding:8px 16px; border-radius:10px; border:1px solid rgba(255,255,255,0.08); background:rgba(255,255,255,0.03); color:var(--text-muted,#94a3b8); cursor:pointer; font-size:0.88rem; transition:all .2s; }
        .rb-tab:hover  { background:rgba(255,255,255,0.07); color:#fff; }
        .rb-tab.active { background:rgba(59,130,246,0.15); border-color:rgba(59,130,246,0.3); color:#60a5fa; font-weight:700; }

        /* Form */
        .rb-form-wrap  { display:flex; flex-direction:column; gap:0; }
        .rb-section    { margin-bottom:18px; }
        .rb-label      { display:block; font-size:0.85rem; font-weight:600; color:var(--text-muted,#94a3b8); margin-bottom:7px; }

        /* Warning */
        .rb-warning-banner { display:flex; align-items:flex-start; gap:10px; padding:13px 15px; background:rgba(245,158,11,0.09); border:1px solid rgba(245,158,11,0.28); border-radius:12px; margin-bottom:22px; }
        .rb-warning-banner p { margin:0; font-size:0.84rem; color:#fbbf24; line-height:1.6; }

        /* Entity type buttons */
        .rb-entity-types { display:flex; flex-wrap:wrap; gap:9px; }
        .rb-entity-btn   { display:flex; align-items:center; gap:7px; padding:9px 14px; border-radius:10px; border:1px solid rgba(255,255,255,0.08); background:rgba(255,255,255,0.03); color:var(--text); cursor:pointer; font-size:0.88rem; font-family:inherit; transition:all .2s; }
        .rb-entity-btn:hover  { border-color:rgba(59,130,246,0.3); background:rgba(59,130,246,0.08); }
        .rb-entity-btn.active { border-color:rgba(59,130,246,0.5); background:rgba(59,130,246,0.15); color:#60a5fa; font-weight:700; }

        /* Input */
        .rb-id-row    { display:flex; gap:8px; align-items:center; }
        .rb-input     { flex:1; padding:11px 14px; background:rgba(255,255,255,0.04); border:1px solid rgba(255,255,255,0.08); border-radius:10px; color:var(--text,#fff); font-size:0.9rem; outline:none; transition:border .2s; }
        .rb-input:focus { border-color:rgba(59,130,246,0.5); }
        .rb-paste-btn { padding:10px 12px; background:rgba(255,255,255,0.05); border:1px solid rgba(255,255,255,0.08); border-radius:10px; color:#94a3b8; cursor:pointer; transition:all .2s; }
        .rb-paste-btn:hover { background:rgba(255,255,255,0.1); color:#fff; }
        .rb-clear-btn { padding:10px 12px; background:rgba(239,68,68,0.1); border:1px solid rgba(239,68,68,0.2); border-radius:10px; color:#f87171; cursor:pointer; font-size:14px; }

        .rb-textarea { width:100%; box-sizing:border-box; padding:11px 14px; background:rgba(255,255,255,0.04); border:1px solid rgba(255,255,255,0.08); border-radius:10px; color:var(--text,#fff); font-size:0.88rem; resize:vertical; outline:none; font-family:inherit; transition:border .2s; }
        .rb-textarea:focus { border-color:rgba(245,158,11,0.5); }
        .rb-char-count { font-size:11px; color:#64748b; float:left; margin-top:4px; }

        /* Buttons */
        .rb-check-btn   { display:flex; align-items:center; justify-content:center; gap:8px; width:100%; padding:13px; border-radius:12px; border:none; background:rgba(59,130,246,0.75); color:#fff; font-size:0.95rem; font-weight:700; cursor:pointer; transition:all .2s; margin-bottom:14px; }
        .rb-check-btn:hover:not(:disabled) { background:rgba(59,130,246,0.95); }
        .rb-check-btn:disabled { opacity:.5; cursor:not-allowed; }
        .rb-execute-btn { display:flex; align-items:center; justify-content:center; gap:8px; width:100%; padding:13px; border-radius:12px; border:none; background:rgba(245,158,11,0.85); color:#fff; font-size:0.95rem; font-weight:700; cursor:pointer; transition:all .2s; margin-top:4px; }
        .rb-execute-btn:hover:not(:disabled) { background:rgba(245,158,11,1); }
        .rb-execute-btn:disabled { opacity:.5; cursor:not-allowed; }

        /* Boxes */
        .rb-error-box   { display:flex; align-items:flex-start; gap:9px; padding:12px 14px; background:rgba(239,68,68,0.1); border:1px solid rgba(239,68,68,0.25); border-radius:10px; color:#f87171; font-size:0.88rem; margin-bottom:14px; }
        .rb-success-box { display:flex; align-items:center; gap:9px; padding:12px 14px; background:rgba(16,185,129,0.1); border:1px solid rgba(16,185,129,0.25); border-radius:10px; color:#34d399; font-size:0.88rem; font-weight:700; margin-bottom:14px; }

        /* Result card */
        .rb-result-card { padding:18px; border-radius:14px; border:1.5px solid rgba(255,255,255,0.08); margin-top:4px; }
        .rb-result-card.ok      { background:rgba(16,185,129,0.07); border-color:rgba(16,185,129,0.3); }
        .rb-result-card.blocked { background:rgba(239,68,68,0.07); border-color:rgba(239,68,68,0.3); }
        .rb-result-header { display:flex; align-items:center; gap:10px; margin-bottom:14px; }
        .rb-result-header > :last-child { margin-inline-end:auto; }
        .rb-close-result { background:none; border:none; color:#94a3b8; cursor:pointer; font-size:16px; padding:0 4px; }
        .rb-close-result:hover { color:#fff; }
        .rb-blocked-reason { padding:11px 14px; background:rgba(239,68,68,0.1); border-radius:10px; font-size:0.86rem; color:#f87171; }
        .rb-preview { padding:12px 0; }
        .rb-preview-summary { font-weight:700; margin:0 0 10px; }
        .rb-preview-changes  { padding-inline-end:16px; margin:0; list-style:none; }
        .rb-preview-changes li { padding:4px 0; font-size:0.86rem; color:#cbd5e1; position:relative; }
        .rb-preview-changes li::before { content:''; }

        /* History */
        .rb-history-wrap  { display:flex; flex-direction:column; gap:10px; padding-top:4px; }
        .rb-loading, .rb-empty-history { text-align:center; padding:40px; color:#64748b; }
        .rb-empty-history p { margin:10px 0 0; }
        .rb-history-card  { padding:14px 16px; background:rgba(255,255,255,0.03); border:1px solid rgba(255,255,255,0.07); border-radius:12px; }
        .rb-history-head  { display:flex; justify-content:space-between; align-items:center; margin-bottom:7px; }
        .rb-history-action { font-weight:700; color:var(--warning); font-size:0.88rem; }
        .rb-history-date   { font-size:0.8rem; color:#64748b; }
        .rb-history-meta   { display:flex; gap:16px; font-size:0.82rem; color:#94a3b8; margin-bottom:5px; }
        .rb-history-reason { font-size:0.82rem; color:#cbd5e1; }
        .rb-pagination { display:flex; align-items:center; justify-content:center; gap:12px; margin-top:14px; }
        .rb-pagination button { padding:7px 12px; background:rgba(255,255,255,0.05); border:1px solid rgba(255,255,255,0.08); border-radius:8px; color:#94a3b8; cursor:pointer; }
        .rb-pagination button:disabled { opacity:.35; cursor:not-allowed; }
        .rb-pagination span { font-size:0.88rem; color:#64748b; }

        /* Spinner */
        .rb-spinner { width:15px; height:15px; border:2px solid rgba(255,255,255,0.3); border-top-color:#fff; border-radius:50%; animation:rb-spin .7s linear infinite; display:inline-block; }
        @keyframes rb-spin { to { transform:rotate(360deg); } }

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
