import { EnvironmentProviders } from '@angular/core';

export const environment = {
  production: true,
  // Dev-only providers (e.g. ngrx store devtools); empty in production to keep the bundle small.
  providers: [] as EnvironmentProviders[],
};
