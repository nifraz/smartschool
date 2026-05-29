import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Apollo, gql } from 'apollo-angular';
import { TranslateModule } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DynamicGridComponent } from '../components/dynamic-grid.component';
import { ResourceRegistryService } from '../services/resource-registry.service';

const ITEMS_QUERY = gql`
  query ResourceItems($resource: String!, $skip: Int!, $take: Int!) {
    resourceItems(resource: $resource, skip: $skip, take: $take) {
      total
      items
    }
  }
`;

@Component({
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, DynamicGridComponent],
  template: `
    @if (resource(); as r) {
      <header class="page-head">
        <h1>{{ r.label }}</h1>
        <a class="btn" [routerLink]="['/', r.plural, 'new']">{{ 'action.create' | translate }}</a>
      </header>
      <ss-dynamic-grid [resource]="r" [items]="items()" />

      @if (total() > pageSize) {
        <nav class="pagination">
          <button [disabled]="page() === 0" (click)="prev()">‹ Prev</button>
          <span>{{ page() + 1 }} / {{ pages() }}</span>
          <button [disabled]="page() >= pages() - 1" (click)="next()">Next ›</button>
        </nav>
      }
    } @else {
      <p>{{ 'common.loading' | translate }}</p>
    }
  `,
  styles: [`
    .page-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .btn { padding: .5rem .9rem; border-radius: .375rem; background: #1976d2; color: white; text-decoration: none; }
    .pagination { display: flex; align-items: center; gap: .75rem; margin-top: 1rem; }
    .pagination button:disabled { opacity: .4; }
  `],
})
export class ResourceListPage {
  private readonly route = inject(ActivatedRoute);
  private readonly registry = inject(ResourceRegistryService);
  private readonly apollo = inject(Apollo);

  readonly pageSize = 25;

  readonly params = toSignal(this.route.paramMap, { requireSync: true });
  readonly resource = computed(() =>
    this.registry.getByPlural(this.params()!.get('resource') ?? ''));

  readonly page = signal(0);
  readonly total = signal(0);
  readonly items = signal<any[]>([]);
  readonly pages = computed(() => Math.ceil(this.total() / this.pageSize) || 1);

  constructor() {
    // Re-fetch when either the resource key or the page changes
    effect(() => {
      const r = this.resource();
      const p = this.page();
      if (r) void this.load(r.key, p);
    });
  }

  private async load(resourceKey: string, page: number): Promise<void> {
    const result = await this.apollo.query<{
      resourceItems: { total: number; items: string[] };
    }>({
      query: ITEMS_QUERY,
      variables: { resource: resourceKey, skip: page * this.pageSize, take: this.pageSize },
      fetchPolicy: 'network-only',
    }).toPromise();

    const data = result?.data?.resourceItems;
    if (!data) return;
    this.total.set(data.total);
    this.items.set(data.items.map((s: string) => {
      try { return JSON.parse(s); } catch { return {}; }
    }));
  }

  prev(): void { this.page.update((p: number) => Math.max(0, p - 1)); }
  next(): void { this.page.update((p: number) => Math.min(this.pages() - 1, p + 1)); }
}
