import { createEntityAdapter, EntityAdapter, EntityState } from '@ngrx/entity';
import { getRouterSelectors } from '@ngrx/router-store';
import { createFeature, createReducer, createSelector, on } from '@ngrx/store';
import { Contact } from '../../api/contacts-api';
import { ContactsApiActions, ContactsPageActions } from './contacts.actions';

export type Status = 'idle' | 'loading' | 'success' | 'error';

export type DetailStatus = Status | 'notFound';

const { selectRouteParam } = getRouterSelectors();

export interface ContactsState extends EntityState<Contact> {
  loadStatus: Status;
  /** Status of loading a single contact; 404 is tracked as `notFound`, not as an error. */
  detailStatus: DetailStatus;
  detailError: string | null;
  saveStatus: Status;
  error: string | null;
  fieldErrors: Record<string, string[]>;
  /** Search term; kept in the store so it survives navigation. */
  query: string;
}

const normalize = (v: string): string => v.toLowerCase().replace(/\s+/g, '');

export const matchesQuery = (c: Contact, query: string): boolean => {
  const q = normalize(query);
  if (!q) return true;
  return [c.firstName, c.surname, c.firstName + c.surname, c.phoneNumber, c.iban, c.ibanFormatted, c.address.city].some(
    (f) => normalize(f ?? '').includes(q),
  );
};

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
    on(ContactsPageActions.opened, ContactsPageActions.retry, (state) => ({
      ...state,
      loadStatus: 'loading' as const,
      error: null,
    })),
    on(ContactsPageActions.searchChanged, (state, { query }) => ({ ...state, query })),
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
    on(ContactsPageActions.detailOpened, (state) => ({
      ...state,
      detailStatus: 'loading' as const,
      detailError: null,
    })),
    on(ContactsApiActions.loadOneSuccess, (state, { contact }) =>
      adapter.upsertOne(contact, { ...state, detailStatus: 'success' as const, detailError: null }),
    ),
    on(ContactsApiActions.loadOneFailure, (state, { error, notFound }) => ({
      ...state,
      detailStatus: notFound ? ('notFound' as const) : ('error' as const),
      detailError: notFound ? null : error.message,
    })),
    on(ContactsPageActions.updateSubmitted, (state) => ({
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
      selectFilteredContacts: createSelector(selectAllContacts, selectQuery, (contacts, query) =>
        query.trim() ? contacts.filter((c) => matchesQuery(c, query)) : contacts,
      ),
      selectSelectedContact: createSelector(
        selectRouteParam('id'),
        selectEntities,
        (id, entities) => (id ? (entities[id] ?? null) : null),
      ),
      selectIsEmpty: createSelector(
        selectLoadStatus,
        selectAllContacts,
        (status, contacts) => status === 'success' && contacts.length === 0,
      ),
    };
  },
});
