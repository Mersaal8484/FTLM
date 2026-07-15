import jsPDF from 'jspdf';
import 'jspdf-autotable';

export const exportToPdf = (headers, rows, title, fileName) => {
  const doc = new jsPDF();

  doc.setFontSize(16);
  doc.text(title, 14, 22);

  doc.setFontSize(10);
  doc.text(`Generated: ${new Date().toLocaleDateString()}`, 14, 30);

  doc.autoTable({
    head: [headers],
    body: rows,
    startY: 35,
    styles: {
      fontSize: 8,
      cellPadding: 3,
    },
    headStyles: {
      fillColor: [59, 130, 246],
      textColor: 255,
      fontStyle: 'bold',
    },
    alternateRowStyles: {
      fillColor: [245, 245, 245],
    },
  });

  doc.save(fileName);
};

export const exportPeriodicToPdf = (sheets, fileName) => {
  if (!sheets || sheets.length === 0) return;
  const doc = new jsPDF('l', 'mm', 'a4'); // landscape for periodic tables

  sheets.forEach((sheet, idx) => {
    if (idx > 0) doc.addPage();

    doc.setFontSize(14);
    doc.text(sheet.title, 14, 15);

    doc.setFontSize(9);
    doc.text(`Generated: ${new Date().toLocaleDateString()}`, 14, 22);

    doc.autoTable({
      head: [sheet.headers],
      body: sheet.rows,
      startY: 27,
      styles: {
        fontSize: 7,
        cellPadding: 2,
      },
      headStyles: {
        fillColor: [16, 185, 129],
        textColor: 255,
        fontStyle: 'bold',
      },
      alternateRowStyles: {
        fillColor: [245, 245, 245],
      },
    });
  });

  doc.save(fileName);
};
