import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-skeleton',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="ss-skeleton"
         [style.width]="width"
         [style.height]="height"
         [style.border-radius]="radius"
         [style.margin-bottom]="mb">
    </div>
  `,
})
export class SkeletonComponent {
  @Input() width  = '100%';
  @Input() height = '1rem';
  @Input() radius = 'var(--ss-radius)';
  @Input() mb     = '0';
}
