import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, CheckCircle2, ChevronLeft, Clock3, Droplet, Image, XCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import client from '../api/client';
import OperationalBackLink from '../components/OperationalBackLink';
import useCommercialMode from '../hooks/useCommercialMode';

// ── API helpers ───────────────────────────────────────────────────────────────

const fetchPending = () =>
  client.get('/api/operational-review').then((r) => r.data);

const fetchDetail = (id) =>
  client.get(`/api/operational-review/${id}`).then((r) => r.data);

const approveSubmission = (id) =>
  client.post(`/api/operational-review/${id}/approve`).then((r) => r.data);

const approveBundle = ({ submissionId, bundleType, subjectId }) =>
  client.post(`/api/operational-review/${submissionId}/bundles/${bundleType}/${subjectId}/approve`).then((r) => r.data);

const rejectLine = ({ lineType, lineId, reason }) =>
  client
    .post(`/api/operational-review/lines/${lineType}/${lineId}/reject`, { reason })
    .then((r) => r.data);

const rejectBundle = ({ submissionId, bundleType, subjectId, reason }) =>
  client
    .post(`/api/operational-review/${submissionId}/bundles/${bundleType}/${subjectId}/reject`, { reason })
    .then((r) => r.data);

// ── Status chip ───────────────────────────────────────────────────────────────

