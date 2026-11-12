import { createEntityAdapter, EntityAdapter, EntityState } from '@ngrx/entity';
import { createFeature, createReducer, createSelector, on } from '@ngrx/store';
import { Contact } from '../../api/contacts-api';
import { ContactsApiActions, ContactsPageActions } from './contacts.actions';

export type Status = 'idle' | 'loading' | 'success' | 'error';

export interface ContactsState extends EntityState<Contact> {
  loadStatus: Status;
  saveStatus: Status;
  error: string | null;
  fieldErrors: Record<string, string[]>;
}

export const adapter: EntityAdapter<Contact> = createEntityAdapter<Contact>({
  sortComparer: (a, b) =>
    a.surname.localeCompare(b.surname, undefined, { sensitivity: 'base' }) ||
    a.firstName.localeCompare(b.firstName, undefined, { sensitivity: 'base' }),
});

export const initialState: ContactsState = adapter.getInitialState({
  loadStatus: 'idle',
  saveStatus: 'idle',
  error: null,
  fieldErrors: {},
});

export const contactsFeature = createFeature({
  name: 'contacts',
  reducer: createReducer(
    initialState,
    on(ContactsPageActions.opened, ContactsPageActions.retry, (state) => ({
      ...state,
      loadStatus: 'loading' as const,
      error: null,
    })),
    on(ContactsApiActions.loadSuccess, (state, { contacts }) =>
      adapter.setAll(contacts, { ...state, loadStatus: 'success' as const, error: null }),
    ),
    on(ContactsApiActions.loadFailure, (state, { error }) => ({
      ...state,
      loadStatus: 'error' as const,
      error: error.message,
    })),
    on(ContactsPageActions.formOpened, (state) => ({
      ...state,
      saveStatus: 'idle' as const,
      error: null,
      fieldErrors: {},
    })),
    on(ContactsPageActions.createSubmitted, (state) => ({
      ...state,
      saveStatus: 'loading' as const,
      error: null,
      fieldErrors: {},
    })),
    on(ContactsApiActions.createSuccess, (state, { contact }) =>
      adapter.addOne(contact, { ...state, saveStatus: 'success' as const }),
    ),
    on(ContactsApiActions.createFailure, (state, { error }) => ({
      ...state,
      saveStatus: 'error' as const,
      error: error.message,
      fieldErrors: error.fieldErrors,
    })),
  ),
  extraSelectors: ({ selectContactsState, selectLoadStatus }) => {
    const selectAllContacts = createSelector(selectContactsState, adapter.getSelectors().selectAll);
    return {
      selectAllContacts,
      selectIsEmpty: createSelector(
        selectLoadStatus,
        selectAllContacts,
        (status, contacts) => status === 'success' && contacts.length === 0,
      ),
    };
  },
});
