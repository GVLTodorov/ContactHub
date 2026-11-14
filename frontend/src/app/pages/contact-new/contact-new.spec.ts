import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { ContactInput } from '../../api/contacts-api';
import { NewContactPageActions } from '../../store/contacts/contacts.actions';
import { contactsFeature } from '../../store/contacts/contacts.reducer';
import { ContactNew } from './contact-new';

describe('ContactNew', () => {
  function setup(error: string | null = null) {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideMockStore()] });
    const store = TestBed.inject(MockStore);
    store.overrideSelector(contactsFeature.selectSaveStatus, error ? 'error' : 'idle');
    store.overrideSelector(contactsFeature.selectError, error);
    store.overrideSelector(contactsFeature.selectFieldErrors, {});
    const dispatch = vi.spyOn(store, 'dispatch');
    const fixture = TestBed.createComponent(ContactNew);
    return { fixture, dispatch, el: fixture.nativeElement as HTMLElement };
  }

  it('dispatches opened and shows no error by default', async () => {
    const { fixture, dispatch, el } = setup();
    await fixture.whenStable();

    expect(dispatch).toHaveBeenCalledWith(NewContactPageActions.opened());
    expect(el.querySelector('[role="alert"]')).toBeNull();
  });

  it('shows the save error', async () => {
    const { fixture, el } = setup('Boom');
    await fixture.whenStable();

    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Boom');
  });

  it('dispatches create on save and navigates on cancel', async () => {
    const { fixture, dispatch } = setup();
    await fixture.whenStable();
    const input = { firstName: 'A' } as ContactInput;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const page = fixture.componentInstance as unknown as {
      save(v: { input: ContactInput; version: number | null }): void;
      cancel(): void;
    };
    page.save({ input, version: null });
    page.cancel();

    expect(dispatch).toHaveBeenCalledWith(NewContactPageActions.createSubmitted({ input }));
    expect(navigate).toHaveBeenCalledWith(['/contacts']);
  });
});
