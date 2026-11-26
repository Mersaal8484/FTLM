import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('./hooks/useCommercialMode', () => ({ default: vi.fn() }));
vi.mock('./socket', () => ({ default: { on: vi.fn(), off: vi.fn() } }));

import useCommercialMode from './hooks/useCommercialMode';
import { MyGeneratorsRoute } from './App';

function renderRoute() {
  return render(
    <MemoryRouter initialEntries={['/my-generators']}>
      <Routes>
        <Route
          path="/my-generators"
          element={
            <MyGeneratorsRoute>
              <div>Enterprise MyGenerators</div>
            </MyGeneratorsRoute>
          }
        />
        <Route path="/operations/capture" element={<div>Commercial Capture Hub</div>} />
      </Routes>
    </MemoryRouter>
  );
}

afterEach(() => vi.resetAllMocks());

test('renders nothing and does not redirect while commercial mode is loading', () => {
  useCommercialMode.mockReturnValue({ isCommercialMode: false, modeResolved: false });
  const { container } = renderRoute();

  expect(container).toBeEmptyDOMElement();
  expect(screen.queryByText('Enterprise MyGenerators')).not.toBeInTheDocument();
  expect(screen.queryByText('Commercial Capture Hub')).not.toBeInTheDocument();
});

test('redirects /my-generators to /operations/capture in commercial mode', () => {
  useCommercialMode.mockReturnValue({ isCommercialMode: true, modeResolved: true });
  renderRoute();

  expect(screen.getByText('Commercial Capture Hub')).toBeInTheDocument();
  expect(screen.queryByText('Enterprise MyGenerators')).not.toBeInTheDocument();
});

test('renders MyGenerators in enterprise mode', () => {
  useCommercialMode.mockReturnValue({ isCommercialMode: false, modeResolved: true });
  renderRoute();

  expect(screen.getByText('Enterprise MyGenerators')).toBeInTheDocument();
  expect(screen.queryByText('Commercial Capture Hub')).not.toBeInTheDocument();
});
