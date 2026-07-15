import React from 'react';
import usePermissions from './usePermissions';

export default function PermissionGate({ module, action = 'view', fallback = null, children, currentUser }) {
  const { can } = usePermissions(currentUser);
  if (can(module, action)) return children;
  return fallback;
}
