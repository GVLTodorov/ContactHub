import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { ApiError, Contact, ContactInput } from '../../api/contacts-api';

// One action group per source, so the devtools log shows where each event came from.

export const ContactListPageActions = createActionGroup({
  source: 'Contact List Page',
  events: {
    Opened: emptyProps(),
    Retry: emptyProps(),
    'Search Changed': props<{ query: string }>(),
    'Delete Confirmed': props<{ id: string }>(),
  },
});

export const ContactDetailPageActions = createActionGroup({
  source: 'Contact Detail Page',
  events: {
    Opened: props<{ id: string }>(),
    Retry: props<{ id: string }>(),
    'Delete Confirmed': props<{ id: string }>(),
  },
});

export const NewContactPageActions = createActionGroup({
  source: 'New Contact Page',
  events: {
    Opened: emptyProps(),
    'Create Submitted': props<{ input: ContactInput }>(),
  },
});

export const EditContactPageActions = createActionGroup({
  source: 'Edit Contact Page',
  events: {
    Opened: props<{ id: string }>(),
    /** `version` is the one the form was filled from; the API answers 409 if it is stale. */
    'Update Submitted': props<{ id: string; version: number; input: ContactInput }>(),
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
