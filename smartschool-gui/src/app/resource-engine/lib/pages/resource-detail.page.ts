import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Apollo, gql } from 'apollo-angular';
import { DynamicFormComponent } from '../components/dynamic-form.component';
import { ResourceRegistryService } from '../services/resource-registry.service';

const ITEM_QUERY = gql`
  query ResourceItem($resource: String!, $id: Long!) {
    resourceItem(resource: $resource, id: $id)
  }
`;

const CREATE_MUTATION = gql`
  mutation CreateResource($resource: String!, $input: Any) {
    createResource(resource: $resource, input: $input)
  }
`;

const UPDATE_MUTATION = gql`
  mutation UpdateResource($resource: String!, $id: Long!, $input: Any) {
    updateResource(resource: $resource, id: $id, input: $input)
  }
`;

@Component({
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, DynamicFormComponent],
  template: `
    @if (resource(); as r) {
      <header class="page-head">
        <h1>{{ r.label }} — {{ isNew() ? ('action.create' | translate) : ('action.edit' | translate) }}</h1>
        <a class="btn-back" [routerLink]="['/', r.plural]">{{ 'action.back' | translate }}</a>
      </header>

      @if (error(); as msg) {
        <p class="error">{{ msg }}</p>
      }

      @if (loading()) {
        <p>{{ 'common.loading' | translate }}</p>
      } @else {
        <ss-dynamic-form [resource]="r" [value]="itemValue()" (save)="onSave($event)" />
      }

      @if (r.relations.length) {
        <section class="relations">
          <h2>{{ 'detail.related' | translate }}</h2>
          <ul>
            @for (rel of r.relations; track rel.key) {
              @if (rel.showInDetail && rel.kind === 'many') {
                <li>
                  <a [routerLink]="['/', rel.targetResource + 's']">{{ rel.label || rel.key }}</a>
                </li>
              }
            }
          </ul>
        </section>
      }
    } @else {
      <p>{{ 'common.loading' | translate }}</p>
    }
  `,
  styles: [`
    .page-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .btn-back { color: #1976d2; }
    .relations { margin-top: 2rem; }
    .error { color: #d32f2f; margin-bottom: .75rem; }
  `],
})
export class ResourceDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly registry = inject(ResourceRegistryService);
  private readonly apollo = inject(Apollo);

  readonly params = toSignal(this.route.paramMap, { requireSync: true });
  readonly resource = computed(() =>
    this.registry.getByPlural(this.params()!.get('resource') ?? ''));
  readonly isNew = computed(() => this.params()!.get('id') === 'new');

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly itemValue = signal<Record<string, unknown>>({});

  constructor() {
    effect(() => {
      const r = this.resource();
      const idStr = this.params()!.get('id');
      if (r && idStr && idStr !== 'new') {
        const id = Number(idStr);
        if (!isNaN(id)) void this.loadItem(r.key, id);
      }
    });
  }

  private async loadItem(resourceKey: string, id: number): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const result = await this.apollo.query<{ resourceItem: string | null }>({
        query: ITEM_QUERY,
        variables: { resource: resourceKey, id },
        fetchPolicy: 'network-only',
      }).toPromise();

      const raw = result?.data?.resourceItem;
      if (raw) {
        try { this.itemValue.set(JSON.parse(raw)); } catch { /* ignore parse errors */ }
      }
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to load record.');
    } finally {
      this.loading.set(false);
    }
  }

  async onSave(value: Record<string, unknown>): Promise<void> {
    const r = this.resource();
    if (!r) return;

    this.error.set(null);
    const idStr = this.params()!.get('id');
    const isNew = idStr === 'new';

    try {
      if (isNew) {
        await this.apollo.mutate({
          mutation: CREATE_MUTATION,
          variables: { resource: r.key, input: value },
        }).toPromise();
      } else {
        await this.apollo.mutate({
          mutation: UPDATE_MUTATION,
          variables: { resource: r.key, id: Number(idStr), input: value },
        }).toPromise();
      }
      this.router.navigate(['/', r.plural]);
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to save record.');
    }
  }
}
