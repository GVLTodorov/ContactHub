import { Contact, ContactInput } from '../../api/contacts-api';
import { ContactsApiActions, ContactsPageActions } from './contacts.actions';
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
});

describe('contacts reducer', () => {
  const reducer = contactsFeature.reducer;

  it('sets loading on open', () => {
    const s = reducer(initialState, ContactsPageActions.opened());
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
    let s = reducer(initialState, ContactsPageActions.createSubmitted({ input: {} as ContactInput }));
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
    const s = reducer(failed, ContactsPageActions.formOpened());
    expect(s.saveStatus).toBe('idle');
    expect(s.fieldErrors).toEqual({});
  });

  it('tracks the detail load, upserting the contact', () => {
    let s = reducer(initialState, ContactsPageActions.detailOpened({ id: '1' }));
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
    let s = reducer(initialState, ContactsPageActions.updateSubmitted({ id: '1', input: {} as ContactInput }));
    expect(s.saveStatus).toBe('loading');
    s = reducer(s, ContactsApiActions.updateFailure({ error: { message: 'bad', fieldErrors: { 'address.city': ['x'] } } }));
    expect(s.saveStatus).toBe('error');
    expect(s.fieldErrors).toEqual({ 'address.city': ['x'] });
    s = reducer(s, ContactsApiActions.updateSuccess({ contact: make('1', 'A', 'B') }));
    expect(s.saveStatus).toBe('success');
    expect(s.ids).toEqual(['1']);
  });
});
