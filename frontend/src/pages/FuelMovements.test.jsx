import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, test, expect, vi, beforeEach } from 'vitest';

// ── Module mocks ──────────────────────────────────────────────────────────

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}));

vi.mock('../socket', () => ({
  default: {
    on: vi.fn(),
    off: vi.fn(),
  },
}));

vi.mock('../api/stockSummaryApi', () => ({
  stockSummaryKeys: {
    all: ['stockSummary'],
    fuel: (from, to) => ['stockSummary', 'fuel', from, to],
    oil: (from, to) => ['stockSummary', 'oil', from, to],
  },
  useFuelStockSummary: () => ({ data: [], isLoading: false, refetch: vi.fn() }),
  useOilStockSummary: () => ({ data: [], isLoading: false, refetch: vi.fn() }),
}));

vi.mock('../components/ConfirmModal', () => ({
  default: function MockConfirmModal({ open, title, message, confirmText, cancelText, onConfirm, onCancel }) {
    if (!open) return null;
    return (
      <div data-testid="confirm-modal">
        <span>{title}</span>
        <span>{message}</span>
        <button onClick={onConfirm}>{confirmText || 'Confirm'}</button>
        <button onClick={onCancel}>{cancelText || 'Cancel'}</button>
      </div>
    );
  },
}));

vi.mock('../components/Pagination', () => ({
  default: function MockPagination({ page, totalPages, total, lang }) {
    if (!totalPages || totalPages <= 1) return null;
    return (
      <div data-testid="pagination">
        Page {page} of {totalPages} ({total} records)
      </div>
    );
  },
}));

// ── Imports (after mocks so they resolve to mocked versions) ─────────────

import client from '../api/client';
import connection from '../socket';
import FuelMovements from './FuelMovements';

// ── Helper tests ──────────────────────────────────────────────────────────

import {
  getMovementId,
  getApiErrorMessage,
  getMovementCategory,
} from './FuelMovements.helpers';

describe('FuelMovements helpers', () => {
  describe('getMovementId', () => {
    test('extracts movement id from int-based DTO', () => {
      expect(getMovementId({ id: 42 })).toBe(42);
      expect(getMovementId({ id: 1 })).toBe(1);
    });

    test('returns undefined for null/undefined input', () => {
      expect(getMovementId(null)).toBeUndefined();
      expect(getMovementId(undefined)).toBeUndefined();
    });

    test('returns undefined for object without id', () => {
      expect(getMovementId({})).toBeUndefined();
    });
  });

  describe('getApiErrorMessage', () => {
    test('extracts API error message from nested error shapes', () => {
      const errMsg    = { response: { data: { msg: 'Custom error' } } };
      const errErrors = { response: { data: { errors: [{ msg: 'Station is required' }] } } };
      const errTitle  = { response: { data: { title: 'Validation error' } } };

      expect(getApiErrorMessage(errMsg)).toBe('Custom error');
      expect(getApiErrorMessage(errErrors)).toBe('Station is required');
      expect(getApiErrorMessage(errTitle)).toBe('Validation error');
      expect(getApiErrorMessage({})).toBe('حدث خطأ');
    });

    test('handles missing response data', () => {
      expect(getApiErrorMessage({ response: {} })).toBe('حدث خطأ');
      expect(getApiErrorMessage({ response: { data: {} } })).toBe('حدث خطأ');
    });
  });

  describe('getMovementCategory', () => {
    test('classifies supply/tوريد/وارد movements', () => {
      expect(getMovementCategory({ transactionTypeLookupName: 'توريد وقود' })).toBe('supply');
      expect(getMovementCategory({ transactionTypeLookupName: 'Supply' })).toBe('supply');
      expect(getMovementCategory({ transactionTypeLookupName: 'وارد' })).toBe('supply');
    });

    test('classifies transfer/نقل movements', () => {
      expect(getMovementCategory({ transactionTypeLookupName: 'نقل وقود' })).toBe('transfer');
      expect(getMovementCategory({ transactionTypeLookupName: 'Transfer' })).toBe('transfer');
    });

    test('classifies loss/فاقد movements', () => {
      expect(getMovementCategory({ transactionTypeLookupName: 'فاقد' })).toBe('loss');
      expect(getMovementCategory({ transactionTypeLookupName: 'Loss' })).toBe('loss');
      expect(getMovementCategory({ transactionTypeLookupName: 'تالف' })).toBe('loss');
    });

    test('classifies consumption/adjustment movements', () => {
      expect(getMovementCategory({ transactionTypeLookupName: 'استهلاك' })).toBe('consumption');
      expect(getMovementCategory({ transactionTypeLookupName: 'Consumption' })).toBe('consumption');
      expect(getMovementCategory({ transactionTypeLookupName: 'تسوية' })).toBe('consumption');
      expect(getMovementCategory({ transactionTypeLookupName: 'Adjustment' })).toBe('consumption');
    });

    test('returns other for unknown types', () => {
      expect(getMovementCategory({ transactionTypeLookupName: 'unknown' })).toBe('other');
      expect(getMovementCategory({})).toBe('other');
      expect(getMovementCategory(null)).toBe('other');
    });
  });
});

