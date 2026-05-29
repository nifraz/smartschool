import { Directive, ElementRef, Input, OnInit, inject } from '@angular/core';
import { PermissionService } from '../services/permission.service';

/**
 * Hides the host element unless the user has the given permission code.
 * Usage: <button ssHasPermission="school:update">Edit</button>
 */
@Directive({
  selector: '[ssHasPermission]',
  standalone: true,
})
export class HasPermissionDirective implements OnInit {
  @Input('ssHasPermission') code = '';
  private readonly el = inject(ElementRef<HTMLElement>);
  private readonly perms = inject(PermissionService);

  ngOnInit(): void {
    if (!this.code) return;
    if (!this.perms.has(this.code)) this.el.nativeElement.style.display = 'none';
  }
}
