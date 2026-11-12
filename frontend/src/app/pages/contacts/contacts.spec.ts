import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { Contact } from '../../api/contacts-api';
import { ContactsPageActions } from '../../store/contacts/contacts.actions';
import { contactsFeature } from '../../store/contacts/contacts.reducer';
import { Contacts } from './contacts';

const contact = {
  id: '1',
  firstName: 'Ann',
  surname: 'Lee',
  dateOfBirth: '1990-01-01',
  phoneNumber: '+49123',
  ibanFormatted: 'DE89',
  address: { city: 'Berlin' },
} as Contact;

describe('Contacts page', () => {
  function setup(all: Contact[], filtered: Contact[], query: string) {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideMockStore()] });
    const store = TestBed.inject(MockStore);
    store.overrideSelector(contactsFeature.selectAllContacts, all);
    store.overrideSelector(contactsFeature.selectFilteredContacts, filtered);
    store.overrideSelector(contactsFeature.selectQuery, query);
    store.overrideSelector(contactsFeature.selectLoadStatus, 'success');
    store.overrideSelector(contactsFeature.selectIsEmpty, all.length === 0);
    store.overrideSelector(contactsFeature.selectError, null);
    const fixture = TestBed.createComponent(Contacts);
    return { fixture, store, el: fixture.nativeElement as HTMLElement };
  }

  it('shows "No matches" and clears the search', async () => {
    const { fixture, store, el } = setup([contact], [], 'zzz');
    const dispatch = vi.spyOn(store, 'dispatch');
    await fixture.whenStable();
    expect(el.textContent).toContain('No matches');
    expect(el.textContent).not.toContain('No contacts yet');
    const clear = Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes('Clear search'));
    clear!.click();
    expect(dispatch).toHaveBeenCalledWith(ContactsPageActions.searchChanged({ query: '' }));
  });

  it('shows "No contacts yet" when there are no contacts', async () => {
    const { fixture, el } = setup([], [], '');
    await fixture.whenStable();
    expect(el.textContent).toContain('No contacts yet');
    expect(el.textContent).not.toContain('No matches');
  });

  it('shows the stored query in the input and dispatches on typing', async () => {
    const { fixture, store, el } = setup([contact], [contact], 'ann');
    const dispatch = vi.spyOn(store, 'dispatch');
    await fixture.whenStable();
    const input = el.querySelector<HTMLInputElement>('#contact-search')!;
    expect(input.value).toBe('ann');
    input.value = 'lee';
    input.dispatchEvent(new Event('input'));
    expect(dispatch).toHaveBeenCalledWith(ContactsPageActions.searchChanged({ query: 'lee' }));
  });
});
