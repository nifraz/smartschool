import { Injectable, signal } from '@angular/core';

/**
 * Holds the current user's permission codes (e.g. ["school:read","class:manage"]).
 * Wildcards: "*:*" or "school:*" or "*:read".
 * Loaded by the bootstrap resolver from the backend.
 */
@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly _codes = signal<Set<string>>(new Set());
  readonly codes = this._codes.asReadonly();

  set(codes: string[]): void {
    this._codes.set(new Set(codes));
  }

  has(code: string): boolean {
    const codes = this._codes();
    if (codes.has('*:*') || codes.has(code)) return true;
    const [res, act] = code.split(':');
    return codes.has(`${res}:*`) || codes.has(`*:${act}`);
  }
}
