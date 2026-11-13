import { useEffect, useState } from 'react';
import useUiStore from '../store/uiStore';

const FALLBACKS = {
  primary: '#69a9e8',
  success: '#65c69a',
  warning: '#e7b86a',
  danger: '#e47d83',
  secondary: '#a78bfa',
  text: '#edf2f7',
  muted: '#a8b5c4',
  grid: '#3b4b5d',
  surface: '#202e3e',
};

const TOKEN_NAMES = {
  primary: '--primary',
  success: '--success',
  warning: '--warning',
  danger: '--danger',
  secondary: '--secondary',
  text: '--text',
  muted: '--text-muted',
  grid: '--chart-grid',
  surface: '--chart-tooltip-bg',
};

export default function useThemeTokens() {
  const theme = useUiStore((state) => state.theme);
  const [tokens, setTokens] = useState(FALLBACKS);

  useEffect(() => {
    const root = document.querySelector('.app-root') || document.documentElement;
    const styles = window.getComputedStyle(root);
    setTokens(Object.fromEntries(Object.entries(TOKEN_NAMES).map(([key, cssName]) => [
      key,
      styles.getPropertyValue(cssName).trim() || FALLBACKS[key],
    ])));
  }, [theme]);

  return tokens;
}
