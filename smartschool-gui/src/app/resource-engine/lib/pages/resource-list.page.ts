import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Apollo, gql } from 'apollo-angular';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { DynamicGridComponent } from '../components/dynamic-grid.component';
import { ResourceRegistryService } from '../services/resource-registry.service';

/**
 * Generic list page for any resource:  /:resource
 * Issues a generic `items(resource, skip, take)` query against the backend.
 * TODO: replace with a proper paged query and a permission-aware action bar.
 */
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
    } @else {
      <p>{{ 'common.loading' | translate }}</p>
    }
  `,
  styles: [`
    .page-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .btn { padding: .5rem .9rem; border-radius: .375rem; background: #1976d2; color: white; text-decoration: none; }
  `],
})
export class ResourceListPage {
  private readonly route = inject(ActivatedRoute);
  private readonly registry = inject(ResourceRegistryService);
  private readonly apollo = inject(Apollo);

  readonly params = toSignal(this.route.paramMap, { requireSync: true });
  readonly resource = computed(() =>
    this.registry.getByPlural(this.params()!.get('resource') ?? ''));

  readonly items = signal<any[]>([]);

  constructor() {
    // Naïve: refetch whenever the resource changes.
    queueMicrotask(() => this.load());
  }

  private async load(): Promise<void> {
    const r = this.resource();
    if (!r) return;
    // TODO: backend generic query – placeholder echoes empty list.
    this.items.set([]);
  }
}
