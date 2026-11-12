import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { ApiError, Contact, ContactInput } from '../../api/contacts-api';

export const ContactsPageActions = createActionGroup({
  source: 'Contacts Page',
  events: {
    Opened: emptyProps(),
    Retry: emptyProps(),
    'Form Opened': emptyProps(),
    'Create Submitted': props<{ input: ContactInput }>(),
  },
});

export const ContactsApiActions = createActionGroup({
  source: 'Contacts API',
  events: {
    'Load Success': props<{ contacts: Contact[] }>(),
    'Load Failure': props<{ error: ApiError }>(),
    'Create Success': props<{ contact: Contact }>(),
    'Create Failure': props<{ error: ApiError }>(),
  },
});
