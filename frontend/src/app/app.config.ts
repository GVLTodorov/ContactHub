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
    // The router state lives in the store so selectors can read route params such as `:id`.
    provideStore({ router: routerReducer }),
    provideRouterStore(),
    provideState(contactsFeature),
    provideEffects(ContactsEffects),
    ...environment.providers,
    // App-wide, so toasts raised from effects show in the layout's <p-toast>.
    MessageService,
    providePrimeNG({
      theme: {
        preset: Aura,
        // PrimeNG styles go in their own CSS layer below Tailwind's utilities (see styles.css),
        // so a Tailwind class always wins over the component theme.
        options: { cssLayer: { name: 'primeng', order: 'theme, base, primeng' } },
      },
    }),
  ],
};
