import { create } from 'zustand';

const useUiStore = create((set) => ({
  sidebarCollapsed: false,
  theme: localStorage.getItem('theme') || 'dark',
  lang: localStorage.getItem('lang') || 'ar',
  unreadCount: 0,
  isLoading: false,

  toggleSidebar: () => set((s) => ({ sidebarCollapsed: !s.sidebarCollapsed })),

  setTheme: (theme) => {
    localStorage.setItem('theme', theme);
    set({ theme });
  },

  toggleTheme: () =>
    set((s) => {
      const next = s.theme === 'dark' ? 'light' : 'dark';
      localStorage.setItem('theme', next);
      return { theme: next };
    }),

  setLang: (lang) => {
    localStorage.setItem('lang', lang);
    set({ lang });
  },

  setUnreadCount: (unreadCount) => set({ unreadCount }),

  setLoading: (isLoading) => set({ isLoading }),
}));

export default useUiStore;
