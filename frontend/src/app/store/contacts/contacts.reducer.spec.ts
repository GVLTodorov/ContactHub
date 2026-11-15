import { Contact, ContactInput } from '../../api/contacts-api';
import {
  ContactDetailPageActions,
  ContactListPageActions,
  ContactsApiActions,
  EditContactPageActions,
  NewContactPageActions,
} from './contacts.actions';
import { contactsFeature, initialState } from './contacts.reducer';

const make = (id: string, firstName: string, surname: string): Contact => ({
  id,
  firstName,
  surname,
  dateOfBirth: '1990-01-01',
  address: { street: 's', postalCode: 'p', city: 'c', country: 'x' },
  phoneNumber: '+4912345678',
  iban: 'DE89370400440532013000',
  ibanFormatted: 'DE89 3704 0044 0532 0130 00',
  createdAt: '',
  updatedAt: '',
  version: 1,
});

describe('contacts reducer', () => {
  const reducer = contactsFeature.reducer;

  it('stores the search query and keeps it across loads', () => {
    let s = reducer(initialState, ContactListPageActions.searchChanged({ query: 'bob' }));
    expect(s.query).toBe('bob');
    s = reducer(s, ContactListPageActions.opened());
    s = reducer(s, ContactsApiActions.loadSuccess({ contacts: [] }));
    expect(s.query).toBe('bob');
  });

  it('sets loading on open', () => {
    const s = reducer(initialState, ContactListPageActions.opened());
    expect(s.loadStatus).toBe('loading');
  });

  it('stores contacts sorted by surname then first name', () => {
    const s = reducer(
      initialState,
      ContactsApiActions.loadSuccess({ contacts: [make('1', 'Zed', 'Brown'), make('2', 'Amy', 'Brown'), make('3', 'Bob', 'Adams')] }),
    );
    expect(s.loadStatus).toBe('success');
    expect(s.ids).toEqual(['3', '2', '1']);
  });

  it('records load failure', () => {
    const s = reducer(initialState, ContactsApiActions.loadFailure({ error: { message: 'boom', fieldErrors: {} } }));
    expect(s.loadStatus).toBe('error');
    expect(s.error).toBe('boom');
  });

  it('tracks create lifecycle', () => {
    let s = reducer(initialState, NewContactPageActions.createSubmitted({ input: {} as ContactInput }));
    expect(s.saveStatus).toBe('loading');
    s = reducer(s, ContactsApiActions.createFailure({ error: { message: 'bad', fieldErrors: { iban: ['x'] } } }));
    expect(s.saveStatus).toBe('error');
    expect(s.fieldErrors).toEqual({ iban: ['x'] });
    s = reducer(s, ContactsApiActions.createSuccess({ contact: make('1', 'A', 'B') }));
    expect(s.saveStatus).toBe('success');
    expect(s.ids).toEqual(['1']);
  });

  it('resets save state when the form opens', () => {
    const failed = reducer(initialState, ContactsApiActions.createFailure({ error: { message: 'bad', fieldErrors: { a: ['b'] } } }));
    const s = reducer(failed, NewContactPageActions.opened());
    expect(s.saveStatus).toBe('idle');
    expect(s.fieldErrors).toEqual({});
  });

  it('resets save state and starts the detail load when the edit page opens', () => {
    const failed = reducer(initialState, ContactsApiActions.updateFailure({ error: { message: 'bad', fieldErrors: { a: ['b'] } } }));
    const s = reducer(failed, EditContactPageActions.opened({ id: '1' }));
    expect(s.saveStatus).toBe('idle');
    expect(s.error).toBeNull();
    expect(s.fieldErrors).toEqual({});
    expect(s.detailStatus).toBe('loading');
  });

  it('tracks the detail load, upserting the contact', () => {
    let s = reducer(initialState, ContactDetailPageActions.opened({ id: '1' }));
    expect(s.detailStatus).toBe('loading');
    s = reducer(s, ContactsApiActions.loadOneSuccess({ contact: make('1', 'A', 'B') }));
    expect(s.detailStatus).toBe('success');
    expect(s.ids).toEqual(['1']);
    s = reducer(s, ContactsApiActions.loadOneSuccess({ contact: make('1', 'Z', 'B') }));
    expect(s.ids).toEqual(['1']);
    expect(s.entities['1']?.firstName).toBe('Z');
  });

  it('tracks not-found separately from other detail errors', () => {
    const error = { message: 'nope', fieldErrors: {} };
    const nf = reducer(initialState, ContactsApiActions.loadOneFailure({ error, notFound: true }));
    expect(nf.detailStatus).toBe('notFound');
    expect(nf.detailError).toBeNull();
    const err = reducer(initialState, ContactsApiActions.loadOneFailure({ error, notFound: false }));
    expect(err.detailStatus).toBe('error');
    expect(err.detailError).toBe('nope');
  });

  it('removes a contact on delete success', () => {
    let s = reducer(
      initialState,
      ContactsApiActions.loadSuccess({ contacts: [make('1', 'A', 'B'), make('2', 'C', 'D')] }),
    );
    s = reducer(s, ContactsApiActions.deleteContactSuccess({ id: '1' }));
    expect(s.ids).toEqual(['2']);
    s = reducer(s, ContactsApiActions.deleteContactFailure({ error: { message: 'x', fieldErrors: {} } }));
    expect(s.ids).toEqual(['2']);
  });

  it('tracks update lifecycle', () => {
    let s = reducer(initialState, EditContactPageActions.updateSubmitted({ id: '1', version: 3, input: {} as ContactInput }));
    expect(s.saveStatus).toBe('loading');
    s = reducer(s, ContactsApiActions.updateFailure({ error: { message: 'bad', fieldErrors: { 'address.city': ['x'] } } }));
    expect(s.saveStatus).toBe('error');
    expect(s.fieldErrors).toEqual({ 'address.city': ['x'] });
    s = reducer(s, ContactsApiActions.updateSuccess({ contact: make('1', 'A', 'B') }));
    expect(s.saveStatus).toBe('success');
    expect(s.ids).toEqual(['1']);
  });
});