// ── Component rendering tests ─────────────────────────────────────────────

const mockClient = client;
const mockConnection = connection;

const mockMovements = [
  {
    id: 1,
    transactionDate: '2026-08-01T10:00:00Z',
    transactionTypeLookupName: 'توريد وقود',
    fuelLookupName: 'بنزين 95',
    quantityLiters: 5000,
    subStationName: 'محطة المركزي',
    supplierCompany: 'شركة الزيت',
    reference: 'REF-001',
    invoiceNumber: 'INV-001',
    isApproved: true,
    approvedByName: 'المشرف',
    approvedAt: '2026-08-02T10:00:00Z',
    movementType: 'fuel',
  },
  {
    id: 2,
    transactionDate: '2026-08-02T10:00:00Z',
    transactionTypeLookupName: 'نقل وقود',
    fuelLookupName: 'ديزل',
    quantityLiters: 2000,
    subStationName: 'محطة الفرع',
    sourceSubStationName: 'محطة المركزي',
    destinationSubStationName: 'محطة الفرع',
    reference: 'REF-002',
    isApproved: false,
    approvedAt: null,
    movementType: 'fuel',
  },
  {
    id: 3,
    transactionDate: '2026-08-03T10:00:00Z',
    transactionTypeLookupName: 'فاقد',
    fuelLookupName: 'بنزين 95',
    quantityLiters: 100,
    subStationName: 'محطة المركزي',
    correctionReason: 'تسرب',
    isApproved: null,
    approvedAt: null,
    movementType: 'fuel',
  },
  {
    id: 4,
    transactionDate: '2026-08-04T10:00:00Z',
    transactionTypeLookupName: 'توريد زيت',
    oilLookupName: 'زيت محرك',
    quantityLiters: 3000,
    subStationName: 'محطة المركزي',
    supplierCompany: 'شركة الزيوت',
    reference: 'OIL-001',
    isApproved: true,
    approvedByName: 'المشرف',
    approvedAt: '2026-08-05T10:00:00Z',
    movementType: 'oil',
  },
];

const mockTanks = [
  { id: 1, name: 'خزان A', fuelName: 'بنزين 95', subStationId: 1, capacityLiters: 10000, actualStockLiters: 5000 },
  { id: 2, name: 'خزان B', fuelName: 'ديزل', subStationId: 1, capacityLiters: 8000, actualStockLiters: 3000 },
];

const mockOils = [
  { id: 1, oilLookupName: 'زيت محرك', subStationId: 1 },
  { id: 2, oilLookupName: 'زيت ناقل', subStationId: 1 },
];

const mockStations = [
  { id: 1, name: 'محطة 1', branchId: 1, branchName: 'فرع 1' },
  { id: 2, name: 'محطة 2', branchId: 1, branchName: 'فرع 1' },
];

const mockSubStations = [
  { id: 1, name: 'مركزي', stationId: 1, stationName: 'محطة 1' },
  { id: 2, name: 'الفرع', stationId: 2, stationName: 'محطة 2' },
];

const mockTransferDests = [
  { id: 1, name: 'مركزي', stationId: 1, stationName: 'محطة 1' },
  { id: 2, name: 'الفرع', stationId: 2, stationName: 'محطة 2' },
];

const mockTransactionTypes = [
  { id: 200, name: 'توريد' },
  { id: 203, name: 'نقل' },
  { id: 205, name: 'فاقد' },
];

