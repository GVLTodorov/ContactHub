import { EnvironmentProviders } from '@angular/core';

export const environment = {
  production: true,
  apiBaseUrl: '/api',
  // Dev-only providers (e.g. ngrx store devtools); empty in production to keep the bundle small.
  providers: [] as EnvironmentProviders[],
};
