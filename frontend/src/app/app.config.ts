import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideRouterStore, routerReducer } from '@ngrx/router-store';
import { provideState, provideStore } from '@ngrx/store';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeuix/themes/aura';
import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { ContactsEffects } from './store/contacts/contacts.effects';
import { contactsFeature } from './store/contacts/contacts.reducer';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(),
    provideStore({ router: routerReducer }),
    provideRouterStore(),
    provideState(contactsFeature),
    provideEffects(ContactsEffects),
    ...environment.providers,
    MessageService,
    providePrimeNG({
      theme: {
        preset: Aura,
        options: { cssLayer: { name: 'primeng', order: 'theme, base, primeng' } },
      },
    }),
  ],
};
