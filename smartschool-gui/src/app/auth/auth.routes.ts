import { Routes } from '@angular/router';
import { AuthComponent } from './auth.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { VerifyComponent } from './verify/verify.component';
import { accountGuard } from './guards/account.guard';

export const routes: Routes = [
  {
    path: '',
    component: AuthComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'login' },
      { path: 'login',    title: 'Login | SmartSchool',    component: LoginComponent,    canActivate: [accountGuard] },
      { path: 'register', title: 'Register | SmartSchool', component: RegisterComponent, canActivate: [accountGuard] },
      { path: 'verify',   title: 'Verify | SmartSchool',   component: VerifyComponent,   canActivate: [accountGuard] },
    ],
  },
];
