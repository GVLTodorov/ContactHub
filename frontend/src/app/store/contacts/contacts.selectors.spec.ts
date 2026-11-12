import { Contact } from '../../api/contacts-api';
import { ContactsApiActions } from './contacts.actions';
import { contactsFeature, initialState } from './contacts.reducer';

describe('contacts selectors', () => {
  const appState = (contacts: Contact[] | null) => ({
    contacts: contacts
      ? contactsFeature.reducer(initialState, ContactsApiActions.loadSuccess({ contacts }))
      : initialState,
  });

  it('selects all contacts', () => {
    const c = { id: '1', firstName: 'A', surname: 'B' } as Contact;
    expect(contactsFeature.selectAllContacts(appState([c]))).toEqual([c]);
  });

  it('is empty only after a successful empty load', () => {
    expect(contactsFeature.selectIsEmpty(appState(null))).toBe(false);
    expect(contactsFeature.selectIsEmpty(appState([]))).toBe(true);
  });

  it('selects statuses and errors', () => {
    const s = appState(null);
    expect(contactsFeature.selectLoadStatus(s)).toBe('idle');
    expect(contactsFeature.selectSaveStatus(s)).toBe('idle');
    expect(contactsFeature.selectError(s)).toBeNull();
    expect(contactsFeature.selectFieldErrors(s)).toEqual({});
  });
});
