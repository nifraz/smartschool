import { CommonModule } from '@angular/common';
import { Component, Input, computed, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ResourceDescriptor } from '../models/resource.models';

/**
 * Minimal dynamic grid. Renders an HTML table from descriptor + items.
 * TODO: swap for AG Grid with server-side row model once metadata-driven
 * column definitions + filter/sort projection mapping is implemented.
 */
@Component({
  selector: 'ss-dynamic-grid',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    @if (resource) {
      <table class="ss-grid">
        <thead>
          <tr>
            @for (f of cols(); track f.key) {
              <th>{{ f.label }}</th>
            }
          </tr>
        </thead>
        <tbody>
          @for (row of items; track row.id) {
            <tr>
              @for (f of cols(); track f.key) {
                <td>{{ row[f.key] }}</td>
              }
            </tr>
          } @empty {
            <tr><td [attr.colspan]="cols().length">{{ 'grid.empty' | translate }}</td></tr>
          }
        </tbody>
      </table>
    }
  `,
  styles: [`
    .ss-grid { width: 100%; border-collapse: collapse; }
    .ss-grid th, .ss-grid td { padding: .5rem .75rem; border-bottom: 1px solid #eee; text-align: start; }
  `],
})
export class DynamicGridComponent {
  @Input({ required: true }) resource!: ResourceDescriptor;
  @Input() items: any[] = [];

  readonly cols = computed(() =>
    this.resource.fields
      .filter(f => f.inGrid && f.type !== 'RefCollection')
      .sort((a, b) => a.sortOrder - b.sortOrder));
}
