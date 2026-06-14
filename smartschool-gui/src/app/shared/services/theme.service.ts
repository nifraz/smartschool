import { Injectable, signal, effect } from '@angular/core';

export type Theme = 'light' | 'dark';
export type AccentKey = 'indigo' | 'teal' | 'rose' | 'amber' | 'slate';

export interface AccentPalette {
  key: AccentKey;
  label: string;
  color: string;       // main accent (shown as swatch)
  light: string;
  dark: string;
  rgb: string;         // "r,g,b" for rgba() usage
}

export const PALETTES: AccentPalette[] = [
  { key: 'indigo', label: 'Indigo', color: '#4f46e5', light: '#818cf8', dark: '#3730a3', rgb: '79,70,229'   },
  { key: 'teal',   label: 'Teal',   color: '#0d9488', light: '#2dd4bf', dark: '#0f766e', rgb: '13,148,136'  },
  { key: 'rose',   label: 'Rose',   color: '#e11d48', light: '#fb7185', dark: '#be123c', rgb: '225,29,72'   },
  { key: 'amber',  label: 'Amber',  color: '#d97706', light: '#fbbf24', dark: '#b45309', rgb: '217,119,6'   },
  { key: 'slate',  label: 'Slate',  color: '#475569', light: '#94a3b8', dark: '#334155', rgb: '71,85,105'   },
];

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme  = signal<Theme>(this.#storedTheme());
  readonly accent = signal<AccentKey>(this.#storedAccent());

  constructor() {
    effect(() => this.#applyTheme(this.theme()));
    effect(() => this.#applyAccent(this.accent()));
  }

  toggle(): void {
    this.theme.update(t => (t === 'light' ? 'dark' : 'light'));
  }

  setAccent(key: AccentKey): void {
    this.accent.set(key);
  }

  // ── Private ───────────────────────────────────────────────────────────────

  #storedTheme(): Theme {
    return (localStorage.getItem('ss-theme') as Theme) ??
      (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  }

  #storedAccent(): AccentKey {
    return (localStorage.getItem('ss-accent') as AccentKey) ?? 'indigo';
  }

  #applyTheme(theme: Theme): void {
    const html = document.documentElement;
    html.setAttribute('data-bs-theme', theme);
    html.classList.toggle('dark-theme', theme === 'dark');
    localStorage.setItem('ss-theme', theme);
  }

  #applyAccent(key: AccentKey): void {
    const p = PALETTES.find(x => x.key === key) ?? PALETTES[0];
    const root = document.documentElement;
    root.style.setProperty('--ss-accent',       p.color);
    root.style.setProperty('--ss-accent-light',  p.light);
    root.style.setProperty('--ss-accent-dark',   p.dark);
    root.style.setProperty('--ss-accent-rgb',    p.rgb);
    // Keep Angular Material primary in sync
    root.style.setProperty('--mat-sys-primary',  p.color);
    localStorage.setItem('ss-accent', key);
  }
}
