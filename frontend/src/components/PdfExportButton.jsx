import React from 'react';
import { FileText } from 'lucide-react';
import { exportToPdf } from '../utils/pdfExport';

const PdfExportButton = ({ headers, rows, title, fileName, lang }) => {
  return (
    <button
      className="btn btn-ghost"
      onClick={() => exportToPdf(headers, rows, title, fileName)}
      style={{ display: 'flex', alignItems: 'center', gap: 8 }}
    >
      <FileText size={16} />
      <span>{lang === 'ar' ? 'تصدير PDF' : 'Export PDF'}</span>
    </button>
  );
};

export default PdfExportButton;