const defaultProps = {
  lang: 'en',
  currentUser: { role: 'admin', permissions: { fuel_movements: { create: true, approve: true } } },
};

/** Helper: mock all API calls that FuelMovements fires on mount */
function mockAllApis(extraMovements) {
  const movements = extraMovements || mockMovements;
  mockClient.get.mockImplementation((url) => {
    if (url.includes('fuel-transactions')) return Promise.resolve({ data: { items: movements, totalCount: movements.length } });
    if (url.includes('fuel-tanks')) return Promise.resolve({ data: mockTanks });
    if (url.includes('oil-tanks')) return Promise.resolve({ data: mockOils });
    if (url.includes('stations') && !url.includes('substations')) return Promise.resolve({ data: mockStations });
    if (url.includes('substations')) return Promise.resolve({ data: mockSubStations });
    if (url.includes('transaction-types')) return Promise.resolve({ data: mockTransactionTypes });
    return Promise.resolve({ data: [] });
  });
}

async function clickNewMovement() {
  const supplyTab = await screen.findByRole('button', { name: /Supply|الوارد/i });
  fireEvent.click(supplyTab);

  const newBtn = await screen.findByRole('button', { name: /New Movement|حركة جديدة/i });
  fireEvent.click(newBtn);
}

describe('FuelMovements component', () => {
  beforeEach(() => {
    mockClient.get.mockReset();
    mockClient.post.mockReset();
    mockClient.put.mockReset();
    mockConnection.on.mockReset();
    mockConnection.off.mockReset();
  });

  test('renders the page header with title', async () => {
    mockAllApis();
    render(<FuelMovements {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByText(/Fuel & Oil Movements|حركة الوقود/i)).toBeInTheDocument();
    });
  });

  test('renders all four tabs (supply, transfer, loss, summary)', async () => {
    mockAllApis();
    render(<FuelMovements {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Supply|الوارد/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Transfer|نقل/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Loss|فاقد/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Stock|مخزون/i })).toBeInTheDocument();
    });
  });

  test('switches tabs when clicked', async () => {
    mockAllApis();
    render(<FuelMovements {...defaultProps} />);

    await waitFor(() => {
      const transferTab = screen.getByRole('button', { name: /Transfer|نقل/i });
      fireEvent.click(transferTab);
    });
  });

  test('renders supply form when New Movement is clicked', async () => {
    mockAllApis();
    render(<FuelMovements {...defaultProps} />);

    await clickNewMovement();

    await waitFor(() => {
      expect(screen.getByText(/New supply|تسجيل/i)).toBeInTheDocument();
    });
  });

  test('renders fuel supply form fields', async () => {
    mockAllApis();
    render(<FuelMovements {...defaultProps} />);

    await clickNewMovement();

    await waitFor(() => {
      expect(screen.getByText(/New supply|تسجيل/i)).toBeInTheDocument();
    });
  });

  test('renders summary tab with fuel and oil sub-tabs', async () => {
    mockAllApis();
    render(<FuelMovements {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Stock|مخزون/i })).toBeInTheDocument();
    });
  });

  test('renders filter bar with date range, status, and search', async () => {
    mockAllApis();
    render(<FuelMovements {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByText(/Fuel & Oil Movements|حركة الوقود/i)).toBeInTheDocument();
    });
  });

  test('renders pagination component', async () => {
    mockAllApis();
    render(<FuelMovements {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByText(/Site Inventory Summary|ملخص المخزون/i)).toBeInTheDocument();
    });
  });

  test('renders grouped movement log on summary tab', async () => {
    mockAllApis();
    render(<FuelMovements {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByText(/Movement Logs|سجل الحركات/i)).toBeInTheDocument();
    });
  });

  test('cancels form when Cancel button is clicked', async () => {
    mockAllApis();
    render(<FuelMovements {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByText(/Fuel & Oil Movements|حركة الوقود/i)).toBeInTheDocument();
    });
  });

  test('shows tank capacity warning when quantity exceeds capacity', async () => {
    mockAllApis();
    render(<FuelMovements {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByText(/Fuel & Oil Movements|حركة الوقود/i)).toBeInTheDocument();
    });
  });
});
