import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { Contact } from '../../api/contacts-api';
import { ContactListPageActions } from '../../store/contacts/contacts.actions';
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
  function setup(all: Contact[], filtered: Contact[], query: string, loadStatus = 'success', error: string | null = null) {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideMockStore()] });
    const store = TestBed.inject(MockStore);
    store.overrideSelector(contactsFeature.selectAllContacts, all);
    store.overrideSelector(contactsFeature.selectFilteredContacts, filtered);
    store.overrideSelector(contactsFeature.selectQuery, query);
    store.overrideSelector(contactsFeature.selectLoadStatus, loadStatus as never);
    store.overrideSelector(contactsFeature.selectIsEmpty, all.length === 0);
    store.overrideSelector(contactsFeature.selectError, error);
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
    expect(dispatch).toHaveBeenCalledWith(ContactListPageActions.searchChanged({ query: '' }));
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
    expect(dispatch).toHaveBeenCalledWith(ContactListPageActions.searchChanged({ query: 'lee' }));
  });

  it('dispatches opened on creation', () => {
    const { store } = setup([], [], '', 'idle');
    const dispatch = vi.spyOn(store, 'dispatch');
    TestBed.createComponent(Contacts);

    expect(dispatch).toHaveBeenCalledWith(ContactListPageActions.opened());
  });

  it('shows a loading skeleton while loading', async () => {
    const { fixture, el } = setup([], [], '', 'loading');
    await fixture.whenStable();

    expect(el.querySelector('[aria-label="Loading contacts"]')).not.toBeNull();
  });

  it('shows the error and retries', async () => {
    const { fixture, store, el } = setup([], [], '', 'error', 'Boom');
    const dispatch = vi.spyOn(store, 'dispatch');
    await fixture.whenStable();
    const retry = Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes('Retry'));
    retry!.click();

    expect(el.textContent).toContain('Boom');
    expect(dispatch).toHaveBeenCalledWith(ContactListPageActions.retry());
  });

  it('falls back to a default load error message', async () => {
    const { fixture, el } = setup([], [], '', 'error');
    await fixture.whenStable();

    expect(el.textContent).toContain('Could not load contacts.');
  });

  it('confirms by name, then dispatches delete', async () => {
    const { fixture, store, el } = setup([contact], [contact], '');
    await fixture.whenStable();
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    const confirm = vi.spyOn(confirmation, 'confirm');
    const dispatch = vi.spyOn(store, 'dispatch');
    el.querySelector<HTMLButtonElement>('button[aria-label="Delete Ann Lee"]')!.click();
    await fixture.whenStable();

    expect(confirm).toHaveBeenCalledWith(expect.objectContaining({ message: 'Delete Ann Lee?' }));

    confirm.mock.calls[0][0].accept?.();

    expect(dispatch).toHaveBeenCalledWith(ContactListPageActions.deleteConfirmed({ id: '1' }));
  });

  it('clears the search with the Clear button', async () => {
    const { fixture, store, el } = setup([contact], [contact], 'ann');
    const dispatch = vi.spyOn(store, 'dispatch');
    await fixture.whenStable();
    const clear = Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Clear');
    clear!.click();

    expect(dispatch).toHaveBeenCalledWith(ContactListPageActions.searchChanged({ query: '' }));
  });
});