function StatusChip({ status }) {
  const { t } = useTranslation();
  const map = {
    Pending:      { color: 'var(--warning)', label: t('operational.history.pending') },
    Approved:     { color: 'var(--success)', label: t('operational.balance.import') },
    Rejected:     { color: 'var(--danger)', label: t('movements.status.rejected') },
    Recorded:     { color: 'var(--primary)', label: 'Recorded' },
    AwaitingPhoto:{ color: 'var(--purple)', label: t('operational.balance.notMeasured') },
  };
  const { color, label } = map[status] ?? { color: 'var(--text-muted)', label: status };
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

function RejectModal({ target, onClose, onDone }) {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const [reason, setReason] = useState('');
  const mutation = useMutation({
    mutationFn: () => target.kind === 'bundle'
      ? Promise.all((target.submissionIds || [target.submissionId]).map((submissionId) =>
        rejectBundle({ submissionId, bundleType: target.bundleType, subjectId: target.subjectId, reason })
      ))
      : rejectLine({ lineType: target.lineType, lineId: target.lineId, reason }),
    onSuccess: () => { qc.invalidateQueries(['review']); onDone(); onClose(); },
  });
  return (
    <div className="modal-overlay" style={{ zIndex: 1000 }}>
      <div style={{ background: 'var(--card-bg)', border: '1px solid var(--border)', borderRadius: 'var(--radius)', padding: 24, width: 360, maxWidth: '95vw', boxShadow: 'var(--shadow-modal)' }}>
        <h3 style={{ margin: '0 0 12px' }}>{t('operational.review.rejectLine')}</h3>
        <textarea
          value={reason} onChange={(e) => setReason(e.target.value)}
          placeholder={t('operational.review.rejectReason')}
          rows={3}
          style={{ width: '100%', borderRadius: 'var(--radius-sm)', padding: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)', boxSizing: 'border-box' }}
        />
        <div style={{ display: 'flex', gap: 8, marginTop: 12, justifyContent: 'flex-end' }}>
          <button onClick={onClose} className="btn btn-ghost">
            {t('movements.actions.cancel')}
          </button>
          <button
            onClick={() => mutation.mutate()}
            disabled={!reason.trim() || mutation.isPending}
            className="btn btn-danger"
          >
            {mutation.isPending ? '…' : t('operational.review.rejectLine')}
          </button>
        </div>
        {mutation.isError && <p style={{ color: 'var(--danger)', marginTop: 8 }}>{mutation.error?.response?.data?.error}</p>}
      </div>
    </div>
  );
}

// ── Submission detail view ────────────────────────────────────────────────────

function SubmissionDetail({ ids, reviewGroup, onBack }) {
  const { t, i18n } = useTranslation();
  const qc = useQueryClient();
  const { isCommercialMode, modeResolved } = useCommercialMode();
  const [rejectTarget, setRejectTarget] = useState(null);
  const submissionIds = [...new Set((ids || []).filter((value) => Number.isInteger(value) && value > 0))];
  const id = submissionIds[0];

  const { data: responses, isLoading, error } = useQuery({
    queryKey: ['review', 'detail', ...submissionIds],
    queryFn: () => Promise.all(submissionIds.map(fetchDetail)),
    enabled: submissionIds.length > 0,
  });

  const approveBundleMutation = useMutation({
    mutationFn: ({ bundleType, subjectId, sourceSubmissionIds }) => Promise.all(
      (sourceSubmissionIds?.length ? sourceSubmissionIds : [id])
        .map((submissionId) => approveBundle({ submissionId, bundleType, subjectId }))
    ),
    onSuccess: () => {
      qc.invalidateQueries(['review']);
      qc.invalidateQueries(['review', 'detail']);
    },
  });

  const approveSubmissionMutation = useMutation({
    mutationFn: () => approveSubmission(id),
    onSuccess: () => { qc.invalidateQueries(['review']); onBack(); },
  });

  if (isLoading) return <p style={{ padding: 24 }}>…</p>;
  if (error || !responses?.length) return <p style={{ padding: 24, color: 'var(--danger)' }}>{error?.response?.data?.error || error?.response?.data?.detail || error?.message}</p>;

  // A commercial end-of-day operation can be uploaded in separate requests
  // (generator, tank and feeder). Present them as one business-day review,
  // while keeping each source submission id for its own approval decision.
  const sourceDetails = responses;
  const linesFrom = (property) => sourceDetails.flatMap((detail) =>
    (detail[property] || []).map((line) => ({ ...line, _submissionId: detail.id }))
  );
  const data = {
    ...sourceDetails[0],
    id,
    siteName: reviewGroup?.siteName || sourceDetails[0].siteName,
    receivedAtUtc: reviewGroup?.receivedAtUtc || sourceDetails[0].receivedAtUtc,
    generatorLines: linesFrom('generatorLines'),
    tankLines: linesFrom('tankLines'),
    feederLines: linesFrom('feederLines'),
    meterLines: linesFrom('meterLines'),
    dailyFuelConsumptions: sourceDetails.flatMap((detail) => detail.dailyFuelConsumptions || []),
  };

  const dateStr = (s) => new Date(s).toLocaleString('en-US');
  const isSubject = (line, numeric, name) => line.subjectType === numeric || line.subjectType === name;
  const meterLabel = (line) => {
    if (isSubject(line, 0, 'GeneratorEnergyProduced')) return t('operational.capture.energyCounter');
    if (isSubject(line, 1, 'GeneratorRunningHours')) return t('operational.capture.runningHours');
    if (isSubject(line, 2, 'FeederEnergySent')) return t('operational.commercialCapture.feedersTitle');
    return t('operational.capture.rawValue');
  };
  const latestCaptured = (lines) => [...lines].sort((left, right) =>
    new Date(right.capturedAtUtc || right.receivedAtUtc || 0) - new Date(left.capturedAtUtc || left.receivedAtUtc || 0)
  )[0];
  const counterBySubject = (subjectType, subjectId) => latestCaptured(data.meterLines.filter((line) =>
    isSubject(line, subjectType, subjectType === 0 ? 'GeneratorEnergyProduced' : subjectType === 1 ? 'GeneratorRunningHours' : 'FeederEnergySent') && line.subjectId === subjectId
  ));
  const photoIds = (...lines) => [...new Set(lines.filter(Boolean).flatMap((line) => line.photoAttachmentIds || []))];

  // A daily feeder entry produces both a load line and a counter line.  They are
  // one operational fact, so review them as one card and show their shared photo once.
  const generatorCards = data.generatorLines.map((line) => {
    const energy = counterBySubject(0, line.values?.generatorId);
    const hours = counterBySubject(1, line.values?.generatorId);
    const tank = data.tankLines.find((tankLine) => tankLine.values?.generatorId === line.values?.generatorId);
    return {
      ...line, _type: 'generator', _bundleType: 'generator', _bundleSubjectId: line.values?.generatorId,
      _energy: energy, _hours: hours, _tank: tank, photoAttachmentIds: photoIds(line, energy, hours, tank)
    };
  });
  const feederCards = data.feederLines.map((line) => {
    const counter = counterBySubject(2, line.values?.feederId ?? line.subjectId);
    return {
      ...line, _type: 'feeder', _bundleType: 'feeder', _bundleSubjectId: line.values?.feederId,
      _counter: counter, photoAttachmentIds: photoIds(line, counter)
    };
  });
  const groupedMeterIds = new Set([
    ...generatorCards.flatMap((line) => [line._energy?.id, line._hours?.id]),
    ...feederCards.map((line) => line._counter?.id),
  ].filter(Boolean));
  const groupedTankIds = new Set(generatorCards.map((line) => line._tank?.id).filter(Boolean));
  const allLines = [
    ...generatorCards,
    ...feederCards,
    ...data.tankLines.filter((line) => !groupedTankIds.has(line.id)).map((line) => ({ ...line, _type: 'tank' })),
    ...data.meterLines.filter((line) => !groupedMeterIds.has(line.id)).map((line) => ({ ...line, _type: 'meter' })),
  ];
  const counterSummary = (line) => line && (
    <div style={{ fontSize: 13, color: 'var(--text-muted)', marginTop: 6, display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
      <strong style={{ color: 'var(--text)' }}>{meterLabel(line)}</strong>
      <span>{t('operational.capture.rawValue')}: <strong>{line.rawValue}</strong></span>
      {line.previousApprovedValue != null && <span>{t('operational.review.previousValue')}: <strong>{line.previousApprovedValue}</strong></span>}
      {line.deltaPreview != null && <span>Δ: <strong>{line.deltaPreview.toFixed(2)}</strong></span>}
      <StatusChip status={line.status} />
    </div>
  );
  const lineLabel = (line) => {
    if (line._type === 'generator') return line.values?.generatorName || t('generators.table.name');
    if (line._type === 'feeder') return line.values?.feederName || t('operational.commercialCapture.feedersTitle');
    if (line._type === 'tank') return line.values?.fuelTankName || t('operational.capture.tank');
    return line._type;
  };
  const bundleHasPendingLines = (line) => [line, line._energy, line._hours, line._tank, line._counter]
    .filter(Boolean)
    .some((entry) => entry.status === 'Pending');
  const distinctPositiveIds = (ids) => [...new Set(ids.filter((value) => Number.isInteger(value) && value > 0))];
  const commercialGeneratorRows = distinctPositiveIds([
    ...data.generatorLines.map((line) => line.values?.generatorId),
    ...data.tankLines.map((line) => line.values?.generatorId),
    ...data.meterLines
      .filter((line) => isSubject(line, 0, 'GeneratorEnergyProduced') || isSubject(line, 1, 'GeneratorRunningHours'))
      .map((line) => line.subjectId),
  ]).map((generatorId) => {
    const generatorLines = data.generatorLines.filter((line) => line.values?.generatorId === generatorId);
    const tankLines = data.tankLines
      .filter((line) => line.values?.generatorId === generatorId)
      .sort((left, right) => new Date(left.capturedAtUtc || left.receivedAtUtc || 0) - new Date(right.capturedAtUtc || right.receivedAtUtc || 0));
    const generator = latestCaptured(generatorLines);
    const energy = counterBySubject(0, generatorId);
    const hours = counterBySubject(1, generatorId);
    const tank = tankLines.at(-1);
    const entries = [...generatorLines, energy, hours, ...tankLines].filter(Boolean);
    const submittedOilLitres = generatorLines.reduce((total, line) => total + (Number(line.values?.oilAddedLitres) || 0), 0);
    const oilLine = latestCaptured(generatorLines.filter((line) => line.values?.oilName || line.values?.oilId));
    const firstTank = tankLines[0];
    const consumptionCandidates = (data.dailyFuelConsumptions || []).filter((consumption) => consumption.generatorId === generatorId);
    const preview = consumptionCandidates.find((consumption) =>
      firstTank?.values?.fuelTankId != null && consumption.dailyTankId === firstTank.values.fuelTankId
    ) || consumptionCandidates[0];
    const openingBalanceLitres = firstTank?.values?.beforeComputedLitres ?? preview?.openingBalanceLitres;
    const inboundRefillsLitres = tankLines
      .filter((line) => ['Receipt', 'TransferIn', 1, 3].includes(line.values?.eventType))
      .reduce((total, line) => {
        const before = line.values?.beforeComputedLitres ?? line.values?.computedLitres;
        const change = Number(line.values?.computedLitres) - Number(before);
        return total + (change > 0 ? change : Number(line.values?.deliveryNoteLitres) || 0);
      }, preview?.inboundRefillsLitres || 0);
    const closingBalanceLitres = tank?.values?.computedLitres ?? preview?.closingBalanceLitres;
    const consumptionLitres = openingBalanceLitres != null && closingBalanceLitres != null
      ? Math.max(0, Number(openingBalanceLitres) + inboundRefillsLitres - Number(closingBalanceLitres))
      : preview?.consumptionLitres;
    return {
      id: generator?.id ?? `generator-${generatorId}`,
      status: entries.find((entry) => entry.status === 'Pending')?.status ?? entries[0]?.status ?? 'Pending',
      generatorId,
      generator: generator?.values,
      energy,
      hours,
      tank,
      consumption: preview && {
        ...preview,
        dailyTankId: tank?.values?.fuelTankId ?? preview.dailyTankId,
        dailyTankName: tank?.values?.fuelTankName ?? preview.dailyTankName,
        openingBalanceLitres,
        inboundRefillsLitres,
        closingBalanceLitres,
        consumptionLitres,
      },
      fuel: {
        openingBalanceLitres,
        inboundRefillsLitres,
        closingBalanceLitres,
        consumptionLitres,
      },
      oil: {
        name: oilLine?.values?.oilName,
        litres: submittedOilLitres || null,
      },
      name: generator?.values?.generatorName || energy?.subjectName || hours?.subjectName || t('operational.review.unnamedGenerator'),
      photoAttachmentIds: photoIds(...entries),
      sourceSubmissionIds: distinctPositiveIds(entries.map((entry) => entry._submissionId)),
      hasSkewWarning: entries.some((entry) => entry.hasSkewWarning),
      hasReuseWarning: entries.some((entry) => entry.hasReuseWarning),
      hasPending: entries.some((entry) => entry.status === 'Pending'),
    };
  });
  const commercialFeederRows = distinctPositiveIds([
    ...data.feederLines.map((line) => line.values?.feederId),
    ...data.meterLines.filter((line) => isSubject(line, 2, 'FeederEnergySent')).map((line) => line.subjectId),
  ]).map((feederId) => {
    const feeder = data.feederLines.find((line) => line.values?.feederId === feederId);
    const counter = counterBySubject(2, feederId);
    const entries = [feeder, counter].filter(Boolean);
    return {
      id: feeder?.id ?? `feeder-${feederId}`,
      status: entries.find((entry) => entry.status === 'Pending')?.status ?? entries[0]?.status ?? 'Pending',
      feederId,
      feeder: feeder?.values,
      counter,
      name: feeder?.values?.feederName || counter?.subjectName || t('operational.review.unnamedFeeder'),
      photoAttachmentIds: photoIds(...entries),
      sourceSubmissionIds: distinctPositiveIds(entries.map((entry) => entry._submissionId)),
      hasSkewWarning: entries.some((entry) => entry.hasSkewWarning),
      hasReuseWarning: entries.some((entry) => entry.hasReuseWarning),
      hasPending: entries.some((entry) => entry.status === 'Pending'),
    };
  });
  const displayNumber = (value) => value == null ? '—' : Number(value).toLocaleString('en-US', { maximumFractionDigits: 2 });
  const renderPhotos = (attachmentIds) => attachmentIds?.length ? (
    <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', justifyContent: 'center' }}>
      {attachmentIds.map((photoId) => (
        <a key={photoId} href={`/api/operational-photos/${photoId}`} target="_blank" rel="noreferrer">
          <img
            src={`/api/operational-photos/${photoId}`}
            alt={t('operational.review.submittedPhoto')}
            style={{ width: 54, height: 42, objectFit: 'cover', borderRadius: 5, border: '1px solid var(--border)' }}
            onError={(event) => { event.currentTarget.style.display = 'none'; }}
          />
        </a>
      ))}
    </div>
  ) : <span>—</span>;
  const renderBundleActions = (bundleType, subjectId, sourceSubmissionIds, hasPending) => {
    if (!hasPending) return <span>—</span>;
    return (
      <div style={{ display: 'flex', justifyContent: 'center', gap: 6, minWidth: 180 }}>
        <button
          onClick={() => {
            if (window.confirm(t('operational.review.confirmApproveBundle'))) {
              approveBundleMutation.mutate({ bundleType, subjectId, sourceSubmissionIds });
            }
          }}
          disabled={approveBundleMutation.isPending}
          style={{ padding: '6px 10px', borderRadius: 7, background: 'var(--success)', color: 'var(--on-accent)', border: 'none', cursor: 'pointer', fontWeight: 700, whiteSpace: 'nowrap' }}
        >
          {approveBundleMutation.isPending ? '…' : t(bundleType === 'generator' ? 'operational.review.approveGeneratorBundle' : 'operational.review.approveFeederBundle')}
        </button>
        <button
          onClick={() => setRejectTarget({ kind: 'bundle', submissionIds: sourceSubmissionIds, bundleType, subjectId })}
          disabled={approveBundleMutation.isPending}
          style={{ padding: '6px 10px', borderRadius: 7, background: 'var(--danger-subtle)', color: 'var(--danger)', border: '1px solid var(--danger)', cursor: 'pointer', fontWeight: 700, whiteSpace: 'nowrap' }}
        >
          {t('operational.review.rejectBundle')}
        </button>
      </div>
    );
  };

  return (
    <section className="capture-page">
      <div className="capture-heading">
        <div>
          <button onClick={onBack} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4, marginBottom: 8 }}>
            <ChevronLeft size={16} /> {t('operational.review.back')}
          </button>
          <h1 style={{ margin: 0 }}>
            {data.siteName} {reviewGroup?.businessDate ? `— ${reviewGroup.businessDate}` : `— #${data.id}`}
          </h1>
          <p style={{ color: 'var(--text-muted)', margin: '4px 0 0' }}>
            {t('operational.review.submittedBy')}: <strong>{data.submitterName}</strong> &nbsp;·&nbsp;
            {t('operational.review.receivedAt')}: {dateStr(data.receivedAtUtc)}
          </p>
        </div>
        {modeResolved && !isCommercialMode && (
          <button
            onClick={() => { if (window.confirm(t('operational.review.confirmApprove'))) approveSubmissionMutation.mutate(); }}
            disabled={approveSubmissionMutation.isPending || data.status === 'Approved'}
            style={{ padding: '10px 20px', borderRadius: 'var(--radius-sm)', background: 'var(--success)', color: 'var(--on-accent)', border: 'none', cursor: 'pointer', fontWeight: 700, display: 'flex', alignItems: 'center', gap: 8 }}
          >
            <CheckCircle2 size={18} />
            {approveSubmissionMutation.isPending ? '…' : t('operational.review.approveSubmission')}
          </button>
        )}
      </div>

      {(approveBundleMutation.isError || approveSubmissionMutation.isError) && (
        <div style={{ background: 'var(--danger-subtle)', border: '1px solid var(--danger)', borderRadius: 'var(--radius-sm)', padding: 12, marginBottom: 16, color: 'var(--danger)' }}>
          {approveBundleMutation.error?.response?.data?.error || approveSubmissionMutation.error?.response?.data?.error}
        </div>
      )}

      {modeResolved && isCommercialMode && (
        <>
          <section style={{ background: 'var(--card-bg)', border: '1px solid var(--border)', borderRadius: 'var(--radius-lg)', padding: 16, marginBottom: 16 }}>
            <h3 style={{ margin: '0 0 8px', display: 'flex', alignItems: 'center', gap: 8, fontSize: 16 }}>
              <Droplet size={18} style={{ color: 'var(--warning)' }} />
              {t('operational.review.generatorEndOfDayTitle')}
            </h3>
            <p style={{ fontSize: 12, color: 'var(--text-muted)', margin: '0 0 12px' }}>
              {t('operational.review.generatorEndOfDayNote')}
            </p>
            {commercialGeneratorRows.length === 0 ? (
              <p style={{ color: 'var(--text-muted)', margin: 0 }}>{t('operational.review.noGeneratorEndOfDayData')}</p>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table className="data-table" style={{ fontSize: 12, width: '100%', minWidth: 1600 }}>
                  <thead>
                    <tr>
                      <th>{t('generators.table.name')}</th>
                      <th>{t('operational.capture.tank')}</th>
                      <th>{t('operational.review.tankLevel')}</th>
                      <th>{t('operational.review.openingBalance')}</th>
                      <th>{t('operational.review.refillsIn')}</th>
                      <th>{t('operational.review.closingBalance')}</th>
                      <th>{t('operational.review.consumptionLitres')}</th>
                      <th>{t('operational.review.oilType')}</th>
                      <th>{t('operational.review.oilAdded')}</th>
                      <th>{t('operational.review.energyPrevious')}</th>
                      <th>{t('operational.review.energyCurrent')}</th>
                      <th>{t('operational.review.hoursPrevious')}</th>
                      <th>{t('operational.review.hoursCurrent')}</th>
                      <th>{t('operational.review.hoursDifference')}</th>
                      <th>{t('operational.review.submittedPhoto')}</th>
                      <th>{t('operational.review.actions')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {commercialGeneratorRows.map((row) => (
                      <tr key={row.id}>
                        <td style={{ fontWeight: 800, whiteSpace: 'nowrap' }}>
                          {row.name} <StatusChip status={row.status} />
                          {row.hasSkewWarning && <AlertTriangle size={13} style={{ color: 'var(--warning)', marginInlineStart: 4, verticalAlign: 'middle' }} />}
                        </td>
                        <td>{row.consumption?.dailyTankName || row.tank?.values?.fuelTankName || '—'}</td>
                        <td>{row.tank?.values?.liquidLevelCm != null ? `${displayNumber(row.tank.values.liquidLevelCm)} cm` : '—'}</td>
                        <td>{displayNumber(row.fuel.openingBalanceLitres)}</td>
                        <td style={{ color: 'var(--success)' }}>{row.fuel.inboundRefillsLitres != null ? `+${displayNumber(row.fuel.inboundRefillsLitres)}` : '—'}</td>
                        <td>{displayNumber(row.fuel.closingBalanceLitres)}</td>
                        <td style={{ fontWeight: 800, color: 'var(--primary)' }}>{displayNumber(row.fuel.consumptionLitres)}</td>
                        <td>{row.oil.name || '—'}</td>
                        <td>{row.oil.litres != null ? `${displayNumber(row.oil.litres)} L` : '—'}</td>
                        <td>{displayNumber(row.energy?.previousApprovedValue)}</td>
                        <td style={{ fontWeight: 700 }}>{displayNumber(row.energy?.rawValue)}</td>
                        <td>{displayNumber(row.hours?.previousApprovedValue)}</td>
                        <td style={{ fontWeight: 700 }}>{displayNumber(row.hours?.rawValue)}</td>
                        <td style={{ fontWeight: 800 }}>{displayNumber(row.hours?.deltaPreview)}</td>
                        <td>{renderPhotos(row.photoAttachmentIds)}</td>
                        <td>{renderBundleActions('generator', row.generatorId, row.sourceSubmissionIds, row.hasPending)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          <section style={{ background: 'var(--card-bg)', border: '1px solid var(--border)', borderRadius: 'var(--radius-lg)', padding: 16, marginBottom: 16 }}>
            <h3 style={{ margin: '0 0 8px', display: 'flex', alignItems: 'center', gap: 8, fontSize: 16 }}>
              <Clock3 size={18} style={{ color: 'var(--primary)' }} />
              {t('operational.review.feederEndOfDayTitle')}
            </h3>
            {commercialFeederRows.length === 0 ? (
              <p style={{ color: 'var(--text-muted)', margin: 0 }}>{t('operational.review.noFeederEndOfDayData')}</p>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table className="data-table" style={{ fontSize: 12, width: '100%', minWidth: 650 }}>
                  <thead>
                    <tr>
                      <th>{t('operational.review.feeder')}</th>
                      <th>{t('operational.review.energyPrevious')}</th>
                      <th>{t('operational.review.energyCurrent')}</th>
                      <th>{t('operational.review.activeLoad')}</th>
                      <th>{t('operational.review.submittedPhoto')}</th>
                      <th>{t('operational.review.actions')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {commercialFeederRows.map((row) => (
                      <tr key={row.id}>
                        <td style={{ fontWeight: 800, whiteSpace: 'nowrap' }}>
                          {row.name} <StatusChip status={row.status} />
                          {row.hasSkewWarning && <AlertTriangle size={13} style={{ color: 'var(--warning)', marginInlineStart: 4, verticalAlign: 'middle' }} />}
                        </td>
                        <td>{displayNumber(row.counter?.previousApprovedValue)}</td>
                        <td style={{ fontWeight: 700 }}>{displayNumber(row.counter?.rawValue)}</td>
                        <td style={{ fontWeight: 800 }}>{displayNumber(row.counter?.deltaPreview)}</td>
                        <td>{renderPhotos(row.photoAttachmentIds)}</td>
                        <td>{renderBundleActions('feeder', row.feederId, row.sourceSubmissionIds, row.hasPending)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}

      {!isCommercialMode && data.status === 'Approved' && data.dailyFuelConsumptions?.length > 0 && (
        <div style={{
          background: 'var(--card-bg)', border: '1px solid var(--border)',
          borderRadius: 'var(--radius-lg)', padding: 16, marginBottom: 16
        }}>
          <h3 style={{ margin: '0 0 8px', display: 'flex', alignItems: 'center', gap: 8, fontSize: 16 }}>
            <Droplet size={18} style={{ color: 'var(--warning)' }} />
            {t('operational.review.dailyFuelConsumptionTitle', 'استهلاك الوقود اليومي للمولدات')}
          </h3>
          <p style={{ fontSize: 12, color: 'var(--text-muted)', margin: '0 0 12px' }}>
            {t('operational.review.fuelFormulaNote', 'معادلة الاستهلاك = رصيد البداية + التعبئة/التحويلات − رصيد نهاية اليوم')}
          </p>
          <div style={{ overflowX: 'auto' }}>
            <table className="data-table" style={{ fontSize: 13, width: '100%' }}>
              <thead>
                <tr>
                  <th>{t('generators.table.name', 'المولد')}</th>
                  <th>{t('operational.capture.tank', 'الخزان اليومي')}</th>
                  <th style={{ textAlign: 'center' }}>{t('operational.review.openingBalance', 'رصيد البداية (لتر)')}</th>
                  <th style={{ textAlign: 'center' }}>{t('operational.review.refillsIn', 'التعبئة / التحويل (لتر)')}</th>
                  <th style={{ textAlign: 'center' }}>{t('operational.review.closingBalance', 'رصيد الإقفال (لتر)')}</th>
                  <th style={{ textAlign: 'center', fontWeight: 800, color: 'var(--primary)' }}>
                    {t('operational.review.consumptionLitres', 'الاستهلاك اليومي (لتر)')}
                  </th>
                  <th style={{ textAlign: 'center' }}>{t('operational.review.producedKwh', 'الطاقة المنتجة (KWh)')}</th>
                  <th style={{ textAlign: 'center' }}>{t('operational.review.kwhPerLiter', 'كفاءة (KWh/لتر)')}</th>
                </tr>
              </thead>
              <tbody>
                {data.dailyFuelConsumptions.map((c) => (
                  <tr key={c.generatorId}>
                    <td style={{ fontWeight: 700 }}>{c.generatorName}</td>
                    <td>{c.dailyTankName || '—'}</td>
                    <td style={{ textAlign: 'center' }}>{c.openingBalanceLitres.toLocaleString()}</td>
                    <td style={{ textAlign: 'center', color: 'var(--success)' }}>+{c.inboundRefillsLitres.toLocaleString()}</td>
                    <td style={{ textAlign: 'center' }}>{c.closingBalanceLitres.toLocaleString()}</td>
                    <td style={{ textAlign: 'center', fontWeight: 800, color: 'var(--primary)', fontSize: 14 }}>
                      {c.consumptionLitres.toLocaleString()} L
                    </td>
                    <td style={{ textAlign: 'center' }}>{c.producedKwh != null ? c.producedKwh.toLocaleString() : '—'}</td>
                    <td style={{ textAlign: 'center' }}>{c.kwhPerLiter != null ? c.kwhPerLiter : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {data.dailyFuelConsumptions.some((c) => c.warnings?.length > 0) && (
            <div style={{ marginTop: 10, display: 'flex', flexDirection: 'column', gap: 4 }}>
              {data.dailyFuelConsumptions.flatMap((c) => c.warnings || []).map((w, idx) => (
                <div key={idx} style={{ color: 'var(--warning)', fontSize: 12, display: 'flex', alignItems: 'center', gap: 6 }}>
                  <AlertTriangle size={14} /> {t(w, w)}
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {!isCommercialMode && <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        {allLines.map((line) => (
          <article key={`${line._type}-${line.id}`} style={{
            background: 'var(--card-bg)', borderRadius: 'var(--radius-lg)', padding: 16,
            border: `1px solid ${line.status === 'Rejected' ? 'var(--danger)' : 'var(--border)'}`
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8, flexWrap: 'wrap' }}>
              <StatusChip status={line.status} />
              <span style={{ fontSize: 13, color: 'var(--text-muted)' }}>
                {line._type === 'meter'
                  ? (line.subjectType === 3 || line.subjectType === 'SolarEnergyProduced'
                      ? (document.documentElement.lang === 'ar' ? '☀️ إنتاج طاقة شمسية' : '☀️ Solar Production')
                      : (line.subjectType === 0 || line.subjectType === 'GeneratorEnergyProduced'
                          ? (document.documentElement.lang === 'ar' ? '⚡ طاقة مولد' : '⚡ Generator Energy')
                          : (line.subjectType === 1 || line.subjectType === 'GeneratorRunningHours'
                              ? (document.documentElement.lang === 'ar' ? '⏱️ ساعات تشغيل' : '⏱️ Running Hours')
                              : (line.subjectType === 2 || line.subjectType === 'FeederEnergySent'
                                  ? (document.documentElement.lang === 'ar' ? '📡 فيدر' : '📡 Feeder')
                                  : 'meter'))))
                  : lineLabel(line)} · {dateStr(line.capturedAtUtc)}
                {line._type === 'meter' && line.subjectName ? ` — ${line.subjectName}` : ''}
              </span>
              {line.hasSkewWarning && (
                <span style={{ color: 'var(--warning)', fontSize: 12, display: 'flex', alignItems: 'center', gap: 4 }}>
                  <AlertTriangle size={14} /> {t('operational.review.skewWarning')}
                </span>
              )}
              {line.hasReuseWarning && (
                <span style={{ color: 'var(--purple)', fontSize: 12, display: 'flex', alignItems: 'center', gap: 4 }}>
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
                      style={{ width: 80, height: 60, objectFit: 'cover', borderRadius: 6, border: '1px solid var(--border)' }}
                      onError={(e) => { e.target.style.display = 'none'; }}
                    />
                  </a>
                ))}
              </div>
            )}

            {/* Tank level reading (cm & stored computed litres) */}
            {line._type === 'generator' && line.values && (
              <div style={{ fontSize: 13, color: 'var(--text-muted)', marginBottom: 6, display: 'flex', gap: 10, flexWrap: 'wrap' }}>
                <strong style={{ color: 'var(--text)' }}>{line.values.generatorName}</strong>
                <span>{line.values.isRunning ? '●' : '○'} {line.values.isRunning ? t('operational.capture.running') : '—'}</span>
                {line.values.lineVoltageVolts != null && <span>{t('operational.capture.voltage')}: <strong>{line.values.lineVoltageVolts}</strong></span>}
                {line.values.currentAmps != null && <span>{t('operational.capture.current')}: <strong>{line.values.currentAmps}</strong></span>}
                {line.values.oilPressure != null && <span>{t('operational.capture.oilPressure')}: <strong>{line.values.oilPressure}</strong></span>}
                {line.values.oilTemperatureCelsius != null && <span>{t('operational.capture.oilTemp')}: <strong>{line.values.oilTemperatureCelsius}</strong></span>}
                {line.values.waterTemperatureCelsius != null && <span>{t('operational.capture.waterTemp')}: <strong>{line.values.waterTemperatureCelsius}</strong></span>}
                {line.values.activeLoadKw != null && <span>{t('operational.capture.activeKw')}: <strong>{line.values.activeLoadKw}</strong></span>}
                {line.values.oilAddedLitres > 0 && <span>{t('operational.commercialCapture.oilTopUpLitres')}: <strong>{line.values.oilAddedLitres} L</strong>{line.values.oilName ? ` — ${line.values.oilName}` : ''}</span>}
              </div>
            )}
            {line._type === 'generator' && <>
              {counterSummary(line._energy)}
              {counterSummary(line._hours)}
              {line._tank && (
                <div style={{ fontSize: 13, color: 'var(--text-muted)', marginTop: 6, display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
                  <strong style={{ color: 'var(--text)' }}>{line._tank.values?.fuelTankName}</strong>
                  {line._tank.values?.beforeLiquidLevelCm != null && <span>{Number(line._tank.values.beforeLiquidLevelCm).toLocaleString('en-US')} cm →</span>}
                  <strong>{Number(line._tank.values?.liquidLevelCm).toLocaleString('en-US')} cm</strong>
                  {line._tank.values?.computedLitres != null && <span>· <strong>{Number(line._tank.values.computedLitres).toLocaleString('en-US')} L</strong></span>}
                  <StatusChip status={line._tank.status} />
                </div>
              )}
            </>}
            {line._type === 'tank' && line.values && (
              <div style={{ fontSize: 13, color: 'var(--text-muted)', marginBottom: 6 }}>
                {line.values.fuelTankName && <strong>{line.values.fuelTankName}: </strong>}
                {line.values.beforeLiquidLevelCm != null && (
                  <>
                    <span>{Number(line.values.beforeLiquidLevelCm).toLocaleString('en-US')} cm</span>
                    {line.values.beforeComputedLitres != null && <> · <span>{Number(line.values.beforeComputedLitres).toLocaleString('en-US')} L</span></>}
                    &nbsp;→&nbsp;
                  </>
                )}
                <strong>{Number(line.values.liquidLevelCm).toLocaleString('en-US')} cm</strong>
                {line.values.computedLitres != null && (
                  <> · <strong>{Number(line.values.computedLitres).toLocaleString('en-US')} L</strong></>
                )}
              </div>
            )}

            {line._type === 'feeder' && line.values && (
              <div style={{ fontSize: 13, color: 'var(--text-muted)', marginBottom: 6 }}>
                <strong style={{ color: 'var(--text)' }}>{line.values.feederName}</strong>
                {line.values.activeLoadKw != null && <> · {t('operational.capture.activeKw')}: <strong>{line.values.activeLoadKw}</strong></>}
                {line.values.apparentLoadKva != null && <> · {t('operational.capture.apparentKva')}: <strong>{line.values.apparentLoadKva}</strong></>}
              </div>
            )}
            {line._type === 'feeder' && counterSummary(line._counter)}

            {/* Always show the submitted counter; previous/variance are only available after an official baseline exists. */}
            {line._type === 'meter' && (
              <div style={{ fontSize: 13, color: 'var(--text-muted)', marginBottom: 6 }}>
                {t('operational.capture.rawValue')}: <strong>{line.rawValue}</strong>
                {line.multiplierSnapshot != null && <> · {t('operational.capture.multiplier')}: <strong>{line.multiplierSnapshot}</strong></>}
                {line.previousApprovedValue != null && <>
                  &nbsp;·&nbsp;{t('operational.review.previousValue')}: <strong>{line.previousApprovedValue}</strong>
                </>}
                {line.deltaPreview != null && <>
                  &nbsp;·&nbsp;Δ = <strong>{line.deltaPreview.toFixed(2)}</strong>
                  {line.deltaSpanDays > 1 && <span style={{ color: 'var(--warning)' }}> ({line.deltaSpanDays} days)</span>}
                </>}
              </div>
            )}

            {/* Rejection reason */}
            {line.rejectionReason && (
              <p style={{ color: 'var(--danger)', fontSize: 13, margin: '4px 0' }}>
                <XCircle size={14} style={{ verticalAlign: 'middle' }} /> {line.rejectionReason}
              </p>
            )}

            {modeResolved && isCommercialMode && line._bundleType && line._bundleSubjectId && bundleHasPendingLines(line) && (
              <div style={{ display: 'flex', gap: 8, marginTop: 10, flexWrap: 'wrap' }}>
                <button
                  onClick={() => {
                    if (window.confirm(t('operational.review.confirmApproveBundle'))) {
                      approveBundleMutation.mutate({ bundleType: line._bundleType, subjectId: line._bundleSubjectId });
                    }
                  }}
                  disabled={approveBundleMutation.isPending}
                  style={{ padding: '7px 13px', borderRadius: 8, background: 'var(--success)', color: 'var(--on-accent)', border: 'none', cursor: 'pointer', fontWeight: 700, display: 'flex', alignItems: 'center', gap: 6 }}
                >
                  <CheckCircle2 size={16} />
                  {approveBundleMutation.isPending ? '…' : t(
                    line._bundleType === 'generator'
                      ? 'operational.review.approveGeneratorBundle'
                      : 'operational.review.approveFeederBundle'
                  )}
                </button>
                <button
                  onClick={() => setRejectTarget({
                    kind: 'bundle', submissionId: id, bundleType: line._bundleType, subjectId: line._bundleSubjectId
                  })}
                  disabled={approveBundleMutation.isPending}
                  style={{ padding: '7px 13px', borderRadius: 8, background: 'var(--danger-subtle)', color: 'var(--danger)', border: '1px solid var(--danger)', cursor: 'pointer', fontWeight: 700 }}
                >
                  {t('operational.review.rejectBundle')}
                </button>
              </div>
            )}

            {/* Reject button */}
            {line.status === 'Pending' && line._type !== 'generator' && line._type !== 'feeder' && (
              <button
                onClick={() => setRejectTarget({ lineType: line._type, lineId: line.id })}
                style={{ marginTop: 8, padding: '6px 14px', borderRadius: 8, background: 'var(--danger-subtle)', color: 'var(--danger)', border: '1px solid var(--danger)', cursor: 'pointer', fontSize: 13 }}
              >
                {t('operational.review.rejectLine')}
              </button>
            )}
          </article>
        ))}
      </div>}

      {rejectTarget && (
        <RejectModal
          target={rejectTarget}
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
  const { isCommercialMode, modeResolved } = useCommercialMode();
  const [selectedGroup, setSelectedGroup] = useState(null);

  const { data = [], isLoading, error } = useQuery({
    queryKey: ['review', 'queue'],
    queryFn: fetchPending,
  });

  if (selectedGroup) {
    return <SubmissionDetail
      ids={selectedGroup.ids}
      reviewGroup={selectedGroup}
      onBack={() => setSelectedGroup(null)}
    />;
  }

  const dateStr = (s) => new Date(s).toLocaleString(i18n.language === 'ar' ? 'ar-SA-u-nu-latn' : 'en-GB');
  const reviewItems = modeResolved && isCommercialMode
    ? Object.values(data.reduce((groups, submission) => {
      const businessDate = submission.businessDate || new Date(submission.receivedAtUtc).toLocaleDateString('en-CA');
      const key = `${submission.subStationId}:${businessDate}`;
      const group = groups[key] || {
        ...submission,
        businessDate,
        ids: [],
        generatorLineCount: 0,
        tankLineCount: 0,
        feederLineCount: 0,
        meterLineCount: 0,
        hasSkewWarning: false,
      };
      group.ids.push(submission.id);
      group.generatorLineCount += submission.generatorLineCount;
      group.tankLineCount += submission.tankLineCount;
      group.feederLineCount += submission.feederLineCount;
      group.meterLineCount += submission.meterLineCount;
      group.hasSkewWarning ||= submission.hasSkewWarning;
      if (new Date(submission.receivedAtUtc) > new Date(group.receivedAtUtc)) {
        group.receivedAtUtc = submission.receivedAtUtc;
        group.submitterName = submission.submitterName;
      }
      groups[key] = group;
      return groups;
    }, {}))
    : data.map((submission) => ({ ...submission, ids: [submission.id] }));

  return (
    <section className="capture-page">
      <OperationalBackLink />
      <div className="capture-heading">
        <div>
          <span className="operational-eyebrow">
            <Clock3 size={16} /> {t('operational.review.title')}
          </span>
          <h1>{t('operational.review.queue')}</h1>
        </div>
      </div>

      {isLoading && <p style={{ padding: 24 }}>…</p>}
      {error && <p style={{ color: 'var(--danger)', padding: 24 }}>{error.message}</p>}
       {!isLoading && reviewItems.length === 0 && (
        <p style={{ color: 'var(--text-muted)', padding: 24, textAlign: 'center' }}>
          {t('operational.review.empty')}
        </p>
      )}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
         {reviewItems.map((sub) => (
           <article
             key={`${sub.subStationId}-${sub.businessDate || sub.id}`}
             onClick={() => setSelectedGroup(sub)}
            style={{
              background: 'var(--card-bg)', borderRadius: 'var(--radius-lg)', padding: 16,
              border: '1px solid var(--border)', cursor: 'pointer',
              display: 'flex', alignItems: 'center', gap: 16
            }}
          >
            <div style={{ flex: 1 }}>
              <strong>{sub.siteName}</strong>
              <span style={{ marginInlineStart: 10, fontSize: 13, color: 'var(--text-muted)' }}>
                {t('operational.review.submittedBy')}: {sub.submitterName}
              </span>
              <div style={{ fontSize: 13, color: 'var(--text-muted)', marginTop: 2 }}>
                 <Clock3 size={13} style={{ verticalAlign: 'middle' }} /> {sub.businessDate || dateStr(sub.receivedAtUtc)}
                &nbsp;·&nbsp;
                {sub.generatorLineCount + sub.tankLineCount + sub.feederLineCount + sub.meterLineCount} {t('operational.review.linesCount')}
              </div>
            </div>
            {sub.hasSkewWarning && <AlertTriangle size={18} color="var(--warning)" />}
            <ChevronLeft size={18} color="var(--text-muted)" style={{ transform: i18n.dir() === 'rtl' ? 'scaleX(-1)' : undefined }} />
          </article>
        ))}
      </div>
    </section>
  );
}
