import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { 
  FileText, 
  Calendar as CalendarIcon, 
  Settings, 
  Droplet, 
  Zap,
  Printer,
  FileSpreadsheet,
  Truck,
  Search,
  Filter,
  ArrowRightLeft,
  AlertCircle
} from 'lucide-react';
import client from '../api/client';
import * as XLSX from 'xlsx-js-style';
import Pagination from '../components/Pagination';
import PdfExportButton from '../components/PdfExportButton';
import { exportPeriodicToPdf } from '../utils/pdfExport';
import GeneratorsReportTab from '../components/Reports/GeneratorsReportTab';
import OilReportTab from '../components/Reports/OilReportTab';
import FuelReportTab from '../components/Reports/FuelReportTab';
import StationReportTab from '../components/Reports/StationReportTab';

import useReportsVisibility from '../permissions/useReportsVisibility';

const REPORT_TABS = [
  { key: 'generators', permission: 'generators', icon: Settings, ar: 'المولدات', en: 'Generators' },
  { key: 'oil', permission: 'oil_report', icon: Droplet, ar: 'الزيوت', en: 'Oils' },
  { key: 'fuel', permission: 'fuel_report', icon: Zap, ar: 'الوقود', en: 'Fuel' },
  { key: 'station', permission: 'station_report', icon: Zap, ar: 'قراءات الموقع', en: 'Site Readings' },
  { key: 'fuel-incoming', permission: 'fuel_incoming', icon: Truck, ar: 'الوقود الوارد', en: 'Fuel Incoming' },
  { key: 'fuel-transfer', permission: 'fuel_transfer', icon: ArrowRightLeft, ar: 'نقل الوقود', en: 'Fuel Transfer' },
  { key: 'fuel-loss', permission: 'fuel_loss', icon: AlertCircle, ar: 'تقرير الفاقد', en: 'Loss Report' },
];

const FULL_DAILY_REPORT_TABS = ['generators', 'oil', 'fuel', 'station'];
const EMPTY_FULL_DAILY_REPORT = {
  generators: [],
  oil: [],
  fuel: [],
  stationReadings: [],
  inventoryEfficiency: [],
};
const EMPTY_PAGINATED_REPORT = { data: [], pagination: { total: 0, page: 1, pages: 1 } };

