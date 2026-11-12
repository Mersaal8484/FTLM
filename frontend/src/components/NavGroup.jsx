import React, { useEffect, useMemo, useState } from 'react';
import { ChevronDown } from 'lucide-react';
import { NavLink, useLocation } from 'react-router-dom';

export default function NavGroup({ icon: Icon, label, headerTo, items, onNavigate }) {
  const location = useLocation();
  const hasActiveChild = useMemo(
    () => items.some(item => location.pathname === item.to || location.pathname.startsWith(`${item.to}/`)),
    [items, location.pathname],
  );
  const [open, setOpen] = useState(hasActiveChild);

  useEffect(() => { if (hasActiveChild) setOpen(true); }, [hasActiveChild]);
  if (!items.length) return null;

  const heading = <><Icon size={18} strokeWidth={1.8} /><span style={{ flex: 1, textAlign: 'start' }}>{label}</span></>;

  return (
    <div className="nav-group" data-nav-group={label} data-nav-top={label}>
      <div className={`nav-group-header${hasActiveChild ? ' child-active' : ''}`}>
        {headerTo ? <>
          <NavLink to={headerTo} end onClick={onNavigate} className={({ isActive }) => `nav-item nav-group-link${isActive ? ' active' : ''}`}>{heading}</NavLink>
          <button type="button" className="nav-group-toggle" aria-label={label} aria-expanded={open} onClick={() => setOpen(value => !value)}>
            <ChevronDown size={14} style={{ transform: open ? 'rotate(180deg)' : 'none', transition: 'var(--transition)' }} />
          </button>
        </> : (
          <button type="button" className="nav-item nav-group-link" aria-expanded={open} onClick={() => setOpen(value => !value)}>
            {heading}<ChevronDown size={14} style={{ transform: open ? 'rotate(180deg)' : 'none', transition: 'var(--transition)' }} />
          </button>
        )}
      </div>
      {open && <div className="nav-group-children">
        {items.map(({ to, icon: ChildIcon, label: childLabel }) => (
          <NavLink key={to} to={to} onClick={onNavigate} className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}>
            <ChildIcon size={16} strokeWidth={1.8} className="text-muted" /><span>{childLabel}</span>
          </NavLink>
        ))}
      </div>}
    </div>
  );
}
