import { Component, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { MatSidenavModule, MatSidenav } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Subject, takeUntil, of, switchMap } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { ThemeService } from '../shared/services/theme.service';
import { ThemePickerComponent } from '../shared/components/theme-picker/theme-picker.component';
import { ToastrService } from 'ngx-toastr';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  condition?: () => boolean;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    ThemePickerComponent,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent implements OnInit, OnDestroy {
  @ViewChild('sidenav') sidenav!: MatSidenav;

  readonly themeService = inject(ThemeService);
  readonly authService  = inject(AuthService);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly router      = inject(Router);
  private readonly toastr      = inject(ToastrService);
  private readonly destroy$    = new Subject<void>();

  collapsed  = false;
  isMobile   = false;
  isLoading  = false;

  readonly navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'dashboard',  route: '/dashboard' },
    { label: 'Schools',   icon: 'school',     route: '/schools',    condition: () => !!this.authService.loggedInUser },
    { label: 'Students',  icon: 'people',     route: '/students',   condition: () => !!this.authService.loggedInUser?.studentId || !!this.authService.loggedInUser?.staffId || !!this.authService.loggedInUser?.teacherId || !!this.authService.loggedInUser?.principalId },
    { label: 'Teachers',  icon: 'person_pin', route: '/teachers',   condition: () => !!this.authService.loggedInUser?.staffId || !!this.authService.loggedInUser?.teacherId || !!this.authService.loggedInUser?.principalId },
    { label: 'Principals',icon: 'manage_accounts', route: '/principals', condition: () => !!this.authService.loggedInUser?.staffId || !!this.authService.loggedInUser?.teacherId || !!this.authService.loggedInUser?.principalId },
    { label: 'Users',     icon: 'group',      route: '/users',      condition: () => !!this.authService.loggedInUser },
  ];

  ngOnInit(): void {
    this.breakpoints.observe([Breakpoints.Handset, Breakpoints.TabletPortrait])
      .pipe(takeUntil(this.destroy$))
      .subscribe(result => {
        this.isMobile = result.matches;
        if (this.sidenav) {
          this.isMobile ? this.sidenav.close() : this.sidenav.open();
        }
      });

    this.authService.userLogged.pipe(
      takeUntil(this.destroy$),
      switchMap(res => {
        this.isLoading = true;
        if (res && this.authService.isLoggedIn()) {
          return this.authService.getUser(res);
        }
        return of(null);
      }),
    ).subscribe({
      next: () => { this.isLoading = false; },
      error: () => {
        this.isLoading = false;
        this.toastr.error('Could not load user', 'User');
      },
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  visibleNavItems(): NavItem[] {
    return this.navItems.filter(item => !item.condition || item.condition());
  }

  toggleCollapse(): void {
    this.collapsed = !this.collapsed;
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/auth', 'login']);
  }

  getInitials(name?: string | null): string {
    if (!name) return '?';
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }
}
