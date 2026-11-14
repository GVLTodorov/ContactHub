import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { MessageService } from 'primeng/api';
import { catchError, exhaustMap, map, of, switchMap, tap } from 'rxjs';
import { ContactsApi, toApiError } from '../../api/contacts-api';
import {
  ContactDetailPageActions,
  ContactListPageActions,
  ContactsApiActions,
  EditContactPageActions,
  NewContactPageActions,
} from './contacts.actions';

@Injectable()
export class ContactsEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(ContactsApi);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);

  readonly load$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContactListPageActions.opened, ContactListPageActions.retry),
      switchMap(() =>
        this.api.list().pipe(
          map((contacts) => ContactsApiActions.loadSuccess({ contacts })),
          catchError((e) => of(ContactsApiActions.loadFailure({ error: toApiError(e) }))),
        ),
      ),
    ),
  );

  readonly create$ = createEffect(() =>
    this.actions$.pipe(
      ofType(NewContactPageActions.createSubmitted),
      exhaustMap(({ input }) =>
        this.api.create(input).pipe(
          map((contact) => ContactsApiActions.createSuccess({ contact })),
          catchError((e) => of(ContactsApiActions.createFailure({ error: toApiError(e) }))),
        ),
      ),
    ),
  );

  readonly loadOne$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContactDetailPageActions.opened, ContactDetailPageActions.retry, EditContactPageActions.opened),
      switchMap(({ id }) =>
        this.api.get(id).pipe(
          map((contact) => ContactsApiActions.loadOneSuccess({ contact })),
          catchError((e) => {
            const error = toApiError(e);
            return of(ContactsApiActions.loadOneFailure({ error, notFound: error.status === 404 }));
          }),
        ),
      ),
    ),
  );

  readonly update$ = createEffect(() =>
    this.actions$.pipe(
      ofType(EditContactPageActions.updateSubmitted),
      exhaustMap(({ id, version, input }) =>
        this.api.update(id, version, input).pipe(
          map((contact) => ContactsApiActions.updateSuccess({ contact })),
          catchError((e) => of(ContactsApiActions.updateFailure({ error: toApiError(e) }))),
        ),
      ),
    ),
  );

  readonly updateSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(ContactsApiActions.updateSuccess),
        tap(({ contact }) => {
          this.messages.add({
            severity: 'success',
            summary: 'Contact updated',
            detail: `${contact.firstName} ${contact.surname} was saved.`,
          });
          void this.router.navigate(['/contacts', contact.id]);
        }),
      ),
    { dispatch: false },
  );

  readonly delete$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContactListPageActions.deleteConfirmed, ContactDetailPageActions.deleteConfirmed),
      exhaustMap(({ id }) =>
        this.api.delete(id).pipe(
          map(() => ContactsApiActions.deleteContactSuccess({ id })),
          catchError((e) => of(ContactsApiActions.deleteContactFailure({ error: toApiError(e) }))),
        ),
      ),
    ),
  );

  readonly deleteSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(ContactsApiActions.deleteContactSuccess),
        tap(() => {
          this.messages.add({ severity: 'success', summary: 'Contact deleted' });
          void this.router.navigate(['/contacts']);
        }),
      ),
    { dispatch: false },
  );

  readonly deleteFailure$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(ContactsApiActions.deleteContactFailure),
        tap(({ error }) =>
          this.messages.add({ severity: 'error', summary: 'Could not delete contact', detail: error.message }),
        ),
      ),
    { dispatch: false },
  );

  readonly createSuccessNavigate$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(ContactsApiActions.createSuccess),
        tap(() => void this.router.navigate(['/contacts'])),
      ),
    { dispatch: false },
  );

  readonly createSuccessToast$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(ContactsApiActions.createSuccess),
        tap(({ contact }) =>
          this.messages.add({
            severity: 'success',
            summary: 'Contact created',
            detail: `${contact.firstName} ${contact.surname} was added.`,
          }),
        ),
      ),
    { dispatch: false },
  );
}
