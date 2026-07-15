import { create } from 'zustand';
import client from '../api/client';

function normalizeUser(data) {
  if (!data) return null;
  return {
    ...data,
    name: data.name || data.FullName || data.fullName || data.userName || data.UserName || '',
    role: (data.role || data.Role || 'operator').toLowerCase(),
    permissions: data.permissions || {},
  };
}

const useAuthStore = create((set, get) => ({
  user: null,
  isAuthenticated: false,
  authChecked: false,

  get passwordChangeRequired() {
    const s = get();
    return s.isAuthenticated && s.user?.mustChangePassword;
  },

  login: (user) => {
    set({ user: normalizeUser(user), isAuthenticated: true });
  },

  logout: async () => {
    try {
      await client.post('/api/auth/logout');
    } catch {}
    set({ user: null, isAuthenticated: false, authChecked: true });
  },

  setUser: (user) => set({ user: normalizeUser(user) }),

  setAuthChecked: (authChecked) => set({ authChecked }),

  setIsAuthenticated: (isAuthenticated) => set({ isAuthenticated }),
}));

export default useAuthStore;
