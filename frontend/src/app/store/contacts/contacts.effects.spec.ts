import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { provideMockActions } from '@ngrx/effects/testing';
import { MessageService } from 'primeng/api';
import { Observable, of, throwError } from 'rxjs';
import { Contact, ContactInput, ContactsApi } from '../../api/contacts-api';
import {
  ContactDetailPageActions,
  ContactListPageActions,
  ContactsApiActions,
  EditContactPageActions,
  NewContactPageActions,
} from './contacts.actions';
import { ContactsEffects } from './contacts.effects';

describe('ContactsEffects', () => {
  const contact = { id: '1', firstName: 'Ann', surname: 'Lee' } as Contact;
  let actions$: Observable<unknown>;
  const api = { list: vi.fn(), create: vi.fn(), get: vi.fn(), update: vi.fn(), delete: vi.fn() };
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
    actions$ = of(ContactListPageActions.opened());
    let result: unknown;
    effects().load$.subscribe((a) => (result = a));
    expect(result).toEqual(ContactsApiActions.loadSuccess({ contacts: [contact] }));
  });

  it('maps load errors to a failure action using ProblemDetails', () => {
    api.list.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500, error: { title: 'Server broke' } })));
    actions$ = of(ContactListPageActions.retry());
    let result: unknown;
    effects().load$.subscribe((a) => (result = a));
    expect(result).toEqual(ContactsApiActions.loadFailure({ error: { message: 'Server broke', fieldErrors: {}, status: 500 } }));
  });

  it('creates a contact', () => {
    api.create.mockReturnValue(of(contact));
    actions$ = of(NewContactPageActions.createSubmitted({ input: {} as ContactInput }));
    let result: unknown;
    effects().create$.subscribe((a) => (result = a));
    expect(result).toEqual(ContactsApiActions.createSuccess({ contact }));
  });

  it('maps validation errors on create', () => {
    const errors = { 'address.city': ['Required'] };
    api.create.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { title: 'Validation failed', errors } })),
    );
    actions$ = of(NewContactPageActions.createSubmitted({ input: {} as ContactInput }));
    let result: unknown;
    effects().create$.subscribe((a) => (result = a));
    expect(result).toEqual(
      ContactsApiActions.createFailure({ error: { message: 'Validation failed', fieldErrors: errors, status: 400 } }),
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

  it('loads one contact', () => {
    api.get.mockReturnValue(of(contact));
    actions$ = of(ContactDetailPageActions.opened({ id: '1' }));
    let result: unknown;
    effects().loadOne$.subscribe((a) => (result = a));
    expect(api.get).toHaveBeenCalledWith('1');
    expect(result).toEqual(ContactsApiActions.loadOneSuccess({ contact }));
  });

  it('loads one contact when the edit page opens or the detail page retries', () => {
    api.get.mockReturnValue(of(contact));
    actions$ = of(EditContactPageActions.opened({ id: '1' }), ContactDetailPageActions.retry({ id: '2' }));
    const results: unknown[] = [];
    effects().loadOne$.subscribe((a) => results.push(a));
    expect(api.get.mock.calls).toEqual([['1'], ['2']]);
    expect(results).toHaveLength(2);
  });

  it('flags 404 as not found and others as errors', () => {
    api.get.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404, error: { title: 'Not Found' } })));
    actions$ = of(ContactDetailPageActions.opened({ id: '1' }));
    let result: { notFound?: boolean } = {};
    const e = effects();
    e.loadOne$.subscribe((a) => (result = a as never));
    expect(result.notFound).toBe(true);

    api.get.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    e.loadOne$.subscribe((a) => (result = a as never));
    expect(result.notFound).toBe(false);
  });

  it('updates a contact', () => {
    api.update.mockReturnValue(of(contact));
    actions$ = of(EditContactPageActions.updateSubmitted({ id: '1', version: 3, input: {} as ContactInput }));
    let result: unknown;
    effects().update$.subscribe((a) => (result = a));
    expect(api.update).toHaveBeenCalledWith('1', 3, {});
    expect(result).toEqual(ContactsApiActions.updateSuccess({ contact }));
  });

  it('maps validation errors on update', () => {
    const errors = { 'address.city': ['Required'] };
    api.update.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { title: 'Validation failed', errors } })),
    );
    actions$ = of(EditContactPageActions.updateSubmitted({ id: '1', version: 3, input: {} as ContactInput }));
    let result: unknown;
    effects().update$.subscribe((a) => (result = a));
    expect(result).toEqual(
      ContactsApiActions.updateFailure({ error: { message: 'Validation failed', fieldErrors: errors, status: 400 } }),
    );
  });

  it('deletes a contact', () => {
    api.delete.mockReturnValue(of(undefined));
    actions$ = of(ContactListPageActions.deleteConfirmed({ id: '1' }));
    let result: unknown;
    effects().delete$.subscribe((a) => (result = a));
    expect(api.delete).toHaveBeenCalledWith('1');
    expect(result).toEqual(ContactsApiActions.deleteContactSuccess({ id: '1' }));
  });

  it('deletes from the detail page too', () => {
    api.delete.mockReturnValue(of(undefined));
    actions$ = of(ContactDetailPageActions.deleteConfirmed({ id: '2' }));
    let result: unknown;
    effects().delete$.subscribe((a) => (result = a));
    expect(result).toEqual(ContactsApiActions.deleteContactSuccess({ id: '2' }));
  });

  it('maps a version conflict on update to a failure with the server message', () => {
    api.update.mockReturnValue(
      throwError(
        () => new HttpErrorResponse({ status: 409, error: { title: 'Conflict', detail: 'Changed by someone else.' } }),
      ),
    );
    actions$ = of(EditContactPageActions.updateSubmitted({ id: '1', version: 3, input: {} as ContactInput }));
    let result: unknown;
    effects().update$.subscribe((a) => (result = a));
    expect(result).toEqual(
      ContactsApiActions.updateFailure({ error: { message: 'Changed by someone else.', fieldErrors: {}, status: 409 } }),
    );
  });

  it('maps delete errors to a failure action', () => {
    api.delete.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404, error: { title: 'Not Found' } })));
    actions$ = of(ContactListPageActions.deleteConfirmed({ id: '1' }));
    let result: unknown;
    effects().delete$.subscribe((a) => (result = a));
    expect(result).toEqual(
      ContactsApiActions.deleteContactFailure({ error: { message: 'Not Found', fieldErrors: {}, status: 404 } }),
    );
  });

  it('toasts and navigates to the list on delete success', () => {
    actions$ = of(ContactsApiActions.deleteContactSuccess({ id: '1' }));
    effects().deleteSuccess$.subscribe();
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success', summary: 'Contact deleted' }));
    expect(router.navigate).toHaveBeenCalledWith(['/contacts']);
  });

  it('toasts the error on delete failure', () => {
    actions$ = of(ContactsApiActions.deleteContactFailure({ error: { message: 'Nope', fieldErrors: {} } }));
    effects().deleteFailure$.subscribe();
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error', detail: 'Nope' }));
  });

  it('toasts and navigates to the detail page on update success', () => {
    actions$ = of(ContactsApiActions.updateSuccess({ contact }));
    effects().updateSuccess$.subscribe();
    expect(router.navigate).toHaveBeenCalledWith(['/contacts', '1']);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });
});
