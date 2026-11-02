export const getMovementId = (movement) => movement?.id;

export const getApiErrorMessage = (err) => (
  err?.response?.data?.msg
  || err?.response?.data?.errors?.[0]?.msg
  || err?.response?.data?.title
  || 'حدث خطأ'
);

export const getMovementCategory = (m) => {
  const type = (m?.transactionTypeLookupName || '').toLowerCase();
  if (type.includes('توريد') || type.includes('supply') || type.includes('وارد')) return 'supply';
  if (type.includes('نقل') || type.includes('transfer')) return 'transfer';
  if (type.includes('فاقد') || type.includes('loss') || type.includes('تالف')) return 'loss';
  if (type.includes('استهلاك') || type.includes('consumption') ||
      type.includes('تسوية') || type.includes('adjustment')) return 'consumption';
  return 'other';
};