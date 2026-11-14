import { Contact } from '../../api/contacts-api';
import { ContactListPageActions, ContactsApiActions } from './contacts.actions';
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

  it('selects the contact for the route id', () => {
    const c = { id: '1', firstName: 'A', surname: 'B' } as Contact;
    const router = (id: string) => ({
      state: { url: '', root: { params: { id }, firstChild: null } },
      navigationId: 1,
    });
    const contacts = appState([c]).contacts;
    expect(contactsFeature.selectSelectedContact({ contacts, router: router('1') } as never)).toEqual(c);
    expect(contactsFeature.selectSelectedContact({ contacts, router: router('2') } as never)).toBeNull();
  });

  it('selects detail status', () => {
    expect(contactsFeature.selectDetailStatus(appState(null))).toBe('idle');
  });

  describe('selectFilteredContacts', () => {
    const make = (id: string, firstName: string, surname: string, phone: string, iban: string, city: string) =>
      ({
        id,
        firstName,
        surname,
        phoneNumber: phone,
        iban,
        ibanFormatted: iban.replace(/(.{4})/g, '$1 ').trim(),
        address: { street: '', postalCode: '', city, country: '' },
      }) as Contact;
    const a = make('1', 'Alice', 'Smith', '+4911111', 'DE89370400440532013000', 'Berlin');
    const b = make('2', 'Bob', 'Jones', '+3322222', 'FR1420041010050500013M02606', 'Paris');
    const stateWith = (query: string) => {
      const contacts = contactsFeature.reducer(
        contactsFeature.reducer(initialState, ContactsApiActions.loadSuccess({ contacts: [a, b] })),
        ContactListPageActions.searchChanged({ query }),
      );
      return { contacts };
    };
    const ids = (q: string) => contactsFeature.selectFilteredContacts(stateWith(q)).map((c) => c.id);

    it('returns everything for an empty query', () => {
      expect(ids('')).toHaveLength(2);
      expect(ids('   ')).toHaveLength(2);
    });
    it('matches first name, surname, phone, city', () => {
      expect(ids('alic')).toEqual(['1']);
      expect(ids('jones')).toEqual(['2']);
      expect(ids('+33')).toEqual(['2']);
      expect(ids('berl')).toEqual(['1']);
    });
    it('matches raw and formatted IBAN ignoring spaces and case', () => {
      expect(ids('de8937')).toEqual(['1']);
      expect(ids('de89 3704 0044')).toEqual(['1']);
      expect(ids('fr14')).toEqual(['2']);
    });
    it('is case-insensitive and returns nothing when no match', () => {
      expect(ids('PARIS')).toEqual(['2']);
      expect(ids('zzz')).toEqual([]);
    });
  });
});
