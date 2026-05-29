import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DynamicFormComponent } from '../components/dynamic-form.component';
import { ResourceRegistryService } from '../services/resource-registry.service';

/**
 * Generic detail/edit page:  /:resource/:id
 * `:id === "new"` switches to create mode.
 * TODO: load entity via generic `item(resource, id)` query;
 * render related-resource tabs based on `resource.relations`.
 */
@Component({
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, DynamicFormComponent],
  template: `
    @if (resource(); as r) {
      <header class="page-head">
        <h1>{{ r.label }} — {{ isNew() ? ('action.create' | translate) : ('action.edit' | translate) }}</h1>
        <a class="btn-back" [routerLink]="['/', r.plural]">{{ 'action.back' | translate }}</a>
      </header>
      <ss-dynamic-form [resource]="r" (save)="onSave($event)" />

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
  `],
})
export class ResourceDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly registry = inject(ResourceRegistryService);

  readonly params = toSignal(this.route.paramMap, { requireSync: true });
  readonly resource = computed(() =>
    this.registry.getByPlural(this.params()!.get('resource') ?? ''));
  readonly isNew = computed(() => this.params()!.get('id') === 'new');

  onSave(value: Record<string, any>): void {
    // TODO: call generic createResource / updateResource mutation.
    console.log('save', value);
    const r = this.resource();
    if (r) this.router.navigate(['/', r.plural]);
  }
}
