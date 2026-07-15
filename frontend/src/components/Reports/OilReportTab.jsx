import React from 'react';
import { Filter } from 'lucide-react';

const OilReportTab = ({
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
        <table className="report-table oil-table">
          <thead>
            <tr>
              <th>{lang === 'ar' ? 'الموقع' : 'Location'}</th>
              <th>{lang === 'ar' ? 'تاريخ ووقت القراءة' : 'Reading DateTime'}</th>
              <th>{lang === 'ar' ? 'نوع الزيت' : 'Oil Type'}</th>
              <th>{lang === 'ar' ? 'مخزون أول اليوم' : 'Opening Stock'}</th>
              <th>{lang === 'ar' ? 'الوارد' : 'Incoming'}</th>
              <th>{lang === 'ar' ? 'الصادر' : 'Outgoing'}</th>
              <th>{lang === 'ar' ? 'الفاقد' : 'Loss'}</th>
              <th>{lang === 'ar' ? 'المستهلك' : 'Consumed'}</th>
              <th>{lang === 'ar' ? 'مخزون نهاية اليوم' : 'Closing Stock'}</th>
              <th className="by-cell">{lang === 'ar' ? 'بواسطة' : 'By'}</th>
            </tr>
          </thead>
          <tbody>
            {data.oil?.length === 0 ? (
              <tr><td colSpan="10" className="empty-msg">{lang === 'ar' ? 'لا توجد بيانات حركة زيوت لهذا اليوم' : 'No oil movements for this day'}</td></tr>
            ) : data.oil?.map((r, i) => (
              <tr key={i}>
                <td className="bold-cell">{r.location}</td>
                <td style={{ fontSize: 10, whiteSpace: 'nowrap' }}>{formatReadingDateTime(r.readingDateTime)}</td>
                <td><span className="oil-type-cell">{r.oilType}</span></td>
                <td>{r.opening}</td>
                <td className="positive-cell">{r.incoming}</td>
                <td className="negative-cell">{r.outgoing || 0}</td>
                <td className="negative-cell">{r.loss || 0}</td>
                <td className="negative-cell">{r.consumed}</td>
                <td className="bold-cell">{r.closing}</td>
                <td className="by-cell">{r.createdByName || '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default OilReportTab;
