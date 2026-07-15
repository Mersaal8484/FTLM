import React from 'react';
import { Filter } from 'lucide-react';

const StationReportTab = ({
  data,
  lang,
  genSubStation,
  setGenSubStation,
  subStations,
  getSubStationOptionLabel,
  fetchReport
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
        <div className="report-subtitle">{lang === 'ar' ? 'بيانات رصد الطاقة المرسلة للشبكة (مقارنة الفترات)' : 'Grid Sent Energy Monitoring (Period Comparison)'}</div>
        <table className="report-table station-table">
          <thead>
            <tr className="header-row-1">
              <th rowSpan="2">{lang === 'ar' ? 'الموقع/المحطة' : 'Station/Location'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'التاريخ' : 'Date'}</th>
              <th rowSpan="2" className="by-cell hidden-report-author-cell">{lang === 'ar' ? 'بواسطة' : 'By'}</th>
              <th colSpan="3">{lang === 'ar' ? 'الطاقة المرسلة (KWh)' : 'Sent Energy (KWh)'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'استهلاك داخلي (KWh)' : 'Internal Cons. (KWh)'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'نسبة الفاقد (%)' : 'Loss Pct (%)'}</th>
            </tr>
            <tr className="header-row-2">
              <th>{lang === 'ar' ? 'يومي' : 'Daily'}</th>
              <th>{lang === 'ar' ? 'فترة (15 يوم)' : '15 Days'}</th>
              <th>{lang === 'ar' ? 'شهر (30 يوم)' : '30 Days'}</th>
            </tr>
          </thead>
          <tbody>
            {data.stationReadings?.length === 0 ? (
              <tr><td colSpan="8" className="empty-msg">{lang === 'ar' ? 'لا توجد قراءات مجمعّة للمحطة لهذا اليوم' : 'No station readings for this day'}</td></tr>
            ) : data.stationReadings?.map((r, i) => (
              <tr key={i}>
                <td className="bold-cell">{r.locationName || r.station?.name}</td>
                <td style={{ fontSize: '11px' }}>{new Date(r.readingDate).toLocaleDateString()}</td>
                <td className="by-cell hidden-report-author-cell">{r.createdByName || '—'}</td>
                <td className="bold-cell" style={{ color: 'var(--primary)' }}>{r.energySentKwh}</td>
                <td style={{ fontWeight: 600 }}>{r.energySent15d}</td>
                <td style={{ fontWeight: 600 }}>{r.energySent30d}</td>
                <td className="negative-cell">{r.internalConsumptionKwh}</td>
                <td className={r.lossPct > 10 ? 'negative-cell' : 'positive-cell'}>{r.lossPct}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default StationReportTab;
