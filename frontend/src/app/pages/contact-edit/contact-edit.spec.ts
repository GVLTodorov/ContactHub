import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { Contact, ContactInput } from '../../api/contacts-api';
import { EditContactPageActions } from '../../store/contacts/contacts.actions';
import { contactsFeature } from '../../store/contacts/contacts.reducer';
import { ContactEdit } from './contact-edit';

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
  version: 3,
};

interface Page {
  save(v: { input: ContactInput; version: number | null }): void;
  cancel(): void;
}

interface Options {
  id?: string | null;
  status?: string;
  contact?: Contact | null;
  detailError?: string | null;
  saveStatus?: string;
  error?: string | null;
}

const routerState = (id?: string | null) => ({
  state: { url: '', root: { params: id ? { id } : {}, firstChild: null } },
  navigationId: 1,
});

describe('ContactEdit', () => {
  // overrideSelector sets results on the shared feature selectors; reset them so other spec files see real ones.
  afterEach(() => TestBed.inject(MockStore, null)?.resetSelectors());

  function setup(opts: Options) {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideMockStore({ initialState: { router: routerState(opts.id) } })] });
    const store = TestBed.inject(MockStore);
    store.overrideSelector(contactsFeature.selectSelectedContact, opts.contact ?? null);
    store.overrideSelector(contactsFeature.selectDetailStatus, (opts.status ?? 'success') as never);
    store.overrideSelector(contactsFeature.selectDetailError, opts.detailError ?? null);
    store.overrideSelector(contactsFeature.selectSaveStatus, (opts.saveStatus ?? 'idle') as never);
    store.overrideSelector(contactsFeature.selectError, opts.error ?? null);
    store.overrideSelector(contactsFeature.selectFieldErrors, {});
    const dispatch = vi.spyOn(store, 'dispatch');
    const fixture = TestBed.createComponent(ContactEdit);
    return { fixture, dispatch, el: fixture.nativeElement as HTMLElement };
  }

  it('does not dispatch opened without a route id', async () => {
    const { fixture, dispatch } = setup({ id: null, status: 'loading' });
    await fixture.whenStable();

    expect(dispatch).not.toHaveBeenCalled();
  });

  it('dispatches opened for the route id and shows loading', async () => {
    const { fixture, dispatch, el } = setup({ id: '1', status: 'loading' });
    await fixture.whenStable();

    expect(dispatch).toHaveBeenCalledWith(EditContactPageActions.opened({ id: '1' }));
    expect(el.querySelector('[aria-label="Loading contact"]')).not.toBeNull();
  });

  it('shows the not-found state', async () => {
    const { fixture, el } = setup({ id: '1', status: 'notFound' });
    await fixture.whenStable();

    expect(el.textContent).toContain('does not exist');
  });

  it('shows the load error', async () => {
    const { fixture, el } = setup({ id: '1', status: 'error', detailError: 'Boom' });
    await fixture.whenStable();

    expect(el.textContent).toContain('Boom');
  });

  it('falls back to a default load error message', async () => {
    const { fixture, el } = setup({ id: '1', status: 'error' });
    await fixture.whenStable();

    expect(el.textContent).toContain('Could not load the contact.');
  });

  it('shows the form and the save error', async () => {
    const { fixture, el } = setup({ id: '1', contact, saveStatus: 'error', error: 'Conflict' });
    await fixture.whenStable();

    expect(el.querySelector('app-contact-form')).not.toBeNull();
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Conflict');
  });

  it('shows the form without an error by default', async () => {
    const { fixture, el } = setup({ id: '1', contact });
    await fixture.whenStable();

    expect(el.querySelector('app-contact-form')).not.toBeNull();
    expect(el.querySelector('[role="alert"]')).toBeNull();
  });

  it('dispatches update on save with an id and version, and ignores otherwise', async () => {
    const { fixture, dispatch } = setup({ id: '1', contact });
    await fixture.whenStable();
    const input = { firstName: 'A' } as ContactInput;
    const page = fixture.componentInstance as unknown as Page;
    page.save({ input, version: null });

    expect(dispatch).not.toHaveBeenCalledWith(expect.objectContaining({ type: EditContactPageActions.updateSubmitted.type }));

    page.save({ input, version: 3 });

    expect(dispatch).toHaveBeenCalledWith(EditContactPageActions.updateSubmitted({ id: '1', version: 3, input }));
  });

  it('ignores save without a route id', async () => {
    const { fixture, dispatch } = setup({ id: null, contact });
    await fixture.whenStable();
    (fixture.componentInstance as unknown as Page).save({ input: {} as ContactInput, version: 3 });

    expect(dispatch).not.toHaveBeenCalled();
  });

  it('navigates back to the detail page on cancel', async () => {
    const { fixture } = setup({ id: '1', contact });
    await fixture.whenStable();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    (fixture.componentInstance as unknown as Page).cancel();

    expect(navigate).toHaveBeenCalledWith(['/contacts', '1']);
  });
});
