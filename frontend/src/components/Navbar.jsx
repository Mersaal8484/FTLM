import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';

function NavigationBar({ t = {}, onToggleLanguage, isAuthenticated, onLogout }) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <header className="navbar-custom shadow-sm" style={{ background: 'var(--surface)', borderBottom: '1px solid var(--border)', padding: '10px 20px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', maxWidth: 1200, margin: '0 auto' }}>
        <NavLink to="/" style={{ fontSize: 18, fontWeight: 700, color: 'var(--primary)', textDecoration: 'none' }}>
          {t.brand || 'FuelTrack'}
        </NavLink>
        
        <nav style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <NavLink to="/" className={({ isActive }) => `btn btn-sm ${isActive ? 'btn-primary' : 'btn-outline-secondary'}`}>
            {t.dashboard || 'Dashboard'}
          </NavLink>
          <NavLink to="/fuel-movements" className={({ isActive }) => `btn btn-sm ${isActive ? 'btn-primary' : 'btn-outline-secondary'}`}>
            {t.fuelInventory || 'Fuel'}
          </NavLink>
          <NavLink to="/generators" className={({ isActive }) => `btn btn-sm ${isActive ? 'btn-primary' : 'btn-outline-secondary'}`}>
            {t.generators || 'Generators'}
          </NavLink>
          <NavLink to="/reports" className={({ isActive }) => `btn btn-sm ${isActive ? 'btn-primary' : 'btn-outline-secondary'}`}>
            {t.reports || 'Reports'}
          </NavLink>
          {isAuthenticated && (
            <NavLink to="/users" className={({ isActive }) => `btn btn-sm ${isActive ? 'btn-primary' : 'btn-outline-secondary'}`}>
              {t.userManagement || 'Users'}
            </NavLink>
          )}
        </nav>

        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          {onToggleLanguage && (
            <button className="btn btn-sm btn-outline-toggle" onClick={onToggleLanguage}>
              {t.switchLanguage || 'AR/EN'}
            </button>
          )}
          {isAuthenticated ? (
            <button className="btn btn-sm btn-danger" onClick={onLogout}>
              {t.logout || 'Logout'}
            </button>
          ) : (
            <NavLink to="/login" className="btn btn-sm btn-primary">
              {t.login || 'Login'}
            </NavLink>
          )}
        </div>
      </div>
    </header>
  );
}

export default NavigationBar;
