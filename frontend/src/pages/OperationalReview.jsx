import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, CheckCircle2, ChevronLeft, Clock3, Image, XCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import client from '../api/client';

// ── API helpers ───────────────────────────────────────────────────────────────

const fetchPending = () =>
  client.get('/api/operational-review').then((r) => r.data);

const fetchDetail = (id) =>
  client.get(`/api/operational-review/${id}`).then((r) => r.data);

const approveSubmission = (id) =>
  client.post(`/api/operational-review/${id}/approve`).then((r) => r.data);

const rejectLine = ({ lineType, lineId, reason }) =>
  client
    .post(`/api/operational-review/lines/${lineType}/${lineId}/reject`, { reason })
    .then((r) => r.data);

// ── Status chip ───────────────────────────────────────────────────────────────

function StatusChip({ status }) {
  const { t } = useTranslation();
  const map = {
    Pending:      { color: '#f59e0b', label: t('operational.history.pending') },
    Approved:     { color: '#22c55e', label: t('operational.balance.import') },
    Rejected:     { color: '#ef4444', label: t('movements.status.rejected') },
    Recorded:     { color: '#3b82f6', label: 'Recorded' },
    AwaitingPhoto:{ color: '#a855f7', label: t('operational.balance.notMeasured') },
  };
  const { color, label } = map[status] ?? { color: '#6b7280', label: status };
  return (
    <span style={{
      background: `${color}22`, color, border: `1px solid ${color}`,
      borderRadius: 6, padding: '2px 8px', fontSize: 12, fontWeight: 600
    }}>
      {label}
    </span>
  );
}

// ── Reject modal ──────────────────────────────────────────────────────────────

