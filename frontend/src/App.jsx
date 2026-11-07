import React, { Suspense, lazy, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import './styles.css';
import client, { setSessionUserUpdateHandler, refreshSessionUser } from './api/client';
import Sidebar from './components/Sidebar';
import ErrorBoundary from './components/ErrorBoundary';
import usePermissions from './permissions/usePermissions';
import useAuthStore from './store/authStore';
import useUiStore from './store/uiStore';
import Toast from './components/Toast';
import socket from './socket';

const Dashboard = lazy(() => import('./pages/Dashboard'));
const Login = lazy(() => import('./pages/Login'));
const ForgotPassword = lazy(() => import('./pages/ForgotPassword'));
const ForceChangePassword = lazy(() => import('./pages/ForceChangePassword'));
const Generators = lazy(() => import('./pages/Generators'));
const ActiveGenerators = lazy(() => import('./pages/ActiveGenerators'));
const TanksManagement = lazy(() => import('./pages/TanksManagement'));
const OilInventory = lazy(() => import('./pages/OilInventory'));
const LocationsManagement = lazy(() => import('./pages/LocationsManagement'));
const MetersManagement = lazy(() => import('./pages/MetersManagement'));
const Users = lazy(() => import('./pages/Users'));
const Reports = lazy(() => import('./pages/reports'));
const Controllers = lazy(() => import('./pages/Controllers'));
const RolesManagement = lazy(() => import('./pages/RolesManagement'));
const AuditLogs = lazy(() => import('./pages/AuditLogs'));
const SetupOils = lazy(() => import('./pages/SetupOils'));
const MyGenerators = lazy(() => import('./pages/MyGenerators'));
const AddReading = lazy(() => import('./pages/AddReading'));
const AddOilToGenerator = lazy(() => import('./pages/AddOilToGenerator'));
const FuelMovements = lazy(() => import('./pages/FuelMovements'));
const MovementLogs = lazy(() => import('./pages/MovementLogs'));
const Notifications = lazy(() => import('./pages/Notifications'));
const PeriodicInventoryPage = lazy(() => import('./pages/PeriodicInventoryPage'));
const ReviewChanges = lazy(() => import('./pages/ReviewChanges'));
const SubStationReading = lazy(() => import('./pages/SubStationReading'));
const RollbackApproval = lazy(() => import('./pages/RollbackApproval'));
const GeneratorsOverview = lazy(() => import('./pages/GeneratorsOverview'));
const FuelStockPage = lazy(() => import('./pages/FuelStockPage'));
const OilStockPage = lazy(() => import('./pages/OilStockPage'));
const WarehouseParts = lazy(() => import('./pages/WarehouseParts'));
const WarehouseStock = lazy(() => import('./pages/WarehouseStock'));
const WarehouseSupply = lazy(() => import('./pages/WarehouseSupply'));
const WarehouseDispatch = lazy(() => import('./pages/WarehouseDispatch'));
const WarehouseReports = lazy(() => import('./pages/WarehouseReports'));
const WorkPermits = lazy(() => import('./pages/WorkPermits'));
const InstalledParts = lazy(() => import('./pages/InstalledParts'));
const MaintenanceDashboard = lazy(() => import('./pages/MaintenanceDashboard'));
const OperationsHub = lazy(() => import('./pages/OperationsHub'));
const OperationalCapture = lazy(() => import('./pages/OperationalCapture'));
const OperationalHistory = lazy(() => import('./pages/OperationalHistory'));
const OperationalReview = lazy(() => import('./pages/OperationalReview'));
const EnergyBalanceBoard = lazy(() => import('./pages/EnergyBalanceBoard'));



/** Maps permission module keys to real app routes (movements â‰  /movements). */
const MODULE_HOME_PATHS = {
  dashboard: '/',
  generators: '/generators',
  movements: '/fuel-movements',
  fuel_movements: '/fuel-movements',
  reports: '/reports',
  readings: '/my-generators',
  inventory: '/inventory',
  tanks: '/tanks',
  oil: '/oils',
  alerts: '/notifications',
};

function firstAllowedHomePath(canFn, viewCanFn = canFn) {
  const order = ['dashboard', 'generators', 'fuel_movements', 'reports', 'readings', 'inventory', 'tanks', 'oil', 'alerts'];
  const module = order.find((m) => viewCanFn(m, 'view'));
  return module ? (MODULE_HOME_PATHS[module] || '/') : '/login';
}

function canAccessOilSetup(canFn) {
  return canFn('settings', 'manage');
}

function AuthBootScreen() {
  return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
      <div className="ft-spinner" style={{ width: 32, height: 32 }} aria-label="Loading" />
    </div>
  );
}