function Reports({ lang, currentUser }) {
  const reportsVisibility = useReportsVisibility();
  const rv = reportsVisibility;   // alias used for periodic sub-tab visibility checks
  const [activeTab, setActiveTab] = useState('generators');
  const [reportDateFrom, setReportDateFrom] = useState(new Date().toISOString().split('T')[0]);
  const [reportDateTo, setReportDateTo] = useState(new Date().toISOString().split('T')[0]);
  const [data, setData] = useState({ generators: [], oil: [], fuel: [], stationReadings: [] });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // Fuel Incoming state
  const [fuelIncoming, setFuelIncoming] = useState({ data: [], pagination: { total: 0, page: 1, pages: 1 } });
  const [fiLoading, setFiLoading] = useState(false);
  const [fiDateFrom, setFiDateFrom] = useState('');
  const [fiDateTo, setFiDateTo] = useState('');
  const [fiSearch, setFiSearch] = useState('');
  const [fiPage, setFiPage] = useState(1);
  const [fiStation, setFiStation] = useState('');
  const [stations, setStations] = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [fiFuelType, setFiFuelType] = useState('');

  // Fuel Transfer state
  const [fuelTransfer, setFuelTransfer] = useState({ data: [], pagination: { total: 0, page: 1, pages: 1 } });
  const [ftLoading, setFtLoading] = useState(false);
  const [ftDateFrom, setFtDateFrom] = useState('');
  const [ftDateTo, setFtDateTo] = useState('');
  const [ftSearch, setFtSearch] = useState('');
  const [ftPage, setFtPage] = useState(1);
  const [ftStation, setFtStation] = useState('');

  // Fuel/Oil Loss state
  const [fuelLoss, setFuelLoss] = useState({ data: [], pagination: { total: 0, page: 1, pages: 1 } });
  const [flLoading, setFlLoading] = useState(false);
  const [flDateFrom, setFlDateFrom] = useState('');
  const [flDateTo, setFlDateTo] = useState('');
  const [flSearch, setFlSearch] = useState('');
  const [flPage, setFlPage] = useState(1);
  const [flSubStation, setFlSubStation] = useState('');
  const [flItemType, setFlItemType] = useState('');

  // Generators report filter state
  const [genSubStation, setGenSubStation] = useState('');

  // Periodic Reports state
  const [periodicDateFrom, setPeriodicDateFrom] = useState('');
  const [periodicDateTo, setPeriodicDateTo] = useState('');
  const [periodicStation, setPeriodicStation] = useState('');
  const [periodicData, setPeriodicData] = useState(null);
  const [periodicLoading, setPeriodicLoading] = useState(false);
  const [periodicError, setPeriodicError] = useState(null);
  const [periodicSubTab, setPeriodicSubTab] = useState('generation');
  // Auto-switch to first visible periodic sub-tab when role visibility changes
  useEffect(() => {
    const visiblePeriodic = [
      { key: 'generation', rvKey: 'periodic_generation' },
      { key: 'fuel',       rvKey: 'periodic_fuel'       },
      { key: 'oil',        rvKey: 'periodic_oil'        },
    ].filter(t => rv[t.rvKey] !== false);
    if (visiblePeriodic.length > 0 && !visiblePeriodic.some(t => t.key === periodicSubTab)) {
      setPeriodicSubTab(visiblePeriodic[0].key);
    }
  }, [rv, periodicSubTab]);

  // Permission helpers
  const perms = useMemo(
    () => currentUser?.permissions?.reports || {},
    [currentUser?.permissions?.reports]
  );
  const isAdmin = currentUser?.role === 'admin';
  const hasReportTabConfiguration = useMemo(
    () => REPORT_TABS.some(tab => Object.prototype.hasOwnProperty.call(perms, tab.permission)),
    [perms]
  );
  const canAccessReportTab = useCallback((tabKey) => {
    const tab = REPORT_TABS.find(item => item.key === tabKey);
    if (!tab) return false;
    if (isAdmin) return true;
    if (perms?.[tab.permission] === true) return true;
    return perms?.view === true && !hasReportTabConfiguration;
  }, [isAdmin, perms, hasReportTabConfiguration]);
  const allowedReportTabs = useMemo(
    () => {
      const rvTabs = reportsVisibility;
      return REPORT_TABS.filter(
        tab => canAccessReportTab(tab.key) && rvTabs[tab.key] !== false
      );
    },
    [canAccessReportTab, reportsVisibility]
  );
  const canLoadFullDaily = FULL_DAILY_REPORT_TABS.some(canAccessReportTab);
  const canAccessPeriodicReports = isAdmin || perms?.view === true || perms?.generators === true || perms?.fuel_report === true || perms?.oil_report === true;
  const canExport = (isAdmin || perms.export) && canAccessReportTab(activeTab);
  const canPrint = (isAdmin || perms.print) && canAccessReportTab(activeTab);

  const getPdfData = useCallback(() => {
    const ar = lang === 'ar';
    if (activeTab === 'generators') {
      return {
        headers: [
          ar ? 'الموقع/المحطة' : 'Station/Location',
          ar ? 'التاريخ' : 'DateTime',
          ar ? 'المولد' : 'Generator',
          ar ? 'الرقم التسلسلي' : 'Serial No',
          ar ? 'القدرة الاسمية' : 'Rated kW',
          ar ? 'القدرة الفعلية' : 'Actual kW',
          ar ? 'ساعات التشغيل' : 'Hours',
          ar ? 'الوقود المستهلك' : 'Fuel (L)',
          ar ? 'الطاقة المنتجة' : 'Energy (KWh)',
        ],
        rows: (data.generators || []).map(r => [
          r.locationName || r.station?.name || '',
          r.readingDateTime ? new Date(r.readingDateTime).toLocaleString() : '',
          r.generator?.name || '',
          r.generator?.serialNo || '',
          r.generator?.ratedCapacityKw || 0,
          r.generator?.actualPowerKw || 0,
          r.operatingHours || 0,
          r.fuelConsumedLiters || 0,
          r.energyProducedKwh || 0,
        ]),
        title: ar ? 'تقرير المولدات' : 'Generators Report',
      };
    }
    if (activeTab === 'oil') {
      return {
        headers: [
          ar ? 'الموقع' : 'Location', ar ? 'التاريخ' : 'DateTime',
          ar ? 'نوع الزيت' : 'Oil Type', ar ? 'الأول' : 'Opening',
          ar ? 'الوارد' : 'Incoming', ar ? 'الصادر' : 'Outgoing',
          ar ? 'الفاقد' : 'Loss', ar ? 'المستهلك' : 'Consumed',
          ar ? 'النهاية' : 'Closing',
        ],
        rows: (data.oil || []).map(r => [
          r.location, r.readingDateTime ? new Date(r.readingDateTime).toLocaleString() : '',
          r.oilType, r.opening, r.incoming, r.outgoing || 0, r.loss || 0, r.consumed, r.closing,
        ]),
        title: ar ? 'تقرير الزيوت' : 'Oil Report',
      };
    }
    if (activeTab === 'fuel') {
      return {
        headers: [
          ar ? 'الموقع' : 'Location', ar ? 'التاريخ' : 'DateTime',
          ar ? 'نوع الوقود' : 'Fuel Type', ar ? 'الأول' : 'Opening',
          ar ? 'الوارد' : 'Incoming', ar ? 'المستهلك' : 'Consumed',
          ar ? 'النهاية' : 'Closing', ar ? 'المتاح' : 'Available',
          ar ? 'يكفي (يوم)' : 'Days Left',
        ],
        rows: (data.fuel || []).map(r => [
          r.location, r.readingDateTime ? new Date(r.readingDateTime).toLocaleString() : '',
          r.fuelType || 'diesel', r.opening, r.incoming, r.consumed, r.closing, r.available, r.daysRemaining,
        ]),
        title: ar ? 'تقرير الوقود' : 'Fuel Report',
      };
    }
    if (activeTab === 'station') {
      return {
        headers: [
          ar ? 'الموقع' : 'Location', ar ? 'التاريخ' : 'Date',
          ar ? 'المرسلة' : 'Sent', ar ? 'الاستهلاك' : 'Consumption',
          ar ? 'نسبة الفاقد' : 'Loss %',
        ],
        rows: (data.stationReadings || []).map(r => [
          r.locationName || r.station?.name || '',
          r.readingDate ? new Date(r.readingDate).toLocaleDateString() : '',
          r.energySentKwh || 0, r.internalConsumptionKwh || 0, r.lossPct || 0,
        ]),
        title: ar ? 'قراءات الموقع' : 'Station Readings',
      };
    }
    return { headers: [], rows: [], title: '' };
  }, [activeTab, data, lang]);

  const getPeriodicPdfData = useCallback(() => {
    if (!periodicData) return { sheets: [] };
    const ar = lang === 'ar';
    const sheets = [];

    // Generation
    const genRows = periodicData.siteGenerationReport || [];
    if (genRows.length > 0) {
      sheets.push({
        title: ar ? 'تقرير التوليد' : 'Generation Report',
        headers: [
          ar ? 'الموقع' : 'Site', ar ? 'عدد المولدات' : 'Gens',
          ar ? 'القدرة الاسمية' : 'Rated kW', ar ? 'القدرة الفعلية' : 'Actual kW',
          ar ? 'الطاقة المنتجة' : 'Produced KWh', ar ? 'ساعات التشغيل' : 'Hours',
          ar ? 'الطاقة المرسلة' : 'Sent KWh', ar ? 'الاستهلاك' : 'Int. Cons.',
          ar ? 'متوسط التوليد' : 'Avg KW/h',
        ],
        rows: genRows.map(r => [
          r.siteLabel || '', r.generatorCount || 0, r.ratedCapacityKw || 0,
          r.actualCapacityKw || 0, r.energyProducedKwh || 0, r.operatingHours || 0,
          r.energySentKwh || 0, r.internalConsumption || 0, r.generationAverage ?? '',
        ]),
      });
    }

    // Fuel
    const fuelRows = periodicData.fuelReport || [];
    if (fuelRows.length > 0) {
      sheets.push({
        title: ar ? 'تقرير الوقود' : 'Fuel Report',
        headers: [
          ar ? 'الموقع' : 'Site', ar ? 'نوع الوقود' : 'Fuel Type',
          ar ? 'الأول' : 'Opening', ar ? 'الوارد' : 'Incoming',
          ar ? 'الصادر' : 'Outgoing', ar ? 'الفاقد' : 'Lost',
          ar ? 'النهاية' : 'Closing', ar ? 'المستهلك' : 'Consumed',
          ar ? 'المعدل' : 'Rate L/KWh', ar ? 'الكفاءة' : 'Eff %',
        ],
        rows: fuelRows.map(r => [
          r.siteLabel || '', r.fuelType || '', r.openingFuel || 0,
          r.incomingFuel || 0, r.outgoingFuel || 0, r.lostFuel || 0,
          r.closingFuelActual || 0, r.actualConsumedFuel || 0,
          r.actualConsumptionRate ?? '', r.efficiency ?? '',
        ]),
      });
    }

    // Oil
    const oilRows = periodicData.oilReport || [];
    if (oilRows.length > 0) {
      sheets.push({
        title: ar ? 'تقرير الزيوت' : 'Oil Report',
        headers: [
          ar ? 'الموقع' : 'Site', ar ? 'نوع الزيت' : 'Oil Type',
          ar ? 'الأول' : 'Opening', ar ? 'الوارد' : 'Incoming',
          ar ? 'الصادر' : 'Outgoing', ar ? 'الفاقد' : 'Lost',
          ar ? 'النهاية' : 'Closing', ar ? 'المستهلك' : 'Consumed',
          ar ? 'المعدل' : 'Rate L/h',
        ],
        rows: oilRows.map(r => [
          r.siteLabel || '', r.oilName || '', r.openingOil || 0,
          r.incomingOil || 0, r.outgoingOil || 0, r.lossOil || 0,
          r.closingOil || 0, r.consumedOil || 0, r.consumptionRate ?? '',
        ]),
      });
    }

    return { sheets };
  }, [periodicData, lang]);

  const fetchReport = useCallback(async () => {
    if (!canLoadFullDaily) {
      setData(EMPTY_FULL_DAILY_REPORT);
      setError(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const params = {};
      if (reportDateFrom === reportDateTo) {
        params.date = reportDateFrom;
      } else {
        params.dateFrom = reportDateFrom;
        params.dateTo = reportDateTo;
      }
      if (genSubStation) params.subStation = genSubStation;
      const res = await client.get('/api/reports/full-daily', { params });
      setData(res.data);
    } catch (err) {
      const status = err.response?.status;
      const serverMsg = err.response?.data?.msg;
      if (status === 403) {
        setError(lang === 'ar' ? 'ليس لديك صلاحية للوصول إلى التقرير' : (serverMsg || 'No permission to access this report'));
      } else if (status === 400) {
        setError(serverMsg || (lang === 'ar' ? 'طلب غير صحيح' : 'Bad request'));
      } else if (status === 500) {
        setError(lang === 'ar' ? 'خطأ في الخادم أثناء إنشاء التقرير' : 'Server error while generating report');
      } else if (err.code === 'ERR_NETWORK') {
        setError(lang === 'ar' ? 'تعذر الاتصال بالخادم' : 'Could not connect to the server');
      } else {
        setError(serverMsg || (lang === 'ar' ? 'فشل تحميل التقرير' : 'Failed to load report'));
      }
    } finally {
      setLoading(false);
    }
  }, [canLoadFullDaily, reportDateFrom, reportDateTo, genSubStation, lang]);

  // Fetch functions always require search to be passed explicitly
  const doFetchFi = useCallback(async (searchVal) => {
    if (!canAccessReportTab('fuel-incoming')) {
      setFuelIncoming(EMPTY_PAGINATED_REPORT);
      return;
    }
    setFiLoading(true);
    try {
      const params = { page: fiPage, limit: 50 };
      if (fiDateFrom) params.dateFrom = fiDateFrom;
      if (fiDateTo) params.dateTo = fiDateTo;
      if (fiStation) params.subStation = fiStation;
      if (fiFuelType) params.fuelType = fiFuelType;
      if (searchVal) params.search = searchVal;
      const res = await client.get('/api/reports/fuel-incoming', { params });
      setFuelIncoming(res.data);
    } catch (err) {
      console.error('Fuel incoming fetch error:', err);
    } finally {
      setFiLoading(false);
    }
  }, [fiPage, fiDateFrom, fiDateTo, fiStation, fiFuelType, canAccessReportTab]);

  const doFetchFt = useCallback(async (searchVal) => {
    if (!canAccessReportTab('fuel-transfer')) {
      setFuelTransfer(EMPTY_PAGINATED_REPORT);
      return;
    }
    setFtLoading(true);
    try {
      const params = { page: ftPage, limit: 50 };
      if (ftDateFrom) params.dateFrom = ftDateFrom;
      if (ftDateTo) params.dateTo = ftDateTo;
      if (ftStation) params.subStation = ftStation;
      if (searchVal) params.search = searchVal;
      const res = await client.get('/api/reports/fuel-transfer', { params });
      setFuelTransfer(res.data);
    } catch (err) {
      console.error('Fuel transfer fetch error:', err);
    } finally {
      setFtLoading(false);
    }
  }, [ftPage, ftDateFrom, ftDateTo, ftStation, canAccessReportTab]);

  const doFetchFl = useCallback(async (searchVal) => {
    if (!canAccessReportTab('fuel-loss')) {
      setFuelLoss(EMPTY_PAGINATED_REPORT);
      return;
    }
    setFlLoading(true);
    try {
      const params = { page: flPage, limit: 50 };
      const effectiveDateFrom = flDateFrom || reportDateFrom;
      const effectiveDateTo = flDateTo || reportDateTo;
      if (effectiveDateFrom) params.dateFrom = effectiveDateFrom;
      if (effectiveDateTo) params.dateTo = effectiveDateTo;
      if (flSubStation) params.subStation = flSubStation;
      if (flItemType) params.itemType = flItemType;
      if (searchVal) params.search = searchVal;
      const res = await client.get('/api/reports/fuel-loss', { params });
      setFuelLoss(res.data);
    } catch (err) {
      console.error('Fuel loss fetch error:', err);
    } finally {
      setFlLoading(false);
    }
  }, [flPage, flDateFrom, flDateTo, reportDateFrom, reportDateTo, flSubStation, flItemType, canAccessReportTab]);

  // Wrappers for useEffect (auto-fetch when tab opens or filters change)
  // Search is NOT included in auto-fetch — only applied via filter button
  const fetchFuelIncoming = useCallback(() => doFetchFi(''), [doFetchFi]);
  const fetchFuelTransfer = useCallback(() => doFetchFt(''), [doFetchFt]);
  const fetchFuelLoss = useCallback(() => doFetchFl(''), [doFetchFl]);

  // Apply search when filter button is clicked
  const applyFiSearch = useCallback(() => {
    setFiPage(1);
    doFetchFi(fiSearch);
  }, [doFetchFi, fiSearch]);

  const applyFtSearch = useCallback(() => {
    setFtPage(1);
    doFetchFt(ftSearch);
  }, [doFetchFt, ftSearch]);

  const applyFlSearch = useCallback(() => {
    setFlPage(1);
    doFetchFl(flSearch);
  }, [doFetchFl, flSearch]);

  const fetchPeriodicReport = useCallback(async () => {
    if (!periodicDateFrom || !periodicDateTo) {
      setPeriodicError(lang === 'ar' ? 'الرجاء تحديد نطاق التاريخ' : 'Please select a date range');
      return;
    }
    setPeriodicLoading(true);
    setPeriodicError(null);
    try {
      const params = { dateFrom: periodicDateFrom, dateTo: periodicDateTo };
      if (periodicStation) params.subStation = periodicStation;
      const res = await client.get('/api/reports/periodic', { params });
      setPeriodicData(res.data);
    } catch (err) {
      const status = err.response?.status;
      const serverMsg = err.response?.data?.msg;
      if (status === 403) {
        setPeriodicError(lang === 'ar' ? 'ليس لديك صلاحية للوصول إلى التقارير الدورية' : (serverMsg || 'No permission for periodic reports'));
      } else if (status === 500) {
        setPeriodicError(lang === 'ar' ? 'خطأ في الخادم أثناء إنشاء التقرير الدوري' : 'Server error while generating periodic report');
      } else if (err.code === 'ERR_NETWORK') {
        setPeriodicError(lang === 'ar' ? 'تعذر الاتصال بالخادم' : 'Could not connect to the server');
      } else {
        setPeriodicError(serverMsg || (lang === 'ar' ? 'فشل تحميل التقارير الدورية' : 'Failed to load periodic reports'));
      }
    } finally {
      setPeriodicLoading(false);
    }
  }, [periodicDateFrom, periodicDateTo, periodicStation, lang]);

  const exportPeriodicExcel = () => {
    if (!periodicData) return;
    const ar = lang === 'ar';
    const wb = XLSX.utils.book_new();

    // Sheet 1: Generation
    const genRows = periodicData.siteGenerationReport || [];
    const genHeaders = [
      ar ? 'الموقع / الفرع' : 'Site / Branch',
      ar ? 'عدد المولدات' : 'Generators',
      ar ? 'القدرة الاسمية' : 'Rated Capacity',
      ar ? 'القدرة الفعلية (ك.و)' : 'Actual Capacity (KW)',
      ar ? 'الطاقة المنتجة (ك.و.س)' : 'Energy Produced (KWh)',
      ar ? 'ساعات التشغيل (س)' : 'Operating Hours (h)',
      ar ? 'الطاقة المرسلة (ك.و.س)' : 'Sent Energy (KWh)',
      ar ? 'الاستهلاك الداخلي (ك.و.س)' : 'Internal Consumption (KWh)',
      ar ? 'متوسط التوليد (ك.و/س)' : 'Generation Average (KW/h)',
    ];
    const genData = genRows.map(r => [
      r.siteLabel || '',
      r.generatorCount || 0,
      r.ratedCapacityKw || 0,
      r.actualCapacityKw || 0,
      r.energyProducedKwh || 0,
      r.operatingHours || 0,
      r.energySentKwh || 0,
      r.internalConsumption || 0,
      r.generationAverage ?? '',
    ]);
    const ws1 = XLSX.utils.aoa_to_sheet([genHeaders, ...genData]);
    if (!ws1['!views']) ws1['!views'] = [];
    ws1['!views'].push({ rightToLeft: ar });
    XLSX.utils.book_append_sheet(wb, ws1, ar ? 'تقرير التوليد' : 'Generation');

    // Sheet 2: Fuel
    const fuelRows = periodicData.fuelReport || [];
    const fuelHeaders = [
      ar ? 'الموقع / الفرع' : 'Site / Branch',
      ar ? 'نوع الوقود' : 'Fuel Type',
      ar ? 'مخزون أول الفترة (لتر)' : 'Opening Fuel (L)',
      ar ? 'الوارد (لتر)' : 'Incoming (L)',
      ar ? 'الصادر (لتر)' : 'Outgoing (L)',
      ar ? 'الفاقد (لتر)' : 'Lost (L)',
      ar ? 'مخزون نهاية الفترة (لتر)' : 'Closing Stock (L)',
      ar ? 'المستهلك الفعلي (لتر)' : 'Consumed (L)',
      ar ? 'معدل الاستهلاك (لتر/ك.و.س)' : 'Cons. Rate (L/KWh)',
      ar ? 'الكفاءة (%)' : 'Efficiency (%)',
    ];
    const fuelData = fuelRows.map(r => {
      const typeTrans = r.fuelType ? r.fuelType.split(', ').map(t => {
        if (t.toLowerCase() === 'diesel') return ar ? 'ديزل' : 'Diesel';
        if (t.toLowerCase() === 'mazout') return ar ? 'مازوت' : 'Mazout';
        if (t.toLowerCase() === 'petrol') return ar ? 'بنزين' : 'Petrol';
        if (t.toLowerCase() === 'kerosene') return ar ? 'كيروسين' : 'Kerosene';
        return t;
      }).join(', ') : (ar ? 'ديزل' : 'Diesel');
      return [
        r.siteLabel || '',
        typeTrans,
        r.openingFuel || 0,
      r.incomingFuel || 0,
      r.outgoingFuel || 0,
      r.lostFuel || 0,
      r.closingFuelActual || 0,
      r.actualConsumedFuel || 0,
      r.actualConsumptionRate ?? '',
      r.efficiency ?? '',
    ];
  });
  const ws2 = XLSX.utils.aoa_to_sheet([fuelHeaders, ...fuelData]);
    if (!ws2['!views']) ws2['!views'] = [];
    ws2['!views'].push({ rightToLeft: ar });
    XLSX.utils.book_append_sheet(wb, ws2, ar ? 'تقرير الوقود' : 'Fuel');

    // Sheet 3: Oil
    const oilRows = periodicData.oilReport || [];
    const oilHeaders = [
      ar ? 'الموقع / الفرع' : 'Site / Branch',
      ar ? 'نوع الزيت' : 'Oil Type',
      ar ? 'مخزون أول الفترة (لتر)' : 'Opening Oil (L)',
      ar ? 'الوارد (لتر)' : 'Incoming (L)',
      ar ? 'الصادر (لتر)' : 'Outgoing (L)',
      ar ? 'الفاقد (لتر)' : 'Lost (L)',
      ar ? 'المخزون الفعلي نهاية الفترة (لتر)' : 'Closing Stock (L)',
      ar ? 'المستهلك الفعلي (لتر)' : 'Consumed (L)',
      ar ? 'معدل الاستهلاك (لتر/س)' : 'Cons. Rate (L/h)',
    ];
    const oilData = oilRows.map(r => [
      r.siteLabel || '',
      r.oilName || '',
      r.openingOil || 0,
      r.incomingOil || 0,
      r.outgoingOil || 0,
      r.lossOil || 0,
      r.closingOil || 0,
      r.consumedOil || 0,
      r.consumptionRate ?? '',
    ]);
    const ws3 = XLSX.utils.aoa_to_sheet([oilHeaders, ...oilData]);
    if (!ws3['!views']) ws3['!views'] = [];
    ws3['!views'].push({ rightToLeft: ar });
    XLSX.utils.book_append_sheet(wb, ws3, ar ? 'تقرير الزيوت' : 'Oil');

    XLSX.writeFile(wb, `periodic-report-${periodicDateFrom}-${periodicDateTo}.xlsx`);
  };

  const printPeriodic = () => {
    if (!periodicData) return;
    const appRoot = document.querySelector('.app-root');
    const reportsPage = document.querySelector('.reports-page');
    const isLight = appRoot?.classList.contains('theme-light');
    if (isLight) appRoot.classList.remove('theme-light');
    reportsPage?.classList.add('print-periodic');
    setTimeout(() => {
      window.print();
      reportsPage?.classList.remove('print-periodic');
      if (isLight) appRoot.classList.add('theme-light');
    }, 80);
  };

  const getActivePrintRange = () => {
    if (activeTab === 'fuel-incoming') return { from: fiDateFrom, to: fiDateTo };
    if (activeTab === 'fuel-transfer') return { from: ftDateFrom, to: ftDateTo };
    if (activeTab === 'fuel-loss') return { from: flDateFrom || reportDateFrom, to: flDateTo || reportDateTo };
    return { from: reportDateFrom, to: reportDateTo };
  };

  const formatPrintRange = () => {
    const { from, to } = getActivePrintRange();
    if (!from && !to) return lang === 'ar' ? 'كل التواريخ' : 'All dates';
    if (from && (!to || from === to)) {
      return lang === 'ar'
        ? `تاريخ التقرير: ${new Date(from).toLocaleDateString('ar-EG')}`
        : `Report Date: ${new Date(from).toLocaleDateString()}`;
    }
    if (!from && to) {
      return lang === 'ar'
        ? `حتى تاريخ: ${new Date(to).toLocaleDateString('ar-EG')}`
        : `Until: ${new Date(to).toLocaleDateString()}`;
    }
    return lang === 'ar'
      ? `الفترة: ${new Date(from).toLocaleDateString('ar-EG')} - ${new Date(to).toLocaleDateString('ar-EG')}`
      : `Period: ${new Date(from).toLocaleDateString()} - ${new Date(to).toLocaleDateString()}`;
  };

  const getRefId = (value) => value?.id || value?._id || value || '';

  const getSubStationParentId = (site) => getRefId(site?.station_id || site?.station);

  const formatStationSiteLabel = (stationName, siteName) => {
    if (stationName && siteName) {
      return String(stationName).includes(String(siteName))
        ? stationName
        : `${stationName} - ${siteName}`;
    }
    return stationName || siteName || '';
  };

  const getSubStationOptionLabel = (site) => {
    const parentId = getSubStationParentId(site);
    const station = stations.find(item => getRefId(item) === parentId);
    return station?.name ? `${station.name} ← ${site?.name || ''}` : (site?.name || '');
  };

  const getSubStationDisplayLabel = (site) => {
    const parentId = getSubStationParentId(site);
    const station = stations.find(item => getRefId(item) === parentId);
    return formatStationSiteLabel(station?.name, site?.name);
  };

  const getMovementSiteName = (movement) => {
    const siteId = getRefId(movement?.tank?.subStation || movement?.oil?.subStation);
    const site = subStations.find(item => getRefId(item) === siteId);
    return site ? getSubStationDisplayLabel(site) : (movement?.station?.name || '');
  };

  const getLossSubStationId = (movement) => (
    movement?.itemType === 'oil'
      ? getRefId(movement?.oil?.subStation)
      : getRefId(movement?.tank?.subStation)
  );

  const getLossSiteName = (movement) => {
    const siteId = getLossSubStationId(movement);
    const site = subStations.find(item => getRefId(item) === siteId);
    return site ? getSubStationDisplayLabel(site) : (movement?.station?.name || '');
  };

  const fetchStations = useCallback(async () => {
    try {
      const [stationsRes, subStationsRes] = await Promise.allSettled([
        client.get('/api/stations'),
        client.get('/api/sub-stations'),
      ]);
      if (stationsRes.status === 'fulfilled') setStations(stationsRes.value.data || []);
      if (subStationsRes.status === 'fulfilled') setSubStations((subStationsRes.value.data || []).filter(s => s.isActive !== false));
    } catch {}
  }, []);

  useEffect(() => {
    if (allowedReportTabs.length > 0 && !canAccessReportTab(activeTab)) {
      setActiveTab(allowedReportTabs[0].key);
    }
  }, [allowedReportTabs, activeTab, canAccessReportTab]);

  useEffect(() => {
    fetchReport();
  }, [fetchReport]);

  useEffect(() => {
    if (activeTab === 'fuel-incoming') {
      fetchFuelIncoming();
    }
  }, [activeTab, fetchFuelIncoming]);

  useEffect(() => {
    if (activeTab === 'fuel-transfer') {
      fetchFuelTransfer();
    }
  }, [activeTab, fetchFuelTransfer]);

  useEffect(() => {
    if (activeTab === 'fuel-loss') {
      fetchFuelLoss();
    }
  }, [activeTab, fetchFuelLoss]);

  useEffect(() => {
    fetchStations();
  }, [fetchStations]);

  const exportExcel = (type) => {
    if (!canAccessReportTab(type)) return;
    let headers = [];
    let rows = [];
    let sheetName = '';
    const ar = lang === 'ar';

    if (type === 'fuel-incoming') {
      sheetName = ar ? 'الوقود الوارد' : 'Fuel Incoming';
      headers = [
        ar ? 'الموقع' : 'Station', ar ? 'الخزان' : 'Tank', ar ? 'المورد' : 'Supplier',
        ar ? 'رقم التوريد' : 'Supply No.', ar ? 'الكمية (لتر)' : 'Qty (L)',
        ar ? 'السواق' : 'Driver', ar ? 'البطاقة' : 'Driver ID',
        ar ? 'رقم القاطرة' : 'Truck No.', ar ? 'رقم المقطورة' : 'Trailer No.',
        ar ? 'رقم الإيصال' : 'Receipt No.', ar ? 'كمية الإيصال' : 'Receipt Qty',
        ar ? 'تاريخ التوريد' : 'Supply Date', ar ? 'تاريخ الإيصال' : 'Receipt Date',
        ar ? 'العداد العياري' : 'Cal. Meter', ar ? 'العداد المشاهد' : 'Obs. Meter',
        ar ? 'الحالة' : 'Status', ar ? 'تاريخ الحركة' : 'Mov. Date', ar ? 'بواسطة' : 'By',
      ];
      rows = fuelIncoming.data.map(r => [
        getMovementSiteName(r), r.tank?.name || '', r.supplierName || '', r.supplyNumber || '',
        r.quantityLiters || 0,
        r.driverName || '', r.driverId || '', r.truckNumber || '', r.trailerNumber || '',
        r.receiptNumber || '', r.receiptQuantity || '',
        r.supplyDate ? new Date(r.supplyDate).toLocaleDateString() : '',
        r.receiptDate ? new Date(r.receiptDate).toLocaleDateString() : '',
        r.calibrationReading || '', r.observedReading || '',
        r.status === 'approved' ? (ar ? 'معتمد' : 'Approved')
          : r.status === 'rejected' ? (ar ? 'مرفوض' : 'Rejected')
          : (ar ? 'قيد الانتظار' : 'Pending'),
        r.movementDate ? new Date(r.movementDate).toLocaleDateString() : '',
        r.createdBy?.name || '',
      ]);
    } else if (type === 'fuel-transfer') {
      sheetName = ar ? 'نقل الوقود' : 'Fuel Transfer';
      headers = [
        ar ? 'من خزان' : 'From Tank', ar ? 'إلى خزان/فرع' : 'To Tank/Branch',
        ar ? 'رقم أمر الصرف' : 'Issue Order No.', ar ? 'تاريخ أمر الصرف' : 'Issue Order Date',
        ar ? 'الموقع المصدر' : 'Source Station', ar ? 'الموقع الوجهة' : 'Target Station',
        ar ? 'الكمية (لتر)' : 'Qty (L)',
        ar ? 'الملاحظات' : 'Notes', ar ? 'الحالة' : 'Status', ar ? 'تاريخ الحركة' : 'Mov. Date', ar ? 'بواسطة' : 'By',
      ];
      rows = fuelTransfer.data.map(r => [
        r.tank?.name || '', r.targetTank?.name || '',
        r.issueOrderNumber || '',
        r.issueOrderDate ? new Date(r.issueOrderDate).toLocaleDateString() : '',
        r.station?.name || '', r.targetStation?.name || '',
        r.quantityLiters || 0, r.notes || '',
        r.status === 'approved' ? (ar ? 'معتمد' : 'Approved')
          : r.status === 'rejected' ? (ar ? 'مرفوض' : 'Rejected')
          : (ar ? 'قيد الانتظار' : 'Pending'),
        r.movementDate ? new Date(r.movementDate).toLocaleDateString() : '',
        r.createdBy?.name || '',
      ]);
    } else if (type === 'fuel-loss') {
      sheetName = ar ? 'تقرير الفاقد' : 'Loss Report';
      headers = [
        ar ? 'نوع المادة' : 'Item Type',
        ar ? 'الموقع' : 'Station',
        ar ? 'الخزان/الزيت' : 'Tank/Oil',
        ar ? 'الكمية' : 'Quantity',
        ar ? 'السبب/التفاصيل' : 'Reason',
        ar ? 'ملاحظات إضافية' : 'Notes',
        ar ? 'تاريخ الحركة' : 'Mov. Date',
        ar ? 'أدخل بواسطة' : 'Created By',
        ar ? 'اعتمد بواسطة' : 'Approved By',
      ];
      rows = fuelLoss.data.map(r => [
        r.itemType === 'oil' ? (ar ? 'زيوت' : 'Oil') : (ar ? 'وقود' : 'Fuel'),
        getLossSiteName(r),
        r.itemType === 'oil' ? (r.oil?.name || '') : (r.tank?.name || ''),
        r.quantityLiters || 0,
        r.reason || '',
        r.notes || '',
        r.movementDate ? new Date(r.movementDate).toLocaleDateString() : '',
        r.createdBy?.name || '',
        r.approvedBy?.name || '',
      ]);
    } else if (type === 'generators') {
      sheetName = lang === 'ar' ? 'المولدات' : 'Generators';
      headers = [
        lang === 'ar' ? 'الموقع/المحطة' : 'Station/Location',
        lang === 'ar' ? 'تاريخ ووقت القراءة' : 'Reading DateTime',
        lang === 'ar' ? 'بواسطة' : 'By',
        lang === 'ar' ? 'المولد' : 'Generator',
        lang === 'ar' ? 'الرقم التسلسلي' : 'Serial No',
        lang === 'ar' ? 'نوع الملكية' : 'Ownership',
        'القدرة الاسمية', 'القدرة الفعلية', 'أقصى الأحمال', 'أدنى الأحمال', 'ساعات التشغيل', 'استهلاك الزيت', 'الوقود المستهلك', lang === 'ar' ? 'نوع الوقود' : 'Fuel Type', 'معدل الاستهلاك', 'الطاقة المنتجة', 'متوسط التوليد'
      ];
      rows = data.generators?.map(r => [
        r.locationName || r.station?.name || '',
        r.readingDateTime ? new Date(r.readingDateTime).toLocaleString('en-GB', { hour12: false }).replace(',', '') : '',
        r.createdByName || '',
        r.generator?.name || '',
        r.generator?.serialNo || '',
        r.generator?.ownershipType === 'مستأجر' ? (lang === 'ar' ? 'مستأجر' : 'Rent') : (lang === 'ar' ? 'مملوك' : 'Owned'),
        r.generator?.ratedCapacityKw || 0,
        r.generator?.actualPowerKw || 0,
        r.maxLoadKw || 0,
        r.minLoadKw || 0,
        r.operatingHours || 0,
        Number((r.oilAddedLiters || 0).toFixed(3)),
        Number((r.fuelConsumedLiters || 0).toFixed(3)),
        getGeneratorFuelType(r),
        getGeneratorConsumptionRate(r),
        r.energyProducedKwh || 0,
        r.operatingHours > 0 ? Number((r.energyProducedKwh / r.operatingHours).toFixed(3)) : 0
      ]) || [];
    } else if (type === 'oil') {
      sheetName = lang === 'ar' ? 'الزيوت' : 'Oils';
      headers = ['الموقع', 'تاريخ ووقت القراءة', 'نوع الزيت', 'مخزون أول اليوم', 'الوارد', 'الصادر', 'الفاقد', 'المستهلك', 'مخزون نهاية اليوم', 'بواسطة'];
      rows = data.oil?.map(r => [
        r.location,
        r.readingDateTime ? new Date(r.readingDateTime).toLocaleString('en-GB', { hour12: false }).replace(',', '') : '',
        r.oilType, r.opening, r.incoming, r.outgoing || 0, r.loss || 0, r.consumed, r.closing, r.createdByName || ''
      ]) || [];
    } else if (type === 'fuel') {
      sheetName = lang === 'ar' ? 'الوقود' : 'Fuel';
      headers = ['الموقع', 'تاريخ ووقت القراءة', 'نوع الوقود', 'مخزون أول اليوم', 'الوارد', 'المستهلك', 'مخزون نهاية اليوم', 'المتاح', 'يكفي لمدة (يوم)', 'بواسطة'];
      rows = data.fuel?.map(r => [
        r.location,
        r.readingDateTime ? new Date(r.readingDateTime).toLocaleString('en-GB', { hour12: false }).replace(',', '') : '',
        r.fuelType || 'diesel', r.opening, r.incoming, r.consumed, r.closing, r.available, r.daysRemaining, r.createdByName || ''
      ]) || [];
    } else if (type === 'station') {
      sheetName = lang === 'ar' ? 'قراءات الموقع' : 'Station Readings';
      headers = ['الموقع', 'التاريخ', 'بواسطة', 'المرسلة (يوم)', 'المرسلة (15 يوم)', 'المرسلة (30 يوم)', 'الاستهلاك الداخلي', 'نسبة الفاقد'];
      rows = data.stationReadings?.map(r => [
        r.locationName || r.station?.name || '',
        new Date(r.readingDate).toLocaleDateString(),
        r.createdByName || '',
        r.energySentKwh || 0,
        r.energySent15d || 0,
        r.energySent30d || 0,
        r.internalConsumptionKwh || 0,
        r.lossPct || 0
      ]) || [];
    }

    const worksheet = XLSX.utils.aoa_to_sheet([headers, ...rows]);
    // Set Sheet Direction to RTL if Arabic
    if (!worksheet['!views']) worksheet['!views'] = [];
    worksheet['!views'].push({ rightToLeft: lang === 'ar' });

    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, sheetName);
    
    // Generate file and trigger download
    const filename = `report-${type}-${reportDateFrom}.xlsx`;
    XLSX.writeFile(workbook, filename);
  };

  const formatReadingDateTime = (val) => {
    if (!val) return '—';
    const d = new Date(val);
    if (isNaN(d.getTime())) return '—';
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');
    return `${day}/${month}/${year} ${hours}:${minutes}`;
  };

  const getGeneratorFuelType = (row) => row?.fuelTypeSnapshot || row?.fuelType || '—';

  const getGeneratorConsumptionRate = (row) => (
    row?.energyProducedKwh > 0
      ? Number(((row.fuelConsumedLiters || 0) / row.energyProducedKwh).toFixed(4))
      : 0
  );

  const renderGeneratorsTable = () => (
    <GeneratorsReportTab
      data={data}
      lang={lang}
      genSubStation={genSubStation}
      setGenSubStation={setGenSubStation}
      subStations={subStations}
      getSubStationOptionLabel={getSubStationOptionLabel}
      fetchReport={fetchReport}
      formatReadingDateTime={formatReadingDateTime}
      getGeneratorFuelType={getGeneratorFuelType}
      getGeneratorConsumptionRate={getGeneratorConsumptionRate}
    />
  );

  const renderOilTable = () => (
    <OilReportTab
      data={data}
      lang={lang}
      genSubStation={genSubStation}
      setGenSubStation={setGenSubStation}
      subStations={subStations}
      getSubStationOptionLabel={getSubStationOptionLabel}
      fetchReport={fetchReport}
      formatReadingDateTime={formatReadingDateTime}
    />
  );

  const renderFuelTable = () => (
    <FuelReportTab
      data={data}
      lang={lang}
      genSubStation={genSubStation}
      setGenSubStation={setGenSubStation}
      subStations={subStations}
      getSubStationOptionLabel={getSubStationOptionLabel}
      fetchReport={fetchReport}
      formatReadingDateTime={formatReadingDateTime}
    />
  );

  const renderStationTable = () => (
    <StationReportTab
      data={data}
      lang={lang}
      genSubStation={genSubStation}
      setGenSubStation={setGenSubStation}
      subStations={subStations}
      getSubStationOptionLabel={getSubStationOptionLabel}
      fetchReport={fetchReport}
    />
  );

  const renderPagination = (pagination, setPageFn) => {
    if (!pagination || pagination.pages <= 1) return null;
    return (
      <div style={{ padding: '16px 22px', borderTop: '1px solid var(--border)' }}>
        <Pagination page={pagination.page} totalPages={pagination.pages}
          total={pagination.total} pageSize={50} lang={lang}
          onPageChange={setPageFn} />
      </div>
    );
  };

  // ── Filter bar shared for new report tabs ──
  const renderFilterBar = ({ dateFrom, setDateFrom, dateTo, setDateTo, searchVal, setSearchVal, stationVal, setStationVal, stationOptions, stationAllLabel, formatStationOption, extraFilters, setPageFn, onApply }) => (
    <div className="no-print" style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap', padding: '16px 22px', borderBottom: '1px solid var(--border)', background: 'hsla(220,40%,60%,0.02)' }}>
      <div className="date-picker-wrap" style={{ flexShrink: 0 }}>
        <CalendarIcon size={14} />
        <input type="date" value={dateFrom} onChange={e => { setDateFrom(e.target.value); if (setPageFn) setPageFn(1); }} style={{ width: 130 }} />
        <span style={{ color: 'var(--text-muted)', fontSize: 11 }}>—</span>
        <input type="date" value={dateTo} onChange={e => { setDateTo(e.target.value); if (setPageFn) setPageFn(1); }} style={{ width: 130 }} />
      </div>
      {stationOptions !== false && (
        <select className="form-select" value={stationVal} onChange={e => { setStationVal(e.target.value); if (setPageFn) setPageFn(1); }} style={{ width: 160, padding: '7px 10px', fontSize: 12 }}>
          <option value="">{stationAllLabel || (lang === 'ar' ? 'كل المواقع' : 'All Stations')}</option>
          {(stationOptions || stations).map(s => (
            <option key={s.id || s._id} value={s.id || s._id}>
              {formatStationOption ? formatStationOption(s) : s.name}
            </option>
          ))}
        </select>
      )}
      {extraFilters}
      <div className="search-bar" style={{ flex: 1, minWidth: 200, position: 'relative' }}>
        <Search size={14} style={{ position: 'absolute', left: lang === 'ar' ? 'auto' : 10, right: lang === 'ar' ? 10 : 'auto', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
        <input
          className="form-control"
          type="text"
          placeholder={lang === 'ar' ? 'بحث...' : 'Search...'}
          value={searchVal}
          onChange={e => setSearchVal(e.target.value)}
          style={{ paddingLeft: lang === 'ar' ? 14 : 32, paddingRight: lang === 'ar' ? 32 : 14, fontSize: 12 }}
        />
      </div>
      <button className="btn btn-sm btn-primary" onClick={onApply}>
        <Filter size={14} />
        <span>{lang === 'ar' ? 'تصفية' : 'Filter'}</span>
      </button>
    </div>
  );

  // ── Fuel Incoming Report Table ──
  const renderFuelIncomingTable = () => (
    <div>
      {renderFilterBar({
        dateFrom: fiDateFrom, setDateFrom: setFiDateFrom,
        dateTo: fiDateTo, setDateTo: setFiDateTo,
        searchVal: fiSearch, setSearchVal: setFiSearch,
        stationVal: fiStation, setStationVal: setFiStation,
        stationOptions: subStations,
        stationAllLabel: lang === 'ar' ? 'كل المواقع' : 'All Sites',
        formatStationOption: getSubStationOptionLabel,
        setPageFn: setFiPage,
        onApply: applyFiSearch,
        extraFilters: (
          <select className="form-select" value={fiFuelType} onChange={e => { setFiFuelType(e.target.value); setFiPage(1); }} style={{ width: 130, padding: '7px 10px', fontSize: 12 }}>
            <option value="">{lang === 'ar' ? 'كل الأنواع' : 'All Types'}</option>
            <option value="diesel">{lang === 'ar' ? 'ديزل' : 'Diesel'}</option>
            <option value="mazout">{lang === 'ar' ? 'مازوت' : 'Mazout'}</option>
            <option value="petrol">{lang === 'ar' ? 'بنزين' : 'Petrol'}</option>
          </select>
        ),
      })}
      <div className="table-container">
        <table className="report-table" style={{ minWidth: 1800 }}>
          <thead>
            <tr className="header-row-1">
              <th rowSpan="2">{lang === 'ar' ? 'الموقع' : 'Station'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'الخزان' : 'Tank'}</th>
              <th rowSpan="2" style={{ textAlign: 'center' }}>{lang === 'ar' ? 'نوع الوقود' : 'Fuel Type'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'المورد' : 'Supplier'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'رقم التوريد' : 'Supply No.'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'الكمية (لتر)' : 'Qty (L)'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'السواق' : 'Driver'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'بطاقة السواق' : 'Driver ID'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'رقم القاطرة' : 'Truck No.'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'رقم المقطورة' : 'Trailer No.'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'رقم الإيصال' : 'Receipt No.'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'كمية الإيصال' : 'Receipt Qty'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'تاريخ التوريد' : 'Supply Date'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'تاريخ الإيصال' : 'Receipt Date'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'العداد العياري' : 'Cal. Meter'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'العداد المشاهد' : 'Obs. Meter'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'الحالة' : 'Status'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'تاريخ الحركة' : 'Mov. Date'}</th>
              <th rowSpan="2">{lang === 'ar' ? 'بواسطة' : 'By'}</th>
            </tr>
          </thead>
          <tbody>
            {fiLoading ? (
              <tr><td colSpan="19"><div className="report-loader"><div className="ft-spinner" /><p>{lang === 'ar' ? 'جاري التحميل...' : 'Loading...'}</p></div></td></tr>
            ) : fuelIncoming.data.length === 0 ? (
              <tr><td colSpan="19" className="empty-msg">{lang === 'ar' ? 'لا توجد حركات وقود وارد' : 'No fuel incoming records'}</td></tr>
            ) : fuelIncoming.data.map((r, i) => (
              <tr key={r._id || i}>
                <td className="bold-cell">{getMovementSiteName(r) || '—'}</td>
                <td>{r.tank?.name || '—'}</td>
                <td style={{ textAlign: 'center' }}>
                  {r.tank?.fuelType ? (
                    <span style={{ background: '#f59e0b20', color: '#f59e0b', border: '1px solid #f59e0b40', borderRadius: 5, padding: '2px 8px', fontSize: 11, fontWeight: 700, whiteSpace: 'nowrap' }}>
                      {r.tank.fuelType}
                    </span>
                  ) : '—'}
                </td>
                <td>{r.supplierName || '—'}</td>
                <td>{r.supplyNumber || '—'}</td>
                <td className="positive-cell">{r.quantityLiters?.toLocaleString(undefined, { minimumFractionDigits: 3, maximumFractionDigits: 3 }) || '0'}</td>
                <td>{r.driverName || '—'}</td>
                <td>{r.driverId || '—'}</td>
                <td>{r.truckNumber || '—'}</td>
                <td>{r.trailerNumber || '—'}</td>
                <td>{r.receiptNumber || '—'}</td>
                <td>{r.receiptQuantity?.toLocaleString() || '—'}</td>
                <td style={{ fontSize: 11 }}>{r.supplyDate ? new Date(r.supplyDate).toLocaleDateString() : '—'}</td>
                <td style={{ fontSize: 11 }}>{r.receiptDate ? new Date(r.receiptDate).toLocaleDateString() : '—'}</td>
                <td>{r.calibrationReading?.toLocaleString() || '—'}</td>
                <td>{r.observedReading?.toLocaleString() || '—'}</td>
                <td>
                  <span className={`badge ${r.status === 'approved' ? 'badge-success' : r.status === 'rejected' ? 'badge-danger' : 'badge-warning'}`}>
                    {r.status === 'approved' ? (lang === 'ar' ? 'معتمد' : 'Approved')
                      : r.status === 'rejected' ? (lang === 'ar' ? 'مرفوض' : 'Rejected')
                      : (lang === 'ar' ? 'قيد الانتظار' : 'Pending')}
                  </span>
                </td>
                <td style={{ fontSize: 11 }}>{r.movementDate ? new Date(r.movementDate).toLocaleDateString() : '—'}</td>
                <td>{r.createdBy?.name || '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {renderPagination(fuelIncoming.pagination, setFiPage)}
    </div>
  );

  // ── Fuel Transfer Report Table ──
  const renderFuelTransferTable = () => (
    <div>
      {renderFilterBar({
        dateFrom: ftDateFrom, setDateFrom: setFtDateFrom,
        dateTo: ftDateTo, setDateTo: setFtDateTo,
        searchVal: ftSearch, setSearchVal: setFtSearch,
        stationVal: ftStation, setStationVal: setFtStation,
        stationOptions: subStations,
        stationAllLabel: lang === 'ar' ? 'كل المواقع' : 'All Sites',
        formatStationOption: getSubStationOptionLabel,
        setPageFn: setFtPage,
        onApply: applyFtSearch,
        extraFilters: null,
      })}
      <div className="table-container">
        <table className="report-table" style={{ minWidth: 1200 }}>
          <thead>
            <tr className="header-row-1">
              <th>{lang === 'ar' ? 'من خزان' : 'From Tank'}</th>
              <th style={{ textAlign: 'center' }}>{lang === 'ar' ? 'نوع الوقود' : 'Fuel Type'}</th>
              <th>{lang === 'ar' ? 'إلى خزان/فرع' : 'To Tank'}</th>
              <th>{lang === 'ar' ? 'رقم أمر الصرف' : 'Issue Order No.'}</th>
              <th>{lang === 'ar' ? 'تاريخ أمر الصرف' : 'Order Date'}</th>
              <th>{lang === 'ar' ? 'الموقع المصدر' : 'Source Station'}</th>
              <th>{lang === 'ar' ? 'الموقع الوجهة' : 'Target Station'}</th>
              <th>{lang === 'ar' ? 'الكمية (لتر)' : 'Qty (L)'}</th>
              <th>{lang === 'ar' ? 'الملاحظات' : 'Notes'}</th>
              <th>{lang === 'ar' ? 'الحالة' : 'Status'}</th>
              <th>{lang === 'ar' ? 'تاريخ الحركة' : 'Mov. Date'}</th>
              <th>{lang === 'ar' ? 'بواسطة' : 'By'}</th>
            </tr>
          </thead>
          <tbody>
            {ftLoading ? (
              <tr><td colSpan="12"><div className="report-loader"><div className="ft-spinner" /><p>{lang === 'ar' ? 'جاري التحميل...' : 'Loading...'}</p></div></td></tr>
            ) : fuelTransfer.data.length === 0 ? (
              <tr><td colSpan="12" className="empty-msg">{lang === 'ar' ? 'لا توجد حركات نقل وقود' : 'No fuel transfer records'}</td></tr>
            ) : fuelTransfer.data.map((r, i) => (
              <tr key={r._id || i}>
                <td className="bold-cell" style={{ color: 'var(--secondary)' }}>{r.tank?.name || '—'}</td>
                <td style={{ textAlign: 'center' }}>
                  {r.tank?.fuelType ? (
                    <span style={{ background: '#f59e0b20', color: '#f59e0b', border: '1px solid #f59e0b40', borderRadius: 5, padding: '2px 8px', fontSize: 11, fontWeight: 700, whiteSpace: 'nowrap' }}>
                      {r.tank.fuelType}
                    </span>
                  ) : '—'}
                </td>
                <td className="bold-cell" style={{ color: 'var(--primary)' }}>{r.targetTank?.name || '—'}</td>
                <td style={{ fontWeight: 600 }}>{r.issueOrderNumber || '—'}</td>
                <td style={{ fontSize: 11 }}>{r.issueOrderDate ? new Date(r.issueOrderDate).toLocaleDateString() : '—'}</td>
                <td>{r.station?.name || '—'}</td>
                <td>{r.targetStation?.name || '—'}</td>
                <td className="positive-cell">{(r.quantityLiters || 0).toLocaleString(undefined, { minimumFractionDigits: 3, maximumFractionDigits: 3 })}</td>
                <td style={{ fontSize: 12, color: 'var(--text-muted)', maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{r.notes || '—'}</td>
                <td>
                  <span className={`badge ${r.status === 'approved' ? 'badge-success' : r.status === 'rejected' ? 'badge-danger' : 'badge-warning'}`}>
                    {r.status === 'approved' ? (lang === 'ar' ? 'معتمد' : 'Approved')
                      : r.status === 'rejected' ? (lang === 'ar' ? 'مرفوض' : 'Rejected')
                      : (lang === 'ar' ? 'قيد الانتظار' : 'Pending')}
                  </span>
                </td>
                <td style={{ fontSize: 11 }}>{r.movementDate ? new Date(r.movementDate).toLocaleDateString() : '—'}</td>
                <td>{r.createdBy?.name || '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {renderPagination(fuelTransfer.pagination, setFtPage)}
    </div>
  );

  const renderFuelLossTable = () => (
    <div>
      {renderFilterBar({
        dateFrom: flDateFrom, setDateFrom: setFlDateFrom,
        dateTo: flDateTo, setDateTo: setFlDateTo,
        searchVal: flSearch, setSearchVal: setFlSearch,
        stationVal: flSubStation, setStationVal: setFlSubStation,
        stationOptions: subStations,
        stationAllLabel: lang === 'ar' ? 'كل المواقع' : 'All Sites',
        formatStationOption: getSubStationOptionLabel,
        setPageFn: setFlPage,
        onApply: applyFlSearch,
        extraFilters: (
          <select className="form-select" value={flItemType} onChange={e => { setFlItemType(e.target.value); setFlPage(1); }} style={{ width: 135, padding: '7px 10px', fontSize: 12 }}>
            <option value="">{lang === 'ar' ? 'وقود وزيوت' : 'Fuel & Oil'}</option>
            <option value="fuel">{lang === 'ar' ? 'وقود فقط' : 'Fuel Only'}</option>
            <option value="oil">{lang === 'ar' ? 'زيوت فقط' : 'Oil Only'}</option>
          </select>
        ),
      })}
      <div className="table-container">
        <table className="report-table" style={{ minWidth: 1250 }}>
          <thead>
            <tr className="header-row-1">
              <th>{lang === 'ar' ? 'نوع المادة' : 'Item Type'}</th>
              <th>{lang === 'ar' ? 'الموقع' : 'Station'}</th>
              <th>{lang === 'ar' ? 'الخزان/الزيت' : 'Tank/Oil'}</th>
              <th>{lang === 'ar' ? 'الكمية' : 'Quantity'}</th>
              <th>{lang === 'ar' ? 'السبب/التفاصيل' : 'Reason'}</th>
              <th>{lang === 'ar' ? 'ملاحظات إضافية' : 'Notes'}</th>
              <th>{lang === 'ar' ? 'الحالة' : 'Status'}</th>
              <th>{lang === 'ar' ? 'تاريخ الحركة' : 'Mov. Date'}</th>
              <th>{lang === 'ar' ? 'أدخل بواسطة' : 'Created By'}</th>
              <th>{lang === 'ar' ? 'اعتمد بواسطة' : 'Approved By'}</th>
            </tr>
          </thead>
          <tbody>
            {flLoading ? (
              <tr><td colSpan="10"><div className="report-loader"><div className="ft-spinner" /><p>{lang === 'ar' ? 'جاري التحميل...' : 'Loading...'}</p></div></td></tr>
            ) : fuelLoss.data.length === 0 ? (
              <tr><td colSpan="10" className="empty-msg">{lang === 'ar' ? 'لا توجد حركات فاقد معتمدة' : 'No approved loss records'}</td></tr>
            ) : fuelLoss.data.map((r, i) => (
              <tr key={r._id || r.id || i}>
                <td>
                  <span className={`badge ${r.itemType === 'oil' ? 'badge-warning' : 'badge-muted'}`}>
                    {r.itemType === 'oil' ? (lang === 'ar' ? 'زيوت' : 'Oil') : (lang === 'ar' ? 'وقود' : 'Fuel')}
                  </span>
                </td>
                <td className="bold-cell">{getLossSiteName(r) || '—'}</td>
                <td className="bold-cell">{r.itemType === 'oil' ? (r.oil?.name || '—') : (r.tank?.name || '—')}</td>
                <td className="negative-cell">{(r.quantityLiters || 0).toLocaleString(undefined, { minimumFractionDigits: 3, maximumFractionDigits: 3 })}</td>
                <td style={{ textAlign: 'center', maxWidth: 260, whiteSpace: 'normal' }}>{r.reason || '—'}</td>
                <td style={{ textAlign: 'center', maxWidth: 260, whiteSpace: 'normal' }}>{r.notes || '—'}</td>
                <td><span className="badge badge-success">{lang === 'ar' ? 'معتمد' : 'Approved'}</span></td>
                <td style={{ fontSize: 11 }}>{r.movementDate ? new Date(r.movementDate).toLocaleDateString() : '—'}</td>
                <td>{r.createdBy?.name || '—'}</td>
                <td>{r.approvedBy?.name || '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {renderPagination(fuelLoss.pagination, setFlPage)}
    </div>
  );

  const formatPeriodicNumber = (value, digits = 0) => {
    const numeric = Number(value);
    if (!Number.isFinite(numeric)) return '0';
    return numeric.toLocaleString(undefined, { minimumFractionDigits: digits, maximumFractionDigits: digits });
  };

  const formatPeriodicNullableNumber = (value, digits = 4) => {
    const numeric = Number(value);
    if (!Number.isFinite(numeric)) return '—';
    return numeric.toLocaleString(undefined, { minimumFractionDigits: digits, maximumFractionDigits: digits });
  };

  const getEfficiencyClass = (value) => {
    const numeric = Number(value);
    if (!Number.isFinite(numeric)) return '';
    if (numeric >= 85) return 'efficiency-high';
    if (numeric < 70) return 'efficiency-low';
    return 'efficiency-mid';
  };

  const renderPeriodicGenerationTable = () => {
    const rows = periodicData?.siteGenerationReport || [];
    const totals = rows.reduce((acc, row) => ({
      generatorCount: acc.generatorCount + (Number(row.generatorCount) || 0),
      ratedCapacityKw: acc.ratedCapacityKw + (Number(row.ratedCapacityKw) || 0),
      actualCapacityKw: acc.actualCapacityKw + (Number(row.actualCapacityKw) || 0),
      energyProducedKwh: acc.energyProducedKwh + (Number(row.energyProducedKwh) || 0),
      operatingHours: acc.operatingHours + (Number(row.operatingHours) || 0),
      energySentKwh: acc.energySentKwh + (Number(row.energySentKwh) || 0),
      internalConsumption: acc.internalConsumption + (Number(row.internalConsumption) || 0),
    }), { generatorCount: 0, ratedCapacityKw: 0, actualCapacityKw: 0, energyProducedKwh: 0, operatingHours: 0, energySentKwh: 0, internalConsumption: 0 });
    const totalAverage = totals.operatingHours > 0 ? totals.energyProducedKwh / totals.operatingHours : null;

    return (
      <div>
        <div className="report-subtitle">{lang === 'ar' ? 'تقرير موقع التوليد الدوري' : 'Periodic Generation Site Report'}</div>
        <div className="table-container">
          <table className="report-table" style={{ minWidth: 1250 }}>
            <thead>
              <tr className="header-row-1">
                <th>{lang === 'ar' ? 'الموقع / الفرع' : 'Site / Branch'}</th>
                <th>{lang === 'ar' ? 'عدد المولدات' : 'Generators'}</th>
                <th>{lang === 'ar' ? 'القدرة الاسمية' : 'Rated Capacity'}</th>
                <th>{lang === 'ar' ? 'القدرة الفعلية (ك.و)' : 'Actual Capacity (KW)'}</th>
                <th>{lang === 'ar' ? 'الطاقة المنتجة (ك.و.س)' : 'Energy Produced (KWh)'}</th>
                <th>{lang === 'ar' ? 'ساعات التشغيل (س)' : 'Operating Hours (h)'}</th>
                <th>{lang === 'ar' ? 'الطاقة المرسلة (ك.و.س)' : 'Sent Energy (KWh)'}</th>
                <th>{lang === 'ar' ? 'الطاقة المستهلكة داخلياً (ك.و.س)' : 'Internal Consumption (KWh)'}</th>
                <th>{lang === 'ar' ? 'متوسط التوليد (ك.و/س)' : 'Generation Average (KW/h)'}</th>
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 ? (
                <tr><td colSpan="9" className="empty-msg">{lang === 'ar' ? 'لا توجد بيانات جرد دوري معتمدة لهذه الفترة' : 'No approved periodic inventory data for this period'}</td></tr>
              ) : rows.map((row, index) => (
                <tr key={`${row.siteLabel}-${index}`}>
                  <td className="bold-cell">{row.siteLabel || '—'}</td>
                  <td>{formatPeriodicNumber(row.generatorCount)}</td>
                  <td>{formatPeriodicNumber(row.ratedCapacityKw, 3)}</td>
                  <td>{formatPeriodicNumber(row.actualCapacityKw, 3)}</td>
                  <td>{formatPeriodicNumber(row.energyProducedKwh, 3)}</td>
                  <td>{formatPeriodicNumber(row.operatingHours, 3)}</td>
                  <td>{formatPeriodicNumber(row.energySentKwh, 3)}</td>
                  <td>{formatPeriodicNumber(row.internalConsumption, 3)}</td>
                  <td className="rate-cell">{formatPeriodicNullableNumber(row.generationAverage, 3)}</td>
                </tr>
              ))}
              {rows.length > 0 && (
                <tr className="total-row">
                  <td className="total-row-label">{lang === 'ar' ? 'إجمالي' : 'Total'}</td>
                  <td>{formatPeriodicNumber(totals.generatorCount)}</td>
                  <td>{formatPeriodicNumber(totals.ratedCapacityKw, 3)}</td>
                  <td>{formatPeriodicNumber(totals.actualCapacityKw, 3)}</td>
                  <td>{formatPeriodicNumber(totals.energyProducedKwh, 3)}</td>
                  <td>{formatPeriodicNumber(totals.operatingHours, 3)}</td>
                  <td>{formatPeriodicNumber(totals.energySentKwh, 3)}</td>
                  <td>{formatPeriodicNumber(totals.internalConsumption, 3)}</td>
                  <td className="rate-cell">{formatPeriodicNullableNumber(totalAverage, 3)}</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    );
  };

  const renderPeriodicFuelTable = () => {
    const rows = periodicData?.fuelReport || [];
    const totals = rows.reduce((acc, row) => {
      const rate = Number(row.actualConsumptionRate);
      const efficiency = Number(row.efficiency);
      return {
        openingFuel: acc.openingFuel + (Number(row.openingFuel) || 0),
        incomingFuel: acc.incomingFuel + (Number(row.incomingFuel) || 0),
        outgoingFuel: acc.outgoingFuel + (Number(row.outgoingFuel) || 0),
        lostFuel: acc.lostFuel + (Number(row.lostFuel) || 0),
        closingFuelActual: acc.closingFuelActual + (Number(row.closingFuelActual) || 0),
        actualConsumedFuel: acc.actualConsumedFuel + (Number(row.actualConsumedFuel) || 0),
        rateSum: acc.rateSum + (Number.isFinite(rate) ? rate : 0),
        rateCount: acc.rateCount + (Number.isFinite(rate) ? 1 : 0),
        efficiencySum: acc.efficiencySum + (Number.isFinite(efficiency) ? efficiency : 0),
        efficiencyCount: acc.efficiencyCount + (Number.isFinite(efficiency) ? 1 : 0),
      };
    }, { openingFuel: 0, incomingFuel: 0, outgoingFuel: 0, lostFuel: 0, closingFuelActual: 0, actualConsumedFuel: 0, rateSum: 0, rateCount: 0, efficiencySum: 0, efficiencyCount: 0 });
    const averageRate = totals.rateCount > 0 ? totals.rateSum / totals.rateCount : null;
    const averageEfficiency = totals.efficiencyCount > 0 ? totals.efficiencySum / totals.efficiencyCount : null;

    return (
      <div>
        <div className="report-subtitle">{lang === 'ar' ? 'التقرير التفصيلي الدوري للوقود' : 'Periodic Detailed Fuel Report'}</div>
        <div className="table-container">
          <table className="report-table" style={{ minWidth: 1450 }}>
            <thead>
              <tr className="header-row-1">
                <th>{lang === 'ar' ? 'الموقع / الفرع' : 'Site / Branch'}</th>
                <th style={{ textAlign: 'center' }}>{lang === 'ar' ? 'نوع الوقود' : 'Fuel Type'}</th>
                <th>{lang === 'ar' ? 'مخزون الوقود أول الفترة (لتر)' : 'Opening Fuel (L)'}</th>
                <th>{lang === 'ar' ? 'الوقود الوارد (لتر)' : 'Incoming Fuel (L)'}</th>
                <th>{lang === 'ar' ? 'الوقود الصادر (لتر)' : 'Outgoing Fuel (L)'}</th>
                <th>{lang === 'ar' ? 'الفاقد (لتر)' : 'Lost Fuel (L)'}</th>
                <th>{lang === 'ar' ? 'المخزون الفعلي نهاية الفترة (لتر)' : 'Closing Actual Stock (L)'}</th>
                <th>{lang === 'ar' ? 'الوقود المستهلك الفعلي (لتر)' : 'Actual Consumed Fuel (L)'}</th>
                <th>{lang === 'ar' ? 'معدل الاستهلاك الفعلي (لتر/ك.و.س)' : 'Actual Consumption Rate (L/KWh)'}</th>
                <th>{lang === 'ar' ? 'الكفاءة (%)' : 'Efficiency (%)'}</th>
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 ? (
                <tr><td colSpan="9" className="empty-msg">{lang === 'ar' ? 'لا توجد بيانات وقود دورية لهذه الفترة' : 'No periodic fuel data for this period'}</td></tr>
              ) : rows.map((row, index) => {
                const typeTrans = row.fuelType ? row.fuelType.split(', ').map(t => {
                  if (t.toLowerCase() === 'diesel') return lang === 'ar' ? 'ديزل' : 'Diesel';
                  if (t.toLowerCase() === 'mazout') return lang === 'ar' ? 'مازوت' : 'Mazout';
                  if (t.toLowerCase() === 'petrol') return lang === 'ar' ? 'بنزين' : 'Petrol';
                  if (t.toLowerCase() === 'kerosene') return lang === 'ar' ? 'كيروسين' : 'Kerosene';
                  return t;
                }).join(', ') : (lang === 'ar' ? 'ديزل' : 'Diesel');
                return (
                  <tr key={`${row.siteLabel}-${index}`}>
                    <td className="bold-cell">{row.siteLabel || '—'}</td>
                    <td style={{ textAlign: 'center' }}>
                      <span style={{
                        background: '#f59e0b20', color: '#f59e0b',
                        border: '1px solid #f59e0b40', borderRadius: 5,
                        padding: '2px 8px', fontSize: 11, fontWeight: 700,
                        whiteSpace: 'nowrap'
                      }}>
                        {typeTrans}
                      </span>
                    </td>
                    <td>{formatPeriodicNumber(row.openingFuel, 3)}</td>
                  <td className="positive-cell">{formatPeriodicNumber(row.incomingFuel, 3)}</td>
                  <td>{formatPeriodicNumber(row.outgoingFuel, 3)}</td>
                  <td className="negative-cell">{formatPeriodicNumber(row.lostFuel, 3)}</td>
                  <td className="bold-cell">{formatPeriodicNumber(row.closingFuelActual, 3)}</td>
                  <td className={Number(row.actualConsumedFuel) < 0 ? 'negative-cell' : 'positive-cell'}>{formatPeriodicNumber(row.actualConsumedFuel, 3)}</td>
                  <td className="rate-cell">{formatPeriodicNullableNumber(row.actualConsumptionRate, 4)}</td>
                  <td className={getEfficiencyClass(row.efficiency)}>{row.efficiency === null || row.efficiency === undefined ? '—' : `${formatPeriodicNumber(row.efficiency, 2)}%`}</td>
                </tr>
              );
            })}
              {rows.length > 0 && (
                <tr className="total-row">
                  <td className="total-row-label" colSpan="2">{lang === 'ar' ? 'إجمالي' : 'Total'}</td>
                  <td>{formatPeriodicNumber(totals.openingFuel, 3)}</td>
                  <td>{formatPeriodicNumber(totals.incomingFuel, 3)}</td>
                  <td>{formatPeriodicNumber(totals.outgoingFuel, 3)}</td>
                  <td>{formatPeriodicNumber(totals.lostFuel, 3)}</td>
                  <td>{formatPeriodicNumber(totals.closingFuelActual, 3)}</td>
                  <td>{formatPeriodicNumber(totals.actualConsumedFuel, 3)}</td>
                  <td className="rate-cell">{formatPeriodicNullableNumber(averageRate, 4)}</td>
                  <td className={getEfficiencyClass(averageEfficiency)}>{averageEfficiency === null ? '—' : `${formatPeriodicNumber(averageEfficiency, 2)}%`}</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    );
  };

  const renderPeriodicOilTable = () => {
    const rows = periodicData?.oilReport || [];
    const totals = rows.reduce((acc, row) => {
      const rate = Number(row.consumptionRate);
      return {
        openingOil: acc.openingOil + (Number(row.openingOil) || 0),
        incomingOil: acc.incomingOil + (Number(row.incomingOil) || 0),
        outgoingOil: acc.outgoingOil + (Number(row.outgoingOil) || 0),
        lossOil: acc.lossOil + (Number(row.lossOil) || 0),
        closingOil: acc.closingOil + (Number(row.closingOil) || 0),
        consumedOil: acc.consumedOil + (Number(row.consumedOil) || 0),
        rateSum: acc.rateSum + (Number.isFinite(rate) ? rate : 0),
        rateCount: acc.rateCount + (Number.isFinite(rate) ? 1 : 0),
      };
    }, { openingOil: 0, incomingOil: 0, outgoingOil: 0, lossOil: 0, closingOil: 0, consumedOil: 0, rateSum: 0, rateCount: 0 });
    const averageRate = totals.rateCount > 0 ? totals.rateSum / totals.rateCount : null;

    return (
      <div>
        <div className="report-subtitle">{lang === 'ar' ? 'التقرير التفصيلي الدوري لمخزون الزيت' : 'Periodic Detailed Oil Stock Report'}</div>
        <div className="table-container">
          <table className="report-table" style={{ minWidth: 1450 }}>
            <thead>
              <tr className="header-row-1">
                <th>{lang === 'ar' ? 'الموقع / الفرع' : 'Site / Branch'}</th>
                <th>{lang === 'ar' ? 'نوع الزيت' : 'Oil Type'}</th>
                <th>{lang === 'ar' ? 'مخزون الزيت أول الفترة (لتر)' : 'Opening Oil (L)'}</th>
                <th>{lang === 'ar' ? 'الزيت الوارد (لتر)' : 'Incoming Oil (L)'}</th>
                <th>{lang === 'ar' ? 'الزيت الصادر (لتر)' : 'Outgoing Oil (L)'}</th>
                <th>{lang === 'ar' ? 'الفاقد من الزيت (لتر)' : 'Lost Oil (L)'}</th>
                <th>{lang === 'ar' ? 'مخزون الزيت الفعلي نهاية الفترة (لتر)' : 'Closing Actual Stock (L)'}</th>
                <th>{lang === 'ar' ? 'الزيت المستهلك الفعلي (لتر)' : 'Actual Consumed Oil (L)'}</th>
                <th>{lang === 'ar' ? 'معدل استهلاك الزيت (لتر/س)' : 'Oil Consumption Rate (L/h)'}</th>
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 ? (
                <tr><td colSpan="9" className="empty-msg">{lang === 'ar' ? 'لا توجد بيانات زيوت دورية لهذه الفترة' : 'No periodic oil data for this period'}</td></tr>
              ) : rows.map((row, index) => (
                <tr key={`${row.siteLabel}-${row.oilName}-${index}`}>
                  <td className="bold-cell">{row.siteLabel || '—'}</td>
                  <td><span className="oil-type-cell">{row.oilName || '—'}</span></td>
                  <td>{formatPeriodicNumber(row.openingOil, 3)}</td>
                  <td className="positive-cell">{formatPeriodicNumber(row.incomingOil, 3)}</td>
                  <td>{formatPeriodicNumber(row.outgoingOil, 3)}</td>
                  <td className="negative-cell">{formatPeriodicNumber(row.lossOil, 3)}</td>
                  <td className="bold-cell">{formatPeriodicNumber(row.closingOil, 3)}</td>
                  <td className={Number(row.consumedOil) < 0 ? 'negative-cell' : 'positive-cell'}>{formatPeriodicNumber(row.consumedOil, 3)}</td>
                  <td className="rate-cell">{formatPeriodicNullableNumber(row.consumptionRate, 4)}</td>
                </tr>
              ))}
              {rows.length > 0 && (
                <tr className="total-row">
                  <td className="total-row-label" colSpan="2">{lang === 'ar' ? 'إجمالي' : 'Total'}</td>
                  <td>{formatPeriodicNumber(totals.openingOil, 3)}</td>
                  <td>{formatPeriodicNumber(totals.incomingOil, 3)}</td>
                  <td>{formatPeriodicNumber(totals.outgoingOil, 3)}</td>
                  <td>{formatPeriodicNumber(totals.lossOil, 3)}</td>
                  <td>{formatPeriodicNumber(totals.closingOil, 3)}</td>
                  <td>{formatPeriodicNumber(totals.consumedOil, 3)}</td>
                  <td className="rate-cell">{formatPeriodicNullableNumber(averageRate, 4)}</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    );
  };

  return (
    <div className="reports-page">
      <div className="page-header">
        <div className="header-main">
          <div className="title-section">
            <div className="icon-box"><FileText size={24} /></div>
            <div>
              <h1>{lang === 'ar' ? 'التقارير التفصيلية' : 'Detailed Reports'}</h1>
              <p>{lang === 'ar' ? 'عرض وتقارير الحالة اليومية للمولدات والوقود والزيوت' : 'View daily status for generators, fuel, and oils'}</p>
            </div>
          </div>
          <div className="controls-section">
            <div className="date-picker-wrap">
              <label className="date-field">
                <span>{lang === 'ar' ? 'من' : 'From'}</span>
                <div className="date-input-shell">
                  <input
                    type="date"
                    value={reportDateFrom}
                    onChange={(e) => setReportDateFrom(e.target.value)}
                    dir="ltr"
                  />
                  <CalendarIcon size={16} />
                </div>
              </label>
              <label className="date-field">
                <span>{lang === 'ar' ? 'إلى' : 'To'}</span>
                <div className="date-input-shell">
                  <input
                    type="date"
                    value={reportDateTo}
                    onChange={(e) => setReportDateTo(e.target.value)}
                    dir="ltr"
                  />
                  <CalendarIcon size={16} />
                </div>
              </label>
              <CalendarIcon size={16} />
              <input type="date" value={reportDateFrom} onChange={(e) => setReportDateFrom(e.target.value)} style={{ width: 130 }} />
              <span style={{ color: 'var(--text-muted)', fontSize: 11 }}>—</span>
              <input type="date" value={reportDateTo} onChange={(e) => setReportDateTo(e.target.value)} style={{ width: 130 }} />
            </div>
            {canExport && (() => { const pdf = getPdfData(); return pdf.headers.length > 0 && (
              <>
                <button className="btn btn-primary" onClick={() => exportExcel(activeTab)}>
                  <FileSpreadsheet size={16} />
                  <span>{lang === 'ar' ? 'تصدير Excel' : 'Export Excel'}</span>
                </button>
                <PdfExportButton
                  headers={pdf.headers}
                  rows={pdf.rows}
                  title={pdf.title}
                  fileName={`report-${activeTab}-${reportDateFrom}.pdf`}
                  lang={lang}
                />
              </>
            ); })()}
            {canPrint && (
              <button className="btn btn-ghost" onClick={() => {
                const appRoot = document.querySelector('.app-root');
                const isLight = appRoot?.classList.contains('theme-light');
                if (isLight) appRoot.classList.remove('theme-light');
                setTimeout(() => { window.print(); if (isLight) appRoot.classList.add('theme-light'); }, 100);
              }}>
                <Printer size={16} />
              </button>
            )}
          </div>
        </div>

        <div className="tabs-navigation">
          {allowedReportTabs.map(({ key, icon: Icon, ar, en }) => (
            <button key={key} className={`tab-item ${activeTab === key ? 'active' : ''}`} onClick={() => setActiveTab(key)}>
              <Icon size={18} />
              <span>{lang === 'ar' ? ar : en}</span>
            </button>
          ))}
        </div>
      </div>

      <div className="report-content">
        {allowedReportTabs.length === 0 ? (
          <div className="report-error">
            <p>{lang === 'ar' ? 'لا توجد صلاحية لأي تبويب تقرير' : 'No report tabs are allowed for this role'}</p>
          </div>
        ) : loading ? (
          <div className="report-loader">
            <div className="ft-spinner" />
            <p>{lang === 'ar' ? 'جاري استخراج البيانات...' : 'Extracting data...'}</p>
          </div>
        ) : error ? (
          <div className="report-error"><p>{error}</p></div>
        ) : (
          <div className="glass-card report-card animate-in">
            {/* ── Print Header (Hidden on Screen) ── */}
            <div className="print-only print-header">
              <div className="print-logo" style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-start', gap: '15px' }}>
                <img src="/pec-logo.png" alt="Logo" onError={(e) => e.target.style.display='none'} style={{ width: 80, height: 80, objectFit: 'contain' }} />
                <div style={{ textAlign: 'center' }}>
                  <div style={{ fontSize: '16px', fontWeight: '900', color: '#000', lineHeight: 1.2, whiteSpace: 'nowrap' }}>Fuel & Energy Tracking</div>
                  <div style={{ fontSize: '12px', color: '#555', fontWeight: '600', textAlign: 'center' }}>نظام تتبع الوقود والطاقة</div>
                </div>
              </div>
              <div className="print-title">
                <div style={{ fontSize: '16px', fontWeight: 'bold', color: '#000', marginBottom: '8px' }}>
                  {activeTab === 'generators' ? (lang === 'ar' ? 'تقرير المولدات' : 'Generators Report') :
                   activeTab === 'oil' ? (lang === 'ar' ? 'تقرير مخزون الزيوت' : 'Oil Inventory Report') :
                   activeTab === 'fuel' ? (lang === 'ar' ? 'تقرير مخزون الوقود (ديزل)' : 'Fuel Inventory Report (Diesel)') :
                   activeTab === 'station' ? (lang === 'ar' ? 'تقرير قراءات الموقع' : 'Station Readings Report') : 
                   activeTab === 'fuel-incoming' ? (lang === 'ar' ? 'تقرير الوقود الوارد' : 'Fuel Incoming Report') :
                   activeTab === 'fuel-transfer' ? (lang === 'ar' ? 'تقرير نقل الوقود' : 'Fuel Transfer Report') : 
                   activeTab === 'fuel-loss' ? (lang === 'ar' ? 'تقرير فاقد الوقود والزيوت' : 'Fuel and Oil Loss Report') :
                   (lang === 'ar' ? 'التقارير التفصيلية' : 'Detailed Reports')}
                </div>
                <div style={{ fontSize: '13px', fontWeight: 'bold', color: '#000' }}>
                  {formatPrintRange()}
                </div>
              </div>
              <div style={{ textAlign: 'left' }}></div>
            </div>

            {activeTab === 'generators' && renderGeneratorsTable()}
            {activeTab === 'oil'        && renderOilTable()}
            {activeTab === 'fuel'       && renderFuelTable()}
            {activeTab === 'station'    && renderStationTable()}
            {activeTab === 'fuel-incoming' && renderFuelIncomingTable()}
            {activeTab === 'fuel-transfer' && renderFuelTransferTable()}
            {activeTab === 'fuel-loss' && renderFuelLossTable()}

            {/* ── Print Footer (Hidden on Screen) ── */}
            <div className="print-only print-footer">
              <div className="print-signatures">
                <div className="signature-box">
                  <p>{lang === 'ar' ? 'توقيع المختص' : 'Specialist Signature'}</p>
                  <div className="line"></div>
                </div>
                <div className="signature-box">
                  <p>{lang === 'ar' ? 'توقيع المدير' : 'Manager Signature'}</p>
                  <div className="line"></div>
                </div>
              </div>
              <div className="print-footer-info">
                <span>{lang === 'ar' ? 'تاريخ ووقت الطباعة:' : 'Print Date:'} {new Date().toLocaleString(lang === 'ar' ? 'ar-EG' : 'en-US')}</span>
                <span>{lang === 'ar' ? 'بواسطة المستخدم:' : 'Printed by:'} {currentUser?.name || 'Unknown'}</span>
              </div>
            </div>
          </div>
        )}
      </div>

      {canAccessPeriodicReports && (
        <div className="periodic-reports-section">
          <div className="periodic-section-header">
            <div className="periodic-title-row">
              <div className="icon-box" style={{ background: 'var(--warning-light, rgba(245,158,11,0.12))', color: '#f59e0b' }}>
                <CalendarIcon size={24} />
              </div>
              <div>
                <h2>{lang === 'ar' ? 'التقارير الدورية' : 'Periodic Reports'}</h2>
                <p>
                  {lang === 'ar'
                    ? 'تقارير الفترة (نصف شهرية أو شهرية) المبنية على بيانات الجرد الدوري المعتمد'
                    : 'Period-based reports (semi-monthly or monthly) built from approved periodic inventory data'}
                </p>
              </div>
            </div>

            <div className="periodic-filter-bar no-print">
              <div className="date-picker-wrap">
                <label className="date-field">
                  <span>{lang === 'ar' ? 'من' : 'From'}</span>
                  <div className="date-input-shell">
                    <input
                      type="date"
                      value={periodicDateFrom}
                      onChange={e => { setPeriodicDateFrom(e.target.value); setPeriodicData(null); }}
                      dir="ltr"
                    />
                    <CalendarIcon size={16} />
                  </div>
                </label>
                <label className="date-field">
                  <span>{lang === 'ar' ? 'إلى' : 'To'}</span>
                  <div className="date-input-shell">
                    <input
                      type="date"
                      value={periodicDateTo}
                      onChange={e => { setPeriodicDateTo(e.target.value); setPeriodicData(null); }}
                      dir="ltr"
                    />
                    <CalendarIcon size={16} />
                  </div>
                </label>
              </div>

              <select
                className="form-select"
                value={periodicStation}
                onChange={e => { setPeriodicStation(e.target.value); setPeriodicData(null); }}
                style={{ width: 200, padding: '7px 10px', fontSize: 12 }}
              >
                <option value="">{lang === 'ar' ? 'كل المواقع / الفروع' : 'All Sites'}</option>
                {subStations.map(s => (
                  <option key={s.id || s._id} value={s.id || s._id}>
                    {getSubStationOptionLabel(s)}
                  </option>
                ))}
              </select>

              <button
                className="btn btn-primary"
                onClick={fetchPeriodicReport}
                disabled={periodicLoading}
                style={{ background: '#f59e0b', borderColor: '#f59e0b' }}
              >
                {periodicLoading ? (
                  <>
                    <span className="ft-spinner" style={{ width: 14, height: 14 }} />
                    <span>{lang === 'ar' ? 'جاري التحميل...' : 'Loading...'}</span>
                  </>
                ) : (
                  <>
                    <Filter size={14} />
                    <span>{lang === 'ar' ? 'عرض التقارير' : 'Show Reports'}</span>
                  </>
                )}
              </button>
            </div>
          </div>

          {periodicError && (
            <div className="report-error" style={{ margin: '12px 0' }}>
              <p>{periodicError}</p>
            </div>
          )}

          {!periodicData && periodicLoading && (
            <div className="periodic-empty-state">
              <div className="ft-spinner" />
              <p>{lang === 'ar' ? 'جاري تحميل التقارير الدورية...' : 'Loading periodic reports...'}</p>
            </div>
          )}

          {!periodicData && !periodicLoading && !periodicError && (
            <div className="periodic-empty-state">
              <CalendarIcon size={40} style={{ color: 'var(--text-muted)', opacity: 0.4 }} />
              <p>
                {lang === 'ar'
                  ? 'حدد نطاق الفترة ثم اضغط "عرض التقارير" لاستخراج البيانات'
                  : 'Select a date range then click "Show Reports" to load data'}
              </p>
            </div>
          )}

          {periodicData && (
            <div className="glass-card report-card animate-in" style={{ marginTop: 0 }}>

              {/* Print header — shown only during periodic window.print() */}
              <div className="print-only periodic-print-header">
                <div className="print-logo" style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-start', gap: '15px' }}>
                  <img src="/pec-logo.png" alt="Logo" onError={(e) => { e.target.style.display='none'; }} style={{ width: 80, height: 80, objectFit: 'contain' }} />
                  <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '16px', fontWeight: '900', color: '#000', lineHeight: 1.2, whiteSpace: 'nowrap' }}>Fuel & Energy Tracking</div>
                    <div style={{ fontSize: '12px', color: '#555', fontWeight: '600', textAlign: 'center' }}>{lang === 'ar' ? 'نظام تتبع الوقود والطاقة' : 'Fuel & Energy Tracking'}</div>
                  </div>
                </div>
                <div className="print-title">
                  <div style={{ fontSize: '16px', fontWeight: 'bold', color: '#000', marginBottom: '8px' }}>
                    {lang === 'ar' ? 'التقارير الدورية' : 'Periodic Reports'}
                  </div>
                  <div style={{ fontSize: '13px', fontWeight: 'bold', color: '#000' }}>
                    {lang === 'ar' ? 'الفترة:' : 'Period:'} {periodicDateFrom} — {periodicDateTo}
                  </div>
                </div>
                <div style={{ textAlign: 'left' }}></div>
              </div>

              <div className="periodic-sub-tabs no-print">
              {[
                  { key: 'generation', rvKey: 'periodic_generation', ar: 'تقرير موقع التوليد', en: 'Generation Report', icon: Zap },
                  { key: 'fuel',       rvKey: 'periodic_fuel',       ar: 'تقرير الوقود',       en: 'Fuel Report',       icon: Droplet },
                  { key: 'oil',        rvKey: 'periodic_oil',        ar: 'تقرير الزيوت',       en: 'Oil Report',        icon: Droplet },
                ]
                  .filter(t => rv[t.rvKey] !== false)
                  .map(({ key, ar: arLabel, en: enLabel, icon: Icon }) => (
                    <button
                      key={key}
                      className={`tab-item ${periodicSubTab === key ? 'active' : ''}`}
                      onClick={() => setPeriodicSubTab(key)}
                      style={periodicSubTab === key ? { borderBottomColor: '#f59e0b', color: '#f59e0b' } : {}}
                    >
                      <Icon size={16} />
                      <span>{lang === 'ar' ? arLabel : enLabel}</span>
                    </button>
                  ))}
              </div>

              {rv.periodic_generation !== false && (
                <div className={periodicSubTab === 'generation' ? '' : 'periodic-tab-hidden'}>{renderPeriodicGenerationTable()}</div>
              )}
              {rv.periodic_fuel !== false && (
                <div className={periodicSubTab === 'fuel' ? '' : 'periodic-tab-hidden'}>{renderPeriodicFuelTable()}</div>
              )}
              {rv.periodic_oil !== false && (
                <div className={periodicSubTab === 'oil' ? '' : 'periodic-tab-hidden'}>{renderPeriodicOilTable()}</div>
              )}

              {/* Signatures + footer — only visible during periodic print */}
              <div className="print-only">
                <div className="print-signatures">
                  <div className="signature-box">
                    <p>{lang === 'ar' ? 'توقيع المختص' : 'Specialist Signature'}</p>
                    <div className="line"></div>
                  </div>
                  <div className="signature-box">
                    <p>{lang === 'ar' ? 'توقيع المدير' : 'Manager Signature'}</p>
                    <div className="line"></div>
                  </div>
                </div>
                <div className="print-footer-info">
                  <span>{lang === 'ar' ? 'تاريخ الطباعة:' : 'Print Date:'} {new Date().toLocaleString(lang === 'ar' ? 'ar-EG' : 'en-US')}</span>
                  <span>{lang === 'ar' ? 'بواسطة المستخدم:' : 'Printed by:'} {currentUser?.name || ''}</span>
                </div>
              </div>

              {/* ── Periodic Print / Export Buttons ── */}
              <div className="no-print" style={{
                display: 'flex', gap: 12, justifyContent: 'flex-end',
                padding: '16px 20px', borderTop: '1px solid var(--border-color)'
              }}>
                <button
                  className="btn btn-primary"
                  onClick={exportPeriodicExcel}
                  style={{ background: '#10b981', borderColor: '#10b981', display: 'flex', alignItems: 'center', gap: 8 }}
                >
                  <FileSpreadsheet size={16} />
                  <span>{lang === 'ar' ? 'تصدير Excel' : 'Export Excel'}</span>
                </button>
                {(() => { const pdf = getPeriodicPdfData(); return pdf.sheets.length > 0 && (
                  <button
                    className="btn btn-ghost"
                    onClick={() => exportPeriodicToPdf(pdf.sheets, `periodic-report-${periodicDateFrom}-${periodicDateTo}.pdf`)}
                    style={{ display: 'flex', alignItems: 'center', gap: 8 }}
                  >
                    <FileText size={16} />
                    <span>{lang === 'ar' ? 'تصدير PDF' : 'Export PDF'}</span>
                  </button>
                ); })()}
                <button
                  className="btn btn-ghost"
                  onClick={printPeriodic}
                  style={{ display: 'flex', alignItems: 'center', gap: 8 }}
                >
                  <Printer size={16} />
                  <span>{lang === 'ar' ? 'طباعة' : 'Print'}</span>
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      <style>{`
        .reports-page { padding: 20px; color: var(--text-main); }
        .header-main { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
        .title-section { display: flex; align-items: center; gap: 16px; }
        .icon-box { width: 48px; height: 48px; background: var(--primary-light); color: var(--primary); display: flex; align-items: center; justify-content: center; border-radius: 12px; }
        .controls-section { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }
        .date-picker-wrap { display: flex; align-items: center; gap: 10px; }
        .date-picker-wrap > svg,
        .date-picker-wrap > input,
        .date-picker-wrap > span { display: none; }
        .date-field { display: flex; flex-direction: column; gap: 6px; color: var(--text-muted); font-size: 12px; font-weight: 700; }
        .date-input-shell { display: flex; align-items: center; gap: 8px; min-width: 158px; height: 44px; background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 10px; padding: 0 12px; color: var(--text-main); cursor: pointer; }
        .date-input-shell:focus-within { border-color: var(--primary); box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.14); }
        .date-input-shell input { flex: 1; width: 100%; background: transparent; border: none; color: inherit; font-family: inherit; font-size: 14px; font-weight: 700; outline: none; cursor: pointer; }
        .date-input-shell svg { color: var(--primary); flex-shrink: 0; pointer-events: none; }
        .date-input-shell input::-webkit-calendar-picker-indicator { cursor: pointer; opacity: 0.85; filter: invert(1); }
        .tabs-navigation { display: flex; gap: 8px; margin-bottom: 20px; border-bottom: 1px solid var(--border-color); padding-bottom: 1px; }
        .tab-item { display: flex; align-items: center; gap: 8px; padding: 12px 20px; border: none; background: transparent; color: var(--text-muted); cursor: pointer; transition: all 0.2s; border-bottom: 2px solid transparent; font-weight: 500; }
        .tab-item:hover { color: var(--text-main); background: var(--bg-hover); }
        .tab-item.active { color: var(--primary); border-bottom-color: var(--primary); background: var(--primary-light-alpha); }
        .report-content { min-height: 400px; }
        .report-card { padding: 0; overflow: hidden; }
        .table-container { overflow-x: auto; }
        .report-table { width: 100%; border-collapse: collapse; font-size: 13px; }
        .report-table th { background: #1e293b; color: #f8fafc; padding: 12px 8px; border: 1px solid #334155; text-align: center; font-weight: 600; }
        .report-table td { padding: 10px 8px; border: 1px solid var(--border-color); text-align: center; }
        .header-row-1 th { background: #0f172a; }
        .unit-row th { background: #334155; font-size: 11px; font-weight: normal; }
        .total-row td { background: var(--primary-light-alpha); font-weight: bold; color: #ef4444; border-top: 2px solid var(--primary); }
        .total-row-label { color: #ef4444 !important; }
        .bold-cell { font-weight: 600; color: var(--text-main); }
        .by-cell { max-width: 110px; font-size: 12px; white-space: normal; word-break: break-word; }
        .hidden-report-author-cell { display: none !important; }
        .rate-cell { font-family: monospace; color: #10b981; font-weight: 600; }
        .positive-cell { color: #10b981; font-weight: 600; }
        .negative-cell { color: #ef4444; font-weight: 600; }
        .oil-type-cell { background: #ffffff; color: #1e293b; border-radius: 4px; padding: 6px 16px; display: inline-flex; align-items: center; justify-content: center; min-width: 100px; font-weight: 700; text-transform: uppercase; font-size: 11px; letter-spacing: 0.5px; box-shadow: 0 2px 5px rgba(0,0,0,0.2); }
        .report-subtitle { padding: 12px 20px; background: #334155; color: white; font-weight: 600; text-align: center; }
        .report-loader { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 100px; gap: 16px; }
        .empty-msg { color: var(--text-muted); padding: 40px; font-style: italic; }
        
        .print-only { display: none; }

        /* Periodic Reports Section */
        .periodic-reports-section {
          margin-top: 36px;
          padding-top: 28px;
          border-top: 2px solid rgba(245, 158, 11, 0.25);
        }
        .periodic-section-header {
          display: flex;
          flex-direction: column;
          gap: 16px;
          margin-bottom: 20px;
        }
        .periodic-title-row {
          display: flex;
          align-items: center;
          gap: 16px;
        }
        .periodic-title-row h2 {
          margin: 0;
          font-size: 1.25rem;
          font-weight: 900;
          color: var(--text-main);
        }
        .periodic-title-row p {
          margin: 4px 0 0;
          font-size: 0.85rem;
          color: var(--text-muted);
        }
        .periodic-filter-bar {
          display: flex;
          gap: 12px;
          align-items: flex-end;
          flex-wrap: wrap;
        }
        .periodic-sub-tabs {
          display: flex;
          gap: 8px;
          border-bottom: 1px solid var(--border-color);
          padding: 0 0 1px;
        }
        .periodic-empty-state {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          padding: 60px 20px;
          gap: 14px;
          color: var(--text-muted);
          font-size: 0.9rem;
        }
        .efficiency-high { color: #10b981; font-weight: 700; }
        .efficiency-mid { color: #f59e0b; font-weight: 700; }
        .efficiency-low { color: #ef4444; font-weight: 700; }
        .periodic-tab-hidden { display: none; }

        @media print {
          /* Removing page margins gets rid of the browser's default headers (URL, Title, Date) */
          @page { size: landscape; margin: 0; }
          
          /* Reset heights to avoid blank pages caused by 100vh */
          html, body, #root, .app-root { height: auto !important; min-height: 0 !important; margin: 0 !important; padding: 0 !important; box-sizing: border-box !important; }
          
          body { background: #fff !important; color: #000 !important; font-family: 'Arial', sans-serif !important; }
          
          .sidebar, .controls-section, .tabs-navigation, .page-header { display: none !important; }
          .main-content { margin: 0 !important; padding: 0 !important; width: 100% !important; max-width: 100% !important; overflow: visible !important; height: auto !important; min-height: 0 !important; }
          .glass-card { border: none !important; box-shadow: none !important; background: transparent !important; margin: 0 !important; padding: 0 !important; overflow: visible !important; }
          
          /* Add padding to the page container instead of body */
          .reports-page { padding: 10mm !important; box-sizing: border-box !important; height: auto !important; min-height: 0 !important; overflow: visible !important; }
          .report-content { min-height: 0 !important; overflow: visible !important; }
          
          .print-only { display: block !important; }
          .no-print { display: none !important; }

          /* Print Layout */
          .print-header { display: grid !important; grid-template-columns: 1fr 2fr 1fr !important; align-items: center !important; border-bottom: 2px solid #000; padding-bottom: 10px; margin-bottom: 15px; }
          .print-title { text-align: center !important; }
          .print-title h1 { font-size: 22px !important; margin: 0 0 8px 0 !important; color: #000 !important; }
          .print-title p { margin: 0 !important; font-size: 14px !important; color: #000 !important; }

          /* Enhanced Table Borders */
          .table-container { margin: 0 !important; padding: 0 !important; overflow: visible !important; }
          .report-table { border: 2px solid #000 !important; border-collapse: collapse !important; width: 100% !important; margin: 0 !important; }
          .report-table th, .report-table td { border: 1px solid #000 !important; padding: 3px 3px !important; color: #000 !important; font-size: 9px !important; background: transparent !important; }
          .report-table th { background-color: #f1f5f9 !important; -webkit-print-color-adjust: exact; print-color-adjust: exact; font-weight: bold !important; color: #000 !important; }
          .header-row-1 th { background-color: #e2e8f0 !important; border-bottom: 2px solid #000 !important; }
          .unit-row th { background-color: #f8fafc !important; font-size: 8px !important; font-style: italic; }
          .total-row td { background-color: #e2e8f0 !important; font-weight: bold !important; -webkit-print-color-adjust: exact; print-color-adjust: exact; border-top: 2px solid #000 !important; }
          /* Ensure wide tables fit in print */
          .reports-page .table-container { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
          .reports-page .report-table { table-layout: fixed !important; min-width: 0 !important; width: calc(100% - 5mm) !important; margin-left: 5mm !important; }
          .reports-page .report-table th,
          .reports-page .report-table td { overflow: visible !important; white-space: normal !important; word-break: break-word !important; }
          .reports-page table[style*="minWidth"] { min-width: 0 !important; width: calc(100% - 5mm) !important; }
          .reports-page .by-cell { font-size: 7px !important; line-height: 1.15 !important; max-width: 18mm !important; }
          .reports-page .generators-report-table th,
          .reports-page .generators-report-table td { font-size: 7px !important; padding: 2px 2px !important; }
          .reports-page .generators-report-table .unit-row th { font-size: 6px !important; }
          /* Daily print: hide periodic section */
          .periodic-reports-section { display: none !important; }

          /* Periodic print mode: hide daily section, show periodic */
          .print-periodic .page-header { display: none !important; }
          .print-periodic .report-content { display: none !important; }
          .print-periodic .tabs-navigation { display: none !important; }
          .print-periodic .periodic-reports-section { display: block !important; margin-top: 0 !important; padding-top: 0 !important; border-top: none !important; }
          .print-periodic .periodic-section-header { display: none !important; }
          .print-periodic .periodic-tab-hidden { display: block !important; }
          .print-periodic .periodic-print-header { display: grid !important; grid-template-columns: 1fr 2fr 1fr !important; align-items: center !important; border-bottom: 2px solid #000 !important; padding-bottom: 6px !important; margin-bottom: 8px !important; }
          .print-periodic .print-signatures { margin-top: 10px !important; padding: 0 40px !important; }
          .print-periodic .signature-box p { margin-bottom: 28px !important; }
          .print-periodic .print-footer-info { margin-top: 12px !important; padding-top: 6px !important; }
          
          /* Colors for Print */
          .rate-cell { color: #000 !important; }
          .positive-cell, .negative-cell { color: #000 !important; }
          .report-subtitle { background: #e2e8f0 !important; color: #000 !important; border: 1px solid #000 !important; border-bottom: none !important; -webkit-print-color-adjust: exact; print-color-adjust: exact; }

          /* Print Footer */
          .print-footer { margin-top: 20px !important; page-break-inside: avoid !important; }
          .print-signatures { display: flex !important; justify-content: space-between !important; margin-top: 20px !important; padding: 0 50px !important; }
          .signature-box { text-align: center !important; width: 250px !important; }
          .signature-box p { font-size: 16px !important; font-weight: bold !important; margin-bottom: 40px !important; }
          .signature-box .line { border-top: 1px solid #000 !important; width: 100% !important; }
          
          .print-footer-info { display: flex !important; justify-content: space-between !important; font-size: 12px !important; color: #000 !important; border-top: 1px solid #000 !important; padding-top: 10px !important; margin-top: 40px !important; }
        }
      `}</style>
    </div>
  );
}

export default Reports;
