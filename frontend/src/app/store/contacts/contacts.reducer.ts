import { createEntityAdapter, EntityAdapter, EntityState } from '@ngrx/entity';
import { getRouterSelectors } from '@ngrx/router-store';
import { createFeature, createReducer, createSelector, on } from '@ngrx/store';
import { Contact } from '../../api/contacts-api';
import {
  ContactDetailPageActions,
  ContactListPageActions,
  ContactsApiActions,
  EditContactPageActions,
  NewContactPageActions,
} from './contacts.actions';

/** Progress of one kind of request: `idle` until the page starts it. */
export type Status = 'idle' | 'loading' | 'success' | 'error';

export type DetailStatus = Status | 'notFound';

const { selectRouteParam } = getRouterSelectors();

export interface ContactsState extends EntityState<Contact> {
  /** Status of loading the whole list. */
  loadStatus: Status;
  /** Status of loading a single contact; 404 is tracked as `notFound`, not as an error. */
  detailStatus: DetailStatus;
  detailError: string | null;
  /** Status of the current create or update; reset each time the new or edit page opens. */
  saveStatus: Status;
  /** Message of the last failed list load or save, for the page's error banner. */
  error: string | null;
  /** Server validation errors of the last failed save, shown under the matching form fields. */
  fieldErrors: Record<string, string[]>;
  /** Search term; kept in the store so it survives navigation. */
  query: string;
}

/** Lower-cases and drops all whitespace, so "NL91 ABNA" matches a compact IBAN. */
const normalize = (v: string): string => v.toLowerCase().replace(/\s+/g, '');

/**
 * Whether a contact matches the search box. Case and spaces are ignored; it checks name, phone,
 * IBAN and city. First name + surname is also matched as one string, so "john smith" finds
 * John Smith.
 */
export const matchesQuery = (c: Contact, query: string): boolean => {
  const q = normalize(query);
  if (!q) return true;
  return [c.firstName, c.surname, c.firstName + c.surname, c.phoneNumber, c.iban, c.ibanFormatted, c.address.city].some(
    (f) => normalize(f ?? '').includes(q),
  );
};

/** Keeps contacts keyed by id and always sorted by surname, then first name, ignoring case and accents. */
export const adapter: EntityAdapter<Contact> = createEntityAdapter<Contact>({
  sortComparer: (a, b) =>
    a.surname.localeCompare(b.surname, undefined, { sensitivity: 'base' }) ||
    a.firstName.localeCompare(b.firstName, undefined, { sensitivity: 'base' }),
});

export const initialState: ContactsState = adapter.getInitialState({
  loadStatus: 'idle',
  detailStatus: 'idle',
  detailError: null,
  saveStatus: 'idle',
  error: null,
  fieldErrors: {},
  query: '',
});

export const contactsFeature = createFeature({
  name: 'contacts',
  reducer: createReducer(
    initialState,
    on(ContactListPageActions.opened, ContactListPageActions.retry, (state) => ({
      ...state,
      loadStatus: 'loading' as const,
      error: null,
    })),
    on(ContactListPageActions.searchChanged, (state, { query }) => ({ ...state, query })),
    on(ContactsApiActions.loadSuccess, (state, { contacts }) =>
      adapter.setAll(contacts, { ...state, loadStatus: 'success' as const, error: null }),
    ),
    on(ContactsApiActions.loadFailure, (state, { error }) => ({
      ...state,
      loadStatus: 'error' as const,
      error: error.message,
    })),
    on(NewContactPageActions.opened, (state) => ({
      ...state,
      saveStatus: 'idle' as const,
      error: null,
      fieldErrors: {},
    })),
    // The edit page both saves and (re)loads the contact, so it resets both statuses.
    on(EditContactPageActions.opened, (state) => ({
      ...state,
      saveStatus: 'idle' as const,
      error: null,
      fieldErrors: {},
      detailStatus: 'loading' as const,
      detailError: null,
    })),
    on(NewContactPageActions.createSubmitted, (state) => ({
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
    on(ContactDetailPageActions.opened, ContactDetailPageActions.retry, (state) => ({
      ...state,
      detailStatus: 'loading' as const,
      detailError: null,
    })),
    // Upsert, not add: the contact may already be cached from the list and must be refreshed.
    on(ContactsApiActions.loadOneSuccess, (state, { contact }) =>
      adapter.upsertOne(contact, { ...state, detailStatus: 'success' as const, detailError: null }),
    ),
    on(ContactsApiActions.loadOneFailure, (state, { error, notFound }) => ({
      ...state,
      detailStatus: notFound ? ('notFound' as const) : ('error' as const),
      detailError: notFound ? null : error.message,
    })),
    on(EditContactPageActions.updateSubmitted, (state) => ({
      ...state,
      saveStatus: 'loading' as const,
      error: null,
      fieldErrors: {},
    })),
    on(ContactsApiActions.updateSuccess, (state, { contact }) =>
      adapter.upsertOne(contact, { ...state, saveStatus: 'success' as const }),
    ),
    on(ContactsApiActions.updateFailure, (state, { error }) => ({
      ...state,
      saveStatus: 'error' as const,
      error: error.message,
      fieldErrors: error.fieldErrors,
    })),
    on(ContactsApiActions.deleteContactSuccess, (state, { id }) => adapter.removeOne(id, state)),
  ),
  extraSelectors: ({ selectContactsState, selectLoadStatus, selectEntities, selectQuery }) => {
    const selectAllContacts = createSelector(selectContactsState, adapter.getSelectors().selectAll);
    return {
      selectAllContacts,
      /** The sorted list narrowed by the search box. */
      selectFilteredContacts: createSelector(selectAllContacts, selectQuery, (contacts, query) =>
        query.trim() ? contacts.filter((c) => matchesQuery(c, query)) : contacts,
      ),
      /** The cached contact for the `:id` in the current URL, or `null` while it is not loaded. */
      selectSelectedContact: createSelector(
        selectRouteParam('id'),
        selectEntities,
        (id, entities) => (id ? (entities[id] ?? null) : null),
      ),
      /** True only once the list has loaded and has no contacts, so the empty state never flashes while loading. */
      selectIsEmpty: createSelector(
        selectLoadStatus,
        selectAllContacts,
        (status, contacts) => status === 'success' && contacts.length === 0,
      ),
    };
  },
});
