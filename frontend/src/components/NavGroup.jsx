import React, { useEffect, useMemo, useState } from 'react';
import { ChevronDown } from 'lucide-react';
import { NavLink, useLocation } from 'react-router-dom';

const navDomain = (path = '') => {
  if (path === '/' || path.startsWith('/generators') || path.startsWith('/my-generators') || path.startsWith('/operations')) return 'blue';
  if (path.startsWith('/fuel') || path.startsWith('/stock') || path.startsWith('/inventory')) return 'amber';
  if (path.startsWith('/maintenance') || path.startsWith('/review')) return 'red';
  if (path.startsWith('/warehouse')) return 'teal';
  if (path.startsWith('/reports')) return 'purple';
  return 'indigo';
};

export default function NavGroup({ icon: Icon, label, headerTo, items, onNavigate, collapsed = false }) {
  const location = useLocation();
  const hasActiveChild = useMemo(
    () => items.some(item => location.pathname === item.to || location.pathname.startsWith(`${item.to}/`)),
    [items, location.pathname],
  );
  const [open, setOpen] = useState(hasActiveChild);

  useEffect(() => { if (hasActiveChild) setOpen(true); }, [hasActiveChild]);
  if (!items.length) return null;

  const groupDomain = headerTo ? navDomain(headerTo) : (items[0] ? navDomain(items[0].to) : 'indigo');
  const heading = <><span className={`nav-icon-tile nav-icon-tile-${groupDomain}`}><Icon size={19} strokeWidth={2.25} /></span><span className="nav-item-label" style={{ flex: 1, textAlign: 'start' }}>{label}</span></>;

  return (
    <div className="nav-group" data-nav-group={label} data-nav-top={label}>
      <div className={`nav-group-header${hasActiveChild ? ' child-active' : ''}`}>
        {headerTo ? <>
          <NavLink to={headerTo} end title={collapsed ? label : undefined} onClick={onNavigate} className={({ isActive }) => `nav-item nav-group-link${isActive ? ' active' : ''}`}>{heading}</NavLink>
          <button type="button" className="nav-group-toggle" aria-label={label} aria-expanded={open} onClick={() => setOpen(value => !value)}>
            <ChevronDown size={14} style={{ transform: open ? 'rotate(180deg)' : 'none', transition: 'var(--transition)' }} />
          </button>
        </> : (
          <button type="button" className="nav-item nav-group-link" title={collapsed ? label : undefined} aria-expanded={open} onClick={() => setOpen(value => !value)}>
            {heading}<ChevronDown size={14} style={{ transform: open ? 'rotate(180deg)' : 'none', transition: 'var(--transition)' }} />
          </button>
        )}
      </div>
      {open && <div className="nav-group-children">
        {items.map(({ to, icon: ChildIcon, label: childLabel }) => (
          <NavLink key={to} to={to} title={collapsed ? childLabel : undefined} onClick={onNavigate} className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}>
            <span className={`nav-icon-tile nav-icon-tile-${navDomain(to)}`}><ChildIcon size={18} strokeWidth={2.25} /></span><span className="nav-item-label">{childLabel}</span>
          </NavLink>
        ))}
      </div>}
    </div>
  );
}
