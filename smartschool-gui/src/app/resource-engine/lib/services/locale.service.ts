import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export interface LocaleInfo {
  code: string;
  name: string;
  nativeName?: string;
  isRtl: boolean;
  sortOrder: number;
}

const STORAGE_KEY = 'ss.locale';
const FALLBACK = 'en';

@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly doc = inject(DOCUMENT);
  private readonly translate = inject(TranslateService);

  private readonly _available = signal<LocaleInfo[]>([]);
  private readonly _current = signal<string>(
    (typeof localStorage !== 'undefined' && localStorage.getItem(STORAGE_KEY)) || FALLBACK
  );

  readonly available = this._available.asReadonly();
  readonly current = this._current.asReadonly();
  readonly isRtl = computed(() =>
    this._available().find(l => l.code === this._current())?.isRtl ?? false);

  constructor() {
    effect(() => {
      const code = this._current();
      this.translate.use(code);
      const html = this.doc.documentElement;
      html.setAttribute('lang', code);
      html.setAttribute('dir', this.isRtl() ? 'rtl' : 'ltr');
      if (typeof localStorage !== 'undefined') localStorage.setItem(STORAGE_KEY, code);
    });
  }

  setAvailable(locales: LocaleInfo[]): void {
    this._available.set(locales);
    if (!locales.find(l => l.code === this._current())) {
      this._current.set(locales[0]?.code ?? FALLBACK);
    }
  }

  use(code: string): void {
    this._current.set(code);
  }
}
