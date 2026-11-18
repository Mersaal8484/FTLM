const supplyStates = { 'مسودة': 'draft', 'مكتمل': 'completed', 'ملغى': 'cancelled' };
const dispatchStates = { 'قيد الانتظار': 'pending', 'معتمد': 'approved', 'موافقة جزئية': 'partial', 'مرفوض': 'rejected', 'تم الصرف': 'executed', 'مكتمل': 'executed', 'ملغى': 'cancelled' };

export const supplyStatus = value => supplyStates[value] || String(value || '').trim().toLowerCase();
export const dispatchStatus = value => dispatchStates[value] || String(value || '').trim().toLowerCase();
