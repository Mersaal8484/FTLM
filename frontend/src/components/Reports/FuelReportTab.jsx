import React from 'react';
import { Filter } from 'lucide-react';

const FuelReportTab = ({
  data,
  lang,
  genSubStation,
  setGenSubStation,
  subStations,
  getSubStationOptionLabel,
  fetchReport,
  formatReadingDateTime
}) => {
  return (
    <div>
      <div className="no-print" style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap', padding: '16px 22px', borderBottom: '1px solid var(--border)', background: 'hsla(220,40%,60%,0.02)' }}>
        <select className="form-select" value={genSubStation} onChange={e => setGenSubStation(e.target.value)} style={{ width: 260, padding: '7px 10px', fontSize: 12 }}>
          <option value="">{lang === 'ar' ? 'كل المواقع' : 'All Locations'}</option>
          {subStations.map(s => (
            <option key={s.id || s._id} value={s.id || s._id}>
              {getSubStationOptionLabel(s)}
            </option>
          ))}
        </select>
        <button className="btn btn-sm btn-primary" onClick={() => fetchReport()}>
          <Filter size={14} />
          <span>{lang === 'ar' ? 'تصفية' : 'Filter'}</span>
        </button>
      </div>
      <div className="table-container">
        <div className="report-subtitle">{lang === 'ar' ? 'مخزون الوقود (لتر)' : 'Fuel Inventory (L)'}</div>
        <table className="report-table fuel-table">
          <thead>
            <tr>
              <th>{lang === 'ar' ? 'موقع الخزان الرئيسي' : 'Main Tank Location'}</th>
              <th>{lang === 'ar' ? 'تاريخ ووقت القراءة' : 'Reading DateTime'}</th>
              <th style={{ textAlign: 'center' }}>{lang === 'ar' ? 'نوع الوقود' : 'Fuel Type'}</th>
              <th>{lang === 'ar' ? 'مخزون أول اليوم' : 'Opening Stock'}</th>
              <th>{lang === 'ar' ? 'الوارد' : 'Incoming'}</th>
              <th>{lang === 'ar' ? 'المستهلك' : 'Consumed'}</th>
              <th>{lang === 'ar' ? 'مخزون نهاية اليوم' : 'Closing Stock'}</th>
              <th>{lang === 'ar' ? 'المتاح' : 'Available'}</th>
              <th>{lang === 'ar' ? 'الوقود يكفي لمدة (يوم)' : 'Fuel Lasts For (Days)'}</th>
              <th className="by-cell hidden-report-author-cell">{lang === 'ar' ? 'بواسطة' : 'By'}</th>
            </tr>
          </thead>
          <tbody>
            {data.fuel?.length === 0 ? (
              <tr><td colSpan="10" className="empty-msg">{lang === 'ar' ? 'لا توجد بيانات حركة وقود لهذا اليوم' : 'No fuel movements for this day'}</td></tr>
            ) : data.fuel?.map((r, i) => (
              <tr key={i}>
                <td className="bold-cell">{r.location}</td>
                <td style={{ fontSize: 10, whiteSpace: 'nowrap' }}>{formatReadingDateTime(r.readingDateTime)}</td>
                <td style={{ textAlign: 'center' }}>
                  <span style={{
                    background: '#f59e0b20', color: '#f59e0b',
                    border: '1px solid #f59e0b40', borderRadius: 5,
                    padding: '2px 8px', fontSize: 11, fontWeight: 700,
                    whiteSpace: 'nowrap'
                  }}>{r.fuelType || 'diesel'}</span>
                </td>
                <td>{r.opening}</td>
                <td className="positive-cell">{r.incoming}</td>
                <td className="negative-cell">{r.consumed}</td>
                <td>{r.closing}</td>
                <td className="available-cell">{r.available}</td>
                <td className="days-cell">{r.daysRemaining}</td>
                <td className="by-cell hidden-report-author-cell">{r.createdByName || '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default FuelReportTab;