function RejectModal({ lineType, lineId, onClose, onDone }) {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const [reason, setReason] = useState('');
  const mutation = useMutation({
    mutationFn: () => rejectLine({ lineType, lineId, reason }),
    onSuccess: () => { qc.invalidateQueries(['review']); onDone(); onClose(); },
  });
  return (
    <div style={{ position: 'fixed', inset: 0, background: '#0008', zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <div style={{ background: 'var(--card-bg, #1e293b)', borderRadius: 12, padding: 24, width: 360, maxWidth: '95vw' }}>
        <h3 style={{ margin: '0 0 12px' }}>{t('operational.review.rejectLine')}</h3>
        <textarea
          value={reason} onChange={(e) => setReason(e.target.value)}
          placeholder={t('operational.review.rejectReason')}
          rows={3}
          style={{ width: '100%', borderRadius: 8, padding: 8, background: '#0f172a', color: '#fff', border: '1px solid #334155', boxSizing: 'border-box' }}
        />
        <div style={{ display: 'flex', gap: 8, marginTop: 12, justifyContent: 'flex-end' }}>
          <button onClick={onClose} style={{ padding: '8px 16px', borderRadius: 8, border: '1px solid #334155', background: 'transparent', color: '#94a3b8', cursor: 'pointer' }}>
            {t('movements.actions.cancel')}
          </button>
          <button
            onClick={() => mutation.mutate()}
            disabled={!reason.trim() || mutation.isPending}
            style={{ padding: '8px 16px', borderRadius: 8, background: '#ef4444', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 600 }}
          >
            {mutation.isPending ? '…' : t('operational.review.rejectLine')}
          </button>
        </div>
        {mutation.isError && <p style={{ color: '#ef4444', marginTop: 8 }}>{mutation.error?.response?.data?.error}</p>}
      </div>
    </div>
  );
}

// ── Submission detail view ────────────────────────────────────────────────────

function SubmissionDetail({ id, onBack }) {
  const { t, i18n } = useTranslation();
  const qc = useQueryClient();
  const [rejectTarget, setRejectTarget] = useState(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ['review', 'detail', id],
    queryFn: () => fetchDetail(id),
  });

  const approveMutation = useMutation({
    mutationFn: () => approveSubmission(id),
    onSuccess: () => { qc.invalidateQueries(['review']); onBack(); },
  });

  if (isLoading) return <p style={{ padding: 24 }}>…</p>;
  if (error || !data) return <p style={{ padding: 24, color: '#ef4444' }}>{error?.message}</p>;

  const allLines = [
    ...data.generatorLines.map((l) => ({ ...l, _type: 'generator' })),
    ...data.tankLines.map((l) => ({ ...l, _type: 'tank' })),
    ...data.feederLines.map((l) => ({ ...l, _type: 'feeder' })),
    ...data.meterLines.map((l) => ({ ...l, _type: 'meter' })),
  ];

  const dateStr = (s) => new Date(s).toLocaleString(i18n.language === 'ar' ? 'ar-SA' : 'en-GB');

  return (
    <section className="capture-page">
      <div className="capture-heading">
        <div>
          <button onClick={onBack} style={{ background: 'none', border: 'none', color: '#94a3b8', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4, marginBottom: 8 }}>
            <ChevronLeft size={16} /> {t('operational.review.back')}
          </button>
          <h1 style={{ margin: 0 }}>{data.siteName} — #{data.id}</h1>
          <p style={{ color: '#94a3b8', margin: '4px 0 0' }}>
            {t('operational.review.submittedBy')}: <strong>{data.submitterName}</strong> &nbsp;·&nbsp;
            {t('operational.review.receivedAt')}: {dateStr(data.receivedAtUtc)}
          </p>
        </div>
        <button
          onClick={() => { if (window.confirm(t('operational.review.confirmApprove'))) approveMutation.mutate(); }}
          disabled={approveMutation.isPending || data.status === 'Approved'}
          style={{ padding: '10px 20px', borderRadius: 10, background: '#22c55e', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 700, display: 'flex', alignItems: 'center', gap: 8 }}
        >
          <CheckCircle2 size={18} />
          {approveMutation.isPending ? '…' : t('operational.review.approveSubmission')}
        </button>
      </div>

      {approveMutation.isError && (
        <div style={{ background: '#7f1d1d44', border: '1px solid #ef4444', borderRadius: 8, padding: 12, marginBottom: 16, color: '#fca5a5' }}>
          {approveMutation.error?.response?.data?.error}
        </div>
      )}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        {allLines.map((line) => (
          <article key={`${line._type}-${line.id}`} style={{
            background: 'var(--card-bg, #1e293b)', borderRadius: 12, padding: 16,
            border: `1px solid ${line.status === 'Rejected' ? '#ef444466' : '#334155'}`
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8, flexWrap: 'wrap' }}>
              <StatusChip status={line.status} />
              <span style={{ fontSize: 13, color: '#94a3b8' }}>{line._type} · {dateStr(line.capturedAtUtc)}</span>
              {line.hasSkewWarning && (
                <span style={{ color: '#f59e0b', fontSize: 12, display: 'flex', alignItems: 'center', gap: 4 }}>
                  <AlertTriangle size={14} /> {t('operational.review.skewWarning')}
                </span>
              )}
              {line.hasReuseWarning && (
                <span style={{ color: '#a855f7', fontSize: 12, display: 'flex', alignItems: 'center', gap: 4 }}>
                  <Image size={14} /> {t('operational.review.reuseWarning')}
                </span>
              )}
            </div>

            {/* Photos */}
            {(line.photoAttachmentIds?.length > 0) && (
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 8 }}>
                {line.photoAttachmentIds.map((pid) => (
                  <a key={pid} href={`/api/operational-photos/${pid}`} target="_blank" rel="noreferrer">
                    <img
                      src={`/api/operational-photos/${pid}`}
                      alt=""
                      style={{ width: 80, height: 60, objectFit: 'cover', borderRadius: 6, border: '1px solid #334155' }}
                      onError={(e) => { e.target.style.display = 'none'; }}
                    />
                  </a>
                ))}
              </div>
            )}

            {/* Meter delta */}
            {line._type === 'meter' && line.deltaPreview != null && (
              <div style={{ fontSize: 13, color: '#94a3b8', marginBottom: 6 }}>
                {t('operational.review.previousValue')}: <strong>{line.previousApprovedValue ?? '—'}</strong>
                &nbsp;→&nbsp;
                <strong>{line.rawValue}</strong>
                &nbsp;·&nbsp; Δ = <strong>{line.deltaPreview?.toFixed(2)}</strong>
                {line.deltaSpanDays > 1 && <span style={{ color: '#f59e0b' }}> ({line.deltaSpanDays} days)</span>}
              </div>
            )}

            {/* Rejection reason */}
            {line.rejectionReason && (
              <p style={{ color: '#fca5a5', fontSize: 13, margin: '4px 0' }}>
                <XCircle size={14} style={{ verticalAlign: 'middle' }} /> {line.rejectionReason}
              </p>
            )}

            {/* Reject button */}
            {line.status === 'Pending' && (
              <button
                onClick={() => setRejectTarget({ lineType: line._type, lineId: line.id })}
                style={{ marginTop: 8, padding: '6px 14px', borderRadius: 8, background: '#7f1d1d', color: '#fca5a5', border: 'none', cursor: 'pointer', fontSize: 13 }}
              >
                {t('operational.review.rejectLine')}
              </button>
            )}
          </article>
        ))}
      </div>

      {rejectTarget && (
        <RejectModal
          {...rejectTarget}
          onClose={() => setRejectTarget(null)}
          onDone={() => {}}
        />
      )}
    </section>
  );
}

