import { EnvironmentProviders } from '@angular/core';
import { provideStoreDevtools } from '@ngrx/store-devtools';

export const environment = {
  production: false,
  apiBaseUrl: '/api',
  providers: [provideStoreDevtools({ maxAge: 25 })] as EnvironmentProviders[],
};
