import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { provideMockActions } from '@ngrx/effects/testing';
import { MessageService } from 'primeng/api';
import { Observable, of, throwError } from 'rxjs';
import { Contact, ContactInput, ContactsApi } from '../../api/contacts-api';
import { ContactsApiActions, ContactsPageActions } from './contacts.actions';
import { ContactsEffects } from './contacts.effects';

describe('ContactsEffects', () => {
  const contact = { id: '1', firstName: 'Ann', surname: 'Lee' } as Contact;
  let actions$: Observable<unknown>;
  const api = { list: vi.fn(), create: vi.fn() };
  const router = { navigate: vi.fn() };
  const messages = { add: vi.fn() };

  function effects(): ContactsEffects {
    TestBed.configureTestingModule({
      providers: [
        ContactsEffects,
        provideMockActions(() => actions$),
        { provide: ContactsApi, useValue: api },
        { provide: Router, useValue: router },
        { provide: MessageService, useValue: messages },
      ],
    });
    return TestBed.inject(ContactsEffects);
  }

  beforeEach(() => vi.resetAllMocks());

  it('loads contacts', () => {
    api.list.mockReturnValue(of([contact]));
    actions$ = of(ContactsPageActions.opened());
    let result: unknown;
    effects().load$.subscribe((a) => (result = a));
    expect(result).toEqual(ContactsApiActions.loadSuccess({ contacts: [contact] }));
  });

  it('maps load errors to a failure action using ProblemDetails', () => {
    api.list.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500, error: { title: 'Server broke' } })));
    actions$ = of(ContactsPageActions.retry());
    let result: unknown;
    effects().load$.subscribe((a) => (result = a));
    expect(result).toEqual(ContactsApiActions.loadFailure({ error: { message: 'Server broke', fieldErrors: {} } }));
  });

  it('creates a contact', () => {
    api.create.mockReturnValue(of(contact));
    actions$ = of(ContactsPageActions.createSubmitted({ input: {} as ContactInput }));
    let result: unknown;
    effects().create$.subscribe((a) => (result = a));
    expect(result).toEqual(ContactsApiActions.createSuccess({ contact }));
  });

  it('maps validation errors on create', () => {
    const errors = { 'address.city': ['Required'] };
    api.create.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { title: 'Validation failed', errors } })),
    );
    actions$ = of(ContactsPageActions.createSubmitted({ input: {} as ContactInput }));
    let result: unknown;
    effects().create$.subscribe((a) => (result = a));
    expect(result).toEqual(
      ContactsApiActions.createFailure({ error: { message: 'Validation failed', fieldErrors: errors } }),
    );
  });

  it('navigates and toasts on create success', () => {
    actions$ = of(ContactsApiActions.createSuccess({ contact }));
    const e = effects();
    e.createSuccessNavigate$.subscribe();
    e.createSuccessToast$.subscribe();
    expect(router.navigate).toHaveBeenCalledWith(['/contacts']);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });
});
