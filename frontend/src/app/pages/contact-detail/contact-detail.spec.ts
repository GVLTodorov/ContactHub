import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { Contact } from '../../api/contacts-api';
import { ContactsPageActions } from '../../store/contacts/contacts.actions';
import { contactsFeature } from '../../store/contacts/contacts.reducer';
import { ageOf, ContactDetail } from './contact-detail';

const contact: Contact = {
  id: '1',
  firstName: 'Ann',
  surname: 'Lee',
  dateOfBirth: '1990-05-20',
  address: { street: 'Main 1', postalCode: '1000', city: 'Berlin', country: 'DE' },
  phoneNumber: '+4912345678',
  iban: 'DE89370400440532013000',
  ibanFormatted: 'DE89 3704 0044 0532 0130 00',
  createdAt: '2024-01-01T10:00:00Z',
  updatedAt: '2024-02-01T10:00:00Z',
};

describe('ContactDetail', () => {
  function setup(status: string, selected: Contact | null, error: string | null = null) {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideMockStore()],
    });
    const store = TestBed.inject(MockStore);
    store.overrideSelector(contactsFeature.selectSelectedContact, selected);
    store.overrideSelector(contactsFeature.selectDetailStatus, status as never);
    store.overrideSelector(contactsFeature.selectDetailError, error);
    const fixture = TestBed.createComponent(ContactDetail);
    return { fixture, store, el: fixture.nativeElement as HTMLElement };
  }

  it('shows a loading state', async () => {
    const { fixture, el } = setup('loading', null);
    await fixture.whenStable();
    expect(el.querySelector('[role="status"]')).not.toBeNull();
  });

  it('shows the not-found state', async () => {
    const { fixture, el } = setup('notFound', null);
    await fixture.whenStable();
    expect(el.textContent).toContain('Contact not found');
    expect(el.textContent).toContain('Back to contacts');
  });

  it('shows the error state and retries', async () => {
    const { fixture, store, el } = setup('error', null, 'Boom');
    await fixture.whenStable();
    expect(el.textContent).toContain('Boom');
    const dispatch = vi.spyOn(store, 'dispatch');
    const retry = Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes('Retry'));
    retry?.click();
    await fixture.whenStable();
    // no route id in the mock store, so nothing is dispatched
    expect(dispatch).not.toHaveBeenCalledWith(ContactsPageActions.detailOpened({ id: '1' }));
  });

  it('shows all contact fields', async () => {
    const { fixture, el } = setup('success', contact);
    await fixture.whenStable();
    const text = el.textContent ?? '';
    expect(text).toContain('DE89 3704 0044 0532 0130 00');
    expect(text).toContain('Berlin');
    expect(text).toContain('+4912345678');
    expect(text).toMatch(/Age \d+ · Born May 20, 1990/);
    expect(text).toContain('Created');
    expect(text).toContain('Updated');
    expect(text).toContain('Edit');
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Ann Lee');
    expect(el.querySelector('a[href^="tel:"]')?.getAttribute('href')).toBe('tel:+4912345678');
    expect(el.querySelector('a[href="/contacts"]')?.textContent).toContain('Back to contacts');
  });

  it('copies the IBAN', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
    const { fixture, el } = setup('success', contact);
    await fixture.whenStable();
    el.querySelector<HTMLButtonElement>('button[aria-label="Copy IBAN"]')?.click();
    await fixture.whenStable();
    expect(writeText).toHaveBeenCalledWith('DE89370400440532013000');
    expect(el.textContent).toContain('Copied');
  });

  it('confirms by name, then dispatches delete', async () => {
    const { fixture, store, el } = setup('success', contact);
    await fixture.whenStable();
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    const confirm = vi.spyOn(confirmation, 'confirm');
    const dispatch = vi.spyOn(store, 'dispatch');
    const btn = Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes('Delete'));
    btn?.click();
    await fixture.whenStable();
    expect(confirm).toHaveBeenCalledWith(expect.objectContaining({ message: 'Delete Ann Lee?' }));
    expect(dispatch).not.toHaveBeenCalledWith(ContactsPageActions.deleteContact({ id: '1' }));
    confirm.mock.calls[0][0].accept?.();
    expect(dispatch).toHaveBeenCalledWith(ContactsPageActions.deleteContact({ id: '1' }));
  });

  it('computes age', () => {
    expect(ageOf('1990-05-20', new Date(2024, 4, 19))).toBe(33);
    expect(ageOf('1990-05-20', new Date(2024, 4, 20))).toBe(34);
  });
});
