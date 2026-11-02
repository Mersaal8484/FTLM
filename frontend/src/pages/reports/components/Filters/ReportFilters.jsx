import React from 'react';
import { Calendar, Search } from 'lucide-react';
import HierarchicalFilter from '../../../../components/common/HierarchicalFilter';
import { DATE_SHORTCUTS } from '../../hooks/useReportFilters';

export default function ReportFilters({ filters, lang, hideLocation = false }) {
  const isAr = lang === 'ar';

  return (
    <div className="report-filters-container">
      <div className="report-filters-grid">
        <div className="filter-group date-group">
          <div className="date-inputs">
            <div className="input-with-icon">
              <Calendar size={16} className="text-gray-400" />
              <input
                type="date"
                value={filters.dateFrom}
                onChange={(e) => filters.setDateFrom(e.target.value)}
                className="form-control"
              />
            </div>
            <span className="date-separator">{isAr ? 'إلى' : 'to'}</span>
            <div className="input-with-icon">
              <Calendar size={16} className="text-gray-400" />
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
    </div>
  );
}
