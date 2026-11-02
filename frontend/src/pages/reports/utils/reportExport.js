import * as XLSX from 'xlsx-js-style';

export const exportToExcel = (data, columns, filename = 'report') => {
  if (!data || data.length === 0) return;

  const headers = columns.map(col => col.header);
  const rows = data.map(row => {
    return columns.map(col => {
      // Basic rendering, skip complex render functions
      return row[col.accessor] || '';
    });
  });

  const wsData = [headers, ...rows];
  const ws = XLSX.utils.aoa_to_sheet(wsData);

  // Styling header
  const range = XLSX.utils.decode_range(ws['!ref']);
  for (let C = range.s.c; C <= range.e.c; ++C) {
    const address = XLSX.utils.encode_cell({ c: C, r: 0 });
    if (!ws[address]) continue;
    ws[address].s = {
      font: { bold: true, color: { rgb: "FFFFFF" } },
      fill: { fgColor: { rgb: "3b82f6" } },
      alignment: { horizontal: "center" }
    };
  }

  // Right-to-Left for Arabic
  ws['!dir'] = 'rtl';

  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, "Report");
  
  XLSX.writeFile(wb, `${filename}_${new Date().toISOString().split('T')[0]}.xlsx`);
};