const Protected = ({
  children,
  module,
  action = 'view',
}) => {
  const authChecked = useAuthStore((s) => s.authChecked);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const currentUser = useAuthStore((s) => s.user);
  const passwordChangeRequired = useAuthStore((s) => s.isAuthenticated && s.user?.mustChangePassword);
  const { can } = usePermissions(currentUser);

  if (!authChecked) return <AuthBootScreen />;
  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: window.location.pathname }} />;
  if (passwordChangeRequired) return <Navigate to="/force-password-change" replace />;

  if (module && currentUser && currentUser.role !== 'admin') {
    if (!can(module, action)) {
      return <Navigate to={firstAllowedHomePath(can)} replace />;
    }
  }

  return <ErrorBoundary name={module || 'protected'}>{children}</ErrorBoundary>;
};

const ExplicitProtected = ({
  children,
  module,
  action = 'view',
}) => {
  const authChecked = useAuthStore((s) => s.authChecked);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const currentUser = useAuthStore((s) => s.user);
  const passwordChangeRequired = useAuthStore((s) => s.isAuthenticated && s.user?.mustChangePassword);
  const { can, canExplicit } = usePermissions(currentUser);

  if (!authChecked) return <AuthBootScreen />;
  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: window.location.pathname }} />;
  if (passwordChangeRequired) return <Navigate to="/force-password-change" replace />;

  if (module && currentUser && currentUser.role !== 'admin' && !canExplicit(module, action)) {
    return <Navigate to={firstAllowedHomePath(can, canExplicit)} replace />;
  }

  return <ErrorBoundary name={module || 'explicit-protected'}>{children}</ErrorBoundary>;
};

const OilSetupProtected = ({
  children,
}) => {
  const authChecked = useAuthStore((s) => s.authChecked);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const currentUser = useAuthStore((s) => s.user);
  const passwordChangeRequired = useAuthStore((s) => s.isAuthenticated && s.user?.mustChangePassword);
  const { can } = usePermissions(currentUser);

  if (!authChecked) return <AuthBootScreen />;
  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: window.location.pathname }} />;
  if (passwordChangeRequired) return <Navigate to="/force-password-change" replace />;

  if (currentUser && currentUser.role !== 'admin' && !canAccessOilSetup(can)) {
    return <Navigate to={firstAllowedHomePath(can)} replace />;
  }

  return <ErrorBoundary name="oil-setup">{children}</ErrorBoundary>;
};

