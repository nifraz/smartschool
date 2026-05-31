import { Injectable, signal, effect } from '@angular/core';

export type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<Theme>(this.#stored());

  constructor() {
    effect(() => this.#apply(this.theme()));
  }

  toggle(): void {
    this.theme.update(t => t === 'light' ? 'dark' : 'light');
  }

  #stored(): Theme {
    return (localStorage.getItem('ss-theme') as Theme) ??
      (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  }

  #apply(theme: Theme): void {
    const html = document.documentElement;
    // Bootstrap 5.3 dark mode
    html.setAttribute('data-bs-theme', theme);
    // CSS class for custom overrides
    html.classList.toggle('dark-theme', theme === 'dark');
    localStorage.setItem('ss-theme', theme);
  }
}
