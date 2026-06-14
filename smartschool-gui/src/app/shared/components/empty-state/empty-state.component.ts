import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div class="empty-state">
      <mat-icon class="empty-icon">{{ icon }}</mat-icon>
      <h3 class="empty-title">{{ title }}</h3>
      <p class="empty-message">{{ message }}</p>
      <ng-content />
    </div>
  `,
  styles: [`
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 3rem 1.5rem;
      text-align: center;
      color: var(--ss-text-muted);
    }
    .empty-icon {
      font-size: 3rem;
      width: 3rem;
      height: 3rem;
      opacity: 0.35;
      margin-bottom: 1rem;
    }
    .empty-title {
      font-size: 1rem;
      font-weight: 600;
      color: var(--ss-text);
      margin: 0 0 0.5rem;
    }
    .empty-message {
      font-size: 0.875rem;
      margin: 0 0 1.25rem;
    }
  `],
})
export class EmptyStateComponent {
  @Input() icon    = 'inbox';
  @Input() title   = 'Nothing here yet';
  @Input() message = '';
}