function App() {
  const { user, login, setUser, setIsAuthenticated, setAuthChecked, logout } = useAuthStore();
  const { lang, theme, unreadCount, setUnreadCount, toggleTheme } = useUiStore();
  const { t, i18n } = useTranslation();

  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const authChecked = useAuthStore((s) => s.authChecked);
  const passwordChangeRequired = isAuthenticated && user?.mustChangePassword;

  useEffect(() => {
    i18n.changeLanguage(lang);
    // Sync document direction + lang attribute on every lang change
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    document.documentElement.lang = lang;
  }, [lang, i18n]);

  useEffect(() => {
    setSessionUserUpdateHandler((userData) => {
      setUser(userData);
      setIsAuthenticated(true);
    });
    return () => setSessionUserUpdateHandler(null);
  }, [setUser, setIsAuthenticated]);

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const res = await client.get('/api/auth/me');
        setUser(res.data);
        setIsAuthenticated(true);
      } catch {
        setIsAuthenticated(false);
        useAuthStore.setState({ user: null });
      } finally {
        setAuthChecked(true);
      }
    };
    checkAuth();
  }, [setUser, setIsAuthenticated, setAuthChecked]);

  useEffect(() => {
    if (!isAuthenticated) return;
    const fetchCount = async () => {
      try {
        const res = await client.get('/api/notifications');
        setUnreadCount(res.data.unreadCount || 0);
      } catch {}
    };
    fetchCount();
    const interval = setInterval(fetchCount, 30000);
    return () => clearInterval(interval);
  }, [isAuthenticated, setUnreadCount]);

  useEffect(() => {
    if (!isAuthenticated) return undefined;
    const refreshUnreadCount = async () => {
      try {
        const res = await client.get('/api/notifications');
        setUnreadCount(res.data.unreadCount || 0);
      } catch {}
    };
    const onCreated = () => refreshUnreadCount();
    const onRead = () => refreshUnreadCount();
    socket.on('NotificationCreated', onCreated);
    socket.on('NotificationRead', onRead);
    socket.on('AllNotificationsRead', onRead);
    return () => {
      socket.off('NotificationCreated', onCreated);
      socket.off('NotificationRead', onRead);
      socket.off('AllNotificationsRead', onRead);
    };
  }, [isAuthenticated, setUnreadCount]);

  const handleLogin = (userData) => {
    login(userData);
  };

  const handlePasswordChanged = (userData) => {
    setUser(userData);
    setIsAuthenticated(true);
  };

  const ar = lang === 'ar';
  return (
      <Router future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <Toast />
        <div className={`app-root ${ar ? 'rtl' : 'ltr'} ${theme === 'light' ? 'theme-light' : ''}`} dir={ar ? 'rtl' : 'ltr'} lang={lang}>
          {isAuthenticated && !passwordChangeRequired && (
            <Sidebar
              lang={lang}
              theme={theme}
              onToggleTheme={toggleTheme}
              currentUser={user}
              unreadCount={unreadCount}
              onLogout={logout}
            />
          )}
          <main className={isAuthenticated && !passwordChangeRequired ? 'main-content' : 'main-content-full'}>
            <div className="page-wrapper">
              <ErrorBoundary>
                <Suspense fallback={<AuthBootScreen />}>
                <Routes>
              <Route
                path="/login"
                element={
                  !authChecked
                    ? <AuthBootScreen />
                    : isAuthenticated
                      ? <Navigate to={passwordChangeRequired ? '/force-password-change' : '/'} replace />
                      : <ErrorBoundary name="login"><Login onLogin={handleLogin} lang={lang} t={t} /></ErrorBoundary>
                }
              />
              <Route path="/forgot-password" element={<ErrorBoundary name="forgot-password"><ForgotPassword /></ErrorBoundary>} />
              <Route
                path="/force-password-change"
                element={
                  !authChecked
                    ? <AuthBootScreen />
                    : isAuthenticated
                      ? (passwordChangeRequired
                        ? <ErrorBoundary name="force-password-change"><ForceChangePassword lang={lang} onPasswordChanged={handlePasswordChanged} onLogout={logout} /></ErrorBoundary>
                        : <Navigate to="/" replace />)
                      : <Navigate to="/login" replace />
                }
              />

              {/* Dashboard */}
              <Route path="/" element={<Protected module="dashboard"><Dashboard lang={lang} t={t} currentUser={user} /></Protected>} />
              <Route path="/operations" element={<Protected module="operational_data"><OperationsHub /></Protected>} />
              <Route path="/operations/capture" element={<Protected module="operational_data" action="capture"><OperationalCapture /></Protected>} />
              <Route path="/operations/history" element={<Protected module="operational_data"><OperationalHistory /></Protected>} />
              <Route path="/operations/review" element={<Protected module="operational_data" action="review"><OperationalReview /></Protected>} />
              <Route path="/energy-balance" element={<Protected module="operational_data"><EnergyBalanceBoard /></Protected>} />

              {/* Notifications */}
              <Route path="/notifications" element={<Protected module="alerts"><Notifications lang={lang} onRead={() => setUnreadCount(Math.max(0, unreadCount - 1))} /></Protected>} />

              {/* Admin Setup */}
              <Route path="/locations" element={<Protected module="settings" action="manage"><LocationsManagement lang={lang} /></Protected>} />
              <Route path="/meters" element={<Protected module="settings" action="manage"><MetersManagement lang={lang} /></Protected>} />
              <Route path="/generators" element={<ExplicitProtected module="generators" action="view"><Generators lang={lang} t={t} currentUser={user} /></ExplicitProtected>} />
               <Route path="/generators/active" element={<Protected module="generators" action="view"><ActiveGenerators lang={lang} t={t} currentUser={user} /></Protected>} />
              <Route path="/generators/overview" element={<Protected module="generators" action="view"><GeneratorsOverview lang={lang} /></Protected>} />
              <Route path="/stock/fuel" element={<Protected module="inventory"><FuelStockPage lang={lang} /></Protected>} />
              <Route path="/stock/oil" element={<Protected module="oil"><OilStockPage lang={lang} /></Protected>} />
              {/* Warehouse */}
              <Route path="/warehouse/parts" element={<Protected module="warehouse"><WarehouseParts lang={lang} currentUser={user} /></Protected>} />
              <Route path="/warehouse/stock" element={<Protected module="warehouse"><WarehouseStock lang={lang} currentUser={user} /></Protected>} />
              <Route path="/warehouse/supply" element={<Protected module="warehouse"><WarehouseSupply lang={lang} currentUser={user} /></Protected>} />
              <Route path="/warehouse/dispatch" element={<Protected module="warehouse"><WarehouseDispatch lang={lang} currentUser={user} /></Protected>} />
              <Route path="/warehouse/reports" element={<Protected module="warehouse"><WarehouseReports lang={lang} /></Protected>} />
              {/* Maintenance */}
              <Route path="/maintenance" element={<Protected module="maintenance"><MaintenanceDashboard lang={lang} currentUser={user} /></Protected>} />
              <Route path="/maintenance/permits" element={<Protected module="maintenance"><WorkPermits lang={lang} currentUser={user} /></Protected>} />
              <Route path="/maintenance/installed-parts" element={<Protected module="maintenance"><InstalledParts lang={lang} currentUser={user} /></Protected>} />
              <Route path="/setup-oils" element={<OilSetupProtected><SetupOils t={t} lang={lang} /></OilSetupProtected>} />
              <Route path="/tanks" element={<Protected module="tanks"><TanksManagement t={t} lang={lang} currentUser={user} /></Protected>} />
              <Route path="/oils" element={<ExplicitProtected module="oil"><OilInventory t={t} lang={lang} currentUser={user} /></ExplicitProtected>} />
              <Route path="/users" element={<Protected module="users" action="view"><Users lang={lang} t={t} currentUser={user} /></Protected>} />
              <Route path="/roles" element={<Protected module="settings" action="manage"><RolesManagement lang={lang} onRolesChanged={refreshSessionUser} /></Protected>} />
              <Route path="/controllers" element={<Protected module="settings" action="manage"><Controllers lang={lang} t={t} /></Protected>} />
              <Route path="/audits" element={<Protected module="audits"><AuditLogs lang={lang} /></Protected>} />
              <Route path="/rollback" element={<Protected module="settings" action="manage"><RollbackApproval lang={lang} user={user} /></Protected>} />
              {/* Reports */}
              <Route path="/reports/*" element={<Protected module="reports"><Reports lang={lang} /></Protected>} />

              {/* Operators */}
              <Route path="/my-generators" element={<Protected><MyGenerators lang={lang} currentUser={user} /></Protected>} />
              <Route path="/generators/:generatorId/add-reading" element={<Protected module="readings" action="add"><AddReading lang={lang} currentUser={user} /></Protected>} />
              <Route path="/generators/:generatorId/add-oil" element={<Protected module="oil" action="add"><AddOilToGenerator lang={lang} /></Protected>} />
              <Route path="/stations/:stationId/reading" element={<Protected module="readings" action="add"><SubStationReading lang={lang} currentUser={user} /></Protected>} />

              {/* Fuel Manager */}
              <Route path="/fuel-movements" element={<Protected module="fuel_movements"><FuelMovements lang={lang} currentUser={user} /></Protected>} />
              <Route path="/movement-logs" element={<Protected module="fuel_movements"><MovementLogs lang={lang} /></Protected>} />

              {/* Supervisor */}
              <Route path="/inventory" element={<Protected module="inventory"><PeriodicInventoryPage lang={lang} currentUser={user} /></Protected>} />
              <Route path="/review-changes" element={<Protected><ReviewChanges lang={lang} /></Protected>} />

              {/* Fallback */}
              <Route path="*" element={<Navigate to="/" replace />} />
                </Routes>
              </Suspense>
              </ErrorBoundary>
            </div>
          </main>
        </div>
      </Router>
  );
}

export default App;
