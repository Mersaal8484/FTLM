import React from 'react';
import { Search } from 'lucide-react';
import HierarchicalFilter from '../../../../components/common/HierarchicalFilter';
import { DATE_SHORTCUTS } from '../../hooks/useReportFilters';
import useCommercialMode from '../../../../hooks/useCommercialMode';

export default function ReportFilters({ filters, lang, hideLocation = false }) {
  const isAr = lang === 'ar';
  const { isCommercialMode } = useCommercialMode();

  const setCommercialDateFrom = (value) => {
    filters.setDateFrom(value);
    if (filters.dateTo && value > filters.dateTo) filters.setDateTo(value);
  };

  const setCommercialDateTo = (value) => {
    filters.setDateTo(value);
    if (filters.dateFrom && value < filters.dateFrom) filters.setDateFrom(value);
  };

  return (
    <div className={`report-filters-container ${isCommercialMode ? 'report-filters-container-commercial' : ''}`}>
      {isCommercialMode ? (
        <div className="report-filters-commercial-row">
          <label className="report-date-field">
            <span>{isAr ? 'من تاريخ' : 'From date'}</span>
            <input
              type="date"
              value={filters.dateFrom}
              onChange={(e) => setCommercialDateFrom(e.target.value)}
              className="form-control"
            />
          </label>
          <label className="report-date-field">
            <span>{isAr ? 'إلى تاريخ' : 'To date'}</span>
            <input
              type="date"
              value={filters.dateTo}
              onChange={(e) => setCommercialDateTo(e.target.value)}
              className="form-control"
            />
          </label>
          <label className="report-search-field">
            <span>{isAr ? 'بحث' : 'Search'}</span>
            <div className="input-with-icon">
              <Search size={16} className="text-gray-400" />
              <input
                type="text"
                placeholder={isAr ? 'بحث...' : 'Search...'}
                value={filters.searchQuery}
                onChange={(e) => filters.setSearchQuery(e.target.value)}
                className="form-control"
              />
            </div>
          </label>
        </div>
      ) : (
      <div className="report-filters-grid">
        <div className="filter-group date-group">
          <div className="date-inputs">
            <div className="input-with-icon">
              <input
                type="date"
                value={filters.dateFrom}
                onChange={(e) => filters.setDateFrom(e.target.value)}
                className="form-control"
              />
            </div>
            <span className="date-separator">{isAr ? 'إلى' : 'to'}</span>
            <div className="input-with-icon">
              <input
                type="date"
                value={filters.dateTo}
                onChange={(e) => filters.setDateTo(e.target.value)}
                className="form-control"
              />
            </div>
          </div>
          
          <div className="date-shortcuts">
            {DATE_SHORTCUTS.map(shortcut => (
              <button
                key={shortcut.key}
                className="shortcut-pill"
                onClick={() => filters.applyShortcut(shortcut.key)}
              >
                {isAr ? shortcut.ar : shortcut.en}
              </button>
            ))}
          </div>
        </div>

        {!hideLocation && (
          <div className="filter-group location-group">
            <HierarchicalFilter
              branchId={filters.branchId}
              stationId={filters.stationId}
              subStationId={filters.subStationId}
              onFilterChange={(updates) => {
                if (updates.branchId !== undefined) filters.setBranchId(updates.branchId);
                if (updates.stationId !== undefined) filters.setStationId(updates.stationId);
                if (updates.subStationId !== undefined) filters.setSubStationId(updates.subStationId);
              }}
              lang={lang}
            />
          </div>
        )}

        <div className="filter-group search-group">
          <div className="input-with-icon">
            <Search size={16} className="text-gray-400" />
            <input
              type="text"
              placeholder={isAr ? 'بحث...' : 'Search...'}
              value={filters.searchQuery}
              onChange={(e) => filters.setSearchQuery(e.target.value)}
              className="form-control"
            />
          </div>
        </div>
      </div>
      )}
    </div>
  );
}
