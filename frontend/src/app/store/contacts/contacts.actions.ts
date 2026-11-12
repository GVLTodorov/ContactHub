import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { ApiError, Contact, ContactInput } from '../../api/contacts-api';

export const ContactsPageActions = createActionGroup({
  source: 'Contacts Page',
  events: {
    Opened: emptyProps(),
    Retry: emptyProps(),
    'Search Changed': props<{ query: string }>(),
    'Form Opened': emptyProps(),
    'Create Submitted': props<{ input: ContactInput }>(),
    'Detail Opened': props<{ id: string }>(),
    'Update Submitted': props<{ id: string; input: ContactInput }>(),
    'Delete Contact': props<{ id: string }>(),
  },
});

export const ContactsApiActions = createActionGroup({
  source: 'Contacts API',
  events: {
    'Load Success': props<{ contacts: Contact[] }>(),
    'Load Failure': props<{ error: ApiError }>(),
    'Create Success': props<{ contact: Contact }>(),
    'Create Failure': props<{ error: ApiError }>(),
    'Load One Success': props<{ contact: Contact }>(),
    'Load One Failure': props<{ error: ApiError; notFound: boolean }>(),
    'Update Success': props<{ contact: Contact }>(),
    'Update Failure': props<{ error: ApiError }>(),
    'Delete Contact Success': props<{ id: string }>(),
    'Delete Contact Failure': props<{ error: ApiError }>(),
  },
});
