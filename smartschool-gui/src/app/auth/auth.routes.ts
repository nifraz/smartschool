import { Routes } from '@angular/router';
import { accountGuard } from './guards/account.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./auth.component').then(m => m.AuthComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'login' },
      {
        path: 'login',
        title: 'Login | SmartSchool',
        canActivate: [accountGuard],
        loadComponent: () => import('./login/login.component').then(m => m.LoginComponent),
      },
      {
        path: 'register',
        title: 'Register | SmartSchool',
        canActivate: [accountGuard],
        loadComponent: () => import('./register/register.component').then(m => m.RegisterComponent),
      },
      {
        path: 'verify',
        title: 'Verify | SmartSchool',
        canActivate: [accountGuard],
        loadComponent: () => import('./verify/verify.component').then(m => m.VerifyComponent),
      },
    ],
  },
];
