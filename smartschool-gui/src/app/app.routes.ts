import { Routes } from '@angular/router';
import { bootstrapResolver, resourceExistsMatch } from './resource-engine';

/**
 * The ENTIRE route table. Two dynamic routes cover every entity.
 * Auth flows are lazy-loaded; everything else is metadata-driven.
 */
export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./auth/auth.routes').then(m => m.routes),
  },
  {
    path: '',
    resolve: { _bootstrap: bootstrapResolver },
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./dashboard/dashboard.component').then(m => m.DashboardComponent),
      },
      // ── Every resource, dynamically ──
      {
        path: ':resource',
        canMatch: [resourceExistsMatch],
        loadComponent: () =>
          import('./resource-engine').then(m => m.ResourceListPage),
      },
      {
        path: ':resource/:id',
        canMatch: [resourceExistsMatch],
        loadComponent: () =>
          import('./resource-engine').then(m => m.ResourceDetailPage),
      },
      {
        path: '**',
        loadComponent: () =>
          import('./shared/not-found/not-found.component').then(m => m.NotFoundComponent),
      },
    ],
  },
];
