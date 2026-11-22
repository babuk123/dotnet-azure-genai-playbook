import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AccountsComponent } from './accounts.component';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideRouter([
      { path: '', component: AccountsComponent },
      { path: 'reports', loadComponent: () => import('./accounts.component').then(m => m.AccountsComponent) }, // lazy load
    ]),
  ],
};
