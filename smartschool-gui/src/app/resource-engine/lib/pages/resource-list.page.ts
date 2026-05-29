import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Apollo, QueryRef, gql } from 'apollo-angular';
import { TranslateModule } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
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

const RESOURCE_CHANGED_SUBSCRIPTION = gql`
  subscription ResourceChanged($resource: String!) {
    resourceChanged(resource: $resource) { event id }
  }
`;

type ItemsResult = { resourceItems: { total: number; items: any[] } };

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
  private readonly route      = inject(ActivatedRoute);
  private readonly registry   = inject(ResourceRegistryService);
  private readonly apollo     = inject(Apollo);
  private readonly destroyRef = inject(DestroyRef);

  readonly pageSize = 25;

  readonly params   = toSignal(this.route.paramMap, { requireSync: true });
  readonly resource = computed(() =>
    this.registry.getByPlural(this.params()!.get('resource') ?? ''));

  readonly page  = signal(0);

  // ── Live state driven by watchQuery ──────────────────────────────────────
  private readonly _data = signal<{ total: number; items: any[] } | null>(null);
  readonly total  = computed(() => this._data()?.total ?? 0);
  readonly items  = computed(() => this._data()?.items ?? []);
  readonly pages  = computed(() => Math.ceil(this.total() / this.pageSize) || 1);

  // ── Subscription handles ──────────────────────────────────────────────────
  private watchRef: QueryRef<ItemsResult> | null = null;
  private changeSub: Subscription | null = null;
  private currentResourceKey = '';

  constructor() {
    effect(() => {
      const r = this.resource();
      const p = this.page();
      if (!r) return;

      const vars = { resource: r.key, skip: p * this.pageSize, take: this.pageSize };

      if (r.key !== this.currentResourceKey) {
        // Resource segment changed — tear down and recreate everything
        this.changeSub?.unsubscribe();
        this.watchRef = null;
        this.currentResourceKey = r.key;

        // watchQuery keeps a live subscription to the Apollo cache;
        // 'cache-and-network' serves cached data immediately then refreshes
        this.watchRef = this.apollo.watchQuery<ItemsResult>({
          query: ITEMS_QUERY,
          variables: vars,
          fetchPolicy: 'cache-and-network',
        });

        // Bridge Apollo observable → signal; takeUntilDestroyed handles cleanup
        this.watchRef.valueChanges
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(({ data }: { data: ItemsResult }) => {
            if (data?.resourceItems) this._data.set(data.resourceItems);
          });

        // Real-time: backend publishes ResourceChangedEvent after any mutation;
        // any change on this resource triggers a refetch for all open clients
        this.changeSub = this.apollo
          .subscribe({ query: RESOURCE_CHANGED_SUBSCRIPTION, variables: { resource: r.key } })
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(() => this.watchRef?.refetch());
      } else {
        // Same resource — page changed, update variables only
        this.watchRef?.refetch(vars);
      }
    });
  }

  prev(): void { this.page.update((p: number) => Math.max(0, p - 1)); }
  next(): void { this.page.update((p: number) => Math.min(this.pages() - 1, p + 1)); }
}
