import React from 'react';
import { Filter } from 'lucide-react';

const GeneratorsReportTab = ({
  data,
  lang,
  genSubStation,
  setGenSubStation,
  subStations,
  getSubStationOptionLabel,
  fetchReport,
  formatReadingDateTime,
  getGeneratorFuelType,
  getGeneratorConsumptionRate
}) => {
  const totals = data.generators?.reduce((acc, r) => ({
    rated: acc.rated + (r.generator?.ratedCapacityKw || 0),
    actual: acc.actual + (r.generator?.actualPowerKw || 0),
    max: acc.max + (r.maxLoadKw || 0),
    min: acc.min + (r.minLoadKw || 0),
    hours: acc.hours + (r.operatingHours || 0),
    oil: acc.oil + (r.oilAddedLiters || 0),
    fuel: acc.fuel + (r.fuelConsumedLiters || 0),
    energy: acc.energy + (r.energyProducedKwh || 0),
  }), { rated: 0, actual: 0, max: 0, min: 0, hours: 0, oil: 0, fuel: 0, energy: 0 }) || { rated: 0, actual: 0, max: 0, min: 0, hours: 0, oil: 0, fuel: 0, energy: 0 };

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
        <table className="report-table generators-report-table">
          <thead>
            <tr className="header-row-1">
              <th rowSpan="2">{lang === 'ar' ? 'الموقع/المحطة' : 'Station/Location'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'تاريخ ووقت القراءة' : 'Reading DateTime'}</th>
              <th rowSpan="2" className="by-cell">{lang === 'ar' ? 'بواسطة' : 'By'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'المولد' : 'Generator'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'الرقم التسلسلي' : 'Serial No'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'نوع الملكية' : 'Ownership'}</th>
              <th colSpan="2">{lang === 'ar' ? 'القدرة' : 'Power'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'أقصى الأحمال' : 'Max Load'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'أدنى الأحمال' : 'Min Load'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'ساعات التشغيل' : 'Op. Hours'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'استهلاك الزيت' : 'Oil Cons.'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'الوقود المستهلك' : 'Fuel Used'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'نوع الوقود' : 'Fuel Type'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'معدل إستهلاك الوقود' : 'Cons. Rate'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'الطاقة المنتجة' : 'Energy Prod.'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'متوسط التوليد' : 'Avg Gen.'}</th>
            </tr>
            <tr className="header-row-2">
              <th>{lang === 'ar' ? 'الاسمية' : 'Rated'}</th>
              <th>{lang === 'ar' ? 'الفعلية' : 'Actual'}</th>
            </tr>
            <tr className="unit-row">
              <th>#</th><th style={{ fontSize: 7 }}>DD/MM/YYYY HH:mm</th><th>-</th><th>#</th><th>S/N</th><th>-</th><th>-</th><th>ك.و</th><th>ك.و</th><th>ك.و</th>
              <th>ساعة</th><th>لتر</th><th>لتر</th><th>-</th><th>لتر/ك.و</th><th>ك.و</th><th>ك.و</th>
            </tr>
          </thead>
          <tbody>
            {data.generators?.map((r, i) => (
              <tr key={i}>
                <td className="bold-cell" style={{ color: '#ffffff' }}>{r.locationName || r.station?.name || '—'}</td>
                <td style={{ fontSize: 10, whiteSpace: 'nowrap' }}>{formatReadingDateTime(r.readingDateTime)}</td>
                <td className="by-cell">{r.createdByName || '—'}</td>
                <td className="bold-cell">{r.generator?.name || '—'}</td>
                <td style={{ fontSize: 11 }}>{r.generator?.serialNo || '—'}</td>
                <td style={{ fontSize: 11, fontWeight: 600, color: r.generator?.ownershipType === 'مستأجر' ? '#f59e0b' : '#3b82f6' }}>{r.generator?.ownershipType === 'مستأجر' ? (lang === 'ar' ? 'مستأجر' : 'Rent') : (lang === 'ar' ? 'مملوك' : 'Owned')}</td>
                <td>{r.generator?.ratedCapacityKw || 0}</td>
                <td>{r.generator?.actualPowerKw || 0}</td>
                <td>{r.maxLoadKw || 0}</td>
                <td>{r.minLoadKw || 0}</td>
                <td>{r.operatingHours?.toFixed(3) || '0.000'}</td>
                <td>{(r.oilAddedLiters || 0).toFixed(3)}</td>
                <td>{(r.fuelConsumedLiters || 0).toFixed(3)}</td>
                <td>{getGeneratorFuelType(r)}</td>
                <td className="rate-cell">{getGeneratorConsumptionRate(r)}</td>
                <td>{r.energyProducedKwh || 0}</td>
                <td>{r.operatingHours > 0 ? (r.energyProducedKwh / r.operatingHours).toFixed(3) : '0.000'}</td>
              </tr>
            ))}
            {(data.generators?.length > 0) && (
              <tr className="total-row">
                <td className="total-row-label" colSpan="6">{lang === 'ar' ? 'إجمالي' : 'Total'}</td>
                <td>{totals.rated}</td><td>{totals.actual}</td>
                <td>{totals.max}</td><td>{totals.min}</td>
                <td>{totals.hours.toFixed(3)}</td><td>{totals.oil.toFixed(3)}</td><td>{totals.fuel.toFixed(3)}</td>
                <td>—</td>
                <td className="rate-cell">{totals.energy > 0 ? Number((totals.fuel / totals.energy).toFixed(4)) : '0'}</td>
                <td>{totals.energy}</td>
                <td>{totals.hours > 0 ? (totals.energy / totals.hours).toFixed(3) : '0.000'}</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default GeneratorsReportTab;