// ── Main review queue page ────────────────────────────────────────────────────

export default function OperationalReview() {
  const { t, i18n } = useTranslation();
  const [selectedId, setSelectedId] = useState(null);

  const { data = [], isLoading, error } = useQuery({
    queryKey: ['review', 'queue'],
    queryFn: fetchPending,
  });

  if (selectedId) {
    return <SubmissionDetail id={selectedId} onBack={() => setSelectedId(null)} />;
  }

  const dateStr = (s) => new Date(s).toLocaleString(i18n.language === 'ar' ? 'ar-SA' : 'en-GB');

  return (
    <section className="capture-page">
      <div className="capture-heading">
        <div>
          <span className="operational-eyebrow">
            <Clock3 size={16} /> {t('operational.review.title')}
          </span>
          <h1>{t('operational.review.queue')}</h1>
        </div>
      </div>

      {isLoading && <p style={{ padding: 24 }}>…</p>}
      {error && <p style={{ color: '#ef4444', padding: 24 }}>{error.message}</p>}
      {!isLoading && data.length === 0 && (
        <p style={{ color: '#94a3b8', padding: 24, textAlign: 'center' }}>
          {t('operational.review.empty')}
        </p>
      )}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {data.map((sub) => (
          <article
            key={sub.id}
            onClick={() => setSelectedId(sub.id)}
            style={{
              background: 'var(--card-bg, #1e293b)', borderRadius: 12, padding: 16,
              border: '1px solid #334155', cursor: 'pointer',
              display: 'flex', alignItems: 'center', gap: 16
            }}
          >
            <div style={{ flex: 1 }}>
              <strong>{sub.siteName}</strong>
              <span style={{ marginInlineStart: 10, fontSize: 13, color: '#94a3b8' }}>
                {t('operational.review.submittedBy')}: {sub.submitterName}
              </span>
              <div style={{ fontSize: 13, color: '#94a3b8', marginTop: 2 }}>
                <Clock3 size={13} style={{ verticalAlign: 'middle' }} /> {dateStr(sub.receivedAtUtc)}
                &nbsp;·&nbsp;
                {sub.generatorLineCount + sub.tankLineCount + sub.feederLineCount + sub.meterLineCount} {t('operational.review.linesCount')}
              </div>
            </div>
            {sub.hasSkewWarning && <AlertTriangle size={18} color="#f59e0b" />}
            <ChevronLeft size={18} color="#94a3b8" style={{ transform: i18n.dir() === 'rtl' ? 'scaleX(-1)' : undefined }} />
          </article>
        ))}
      </div>
    </section>
  );
}
