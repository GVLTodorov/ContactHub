import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { MessageService } from 'primeng/api';
import { catchError, exhaustMap, map, of, switchMap, tap } from 'rxjs';
import { ContactsApi, toApiError } from '../../api/contacts-api';
import { ContactsApiActions, ContactsPageActions } from './contacts.actions';

@Injectable()
export class ContactsEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(ContactsApi);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);

  readonly load$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContactsPageActions.opened, ContactsPageActions.retry),
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
      ofType(ContactsPageActions.createSubmitted),
      exhaustMap(({ input }) =>
        this.api.create(input).pipe(
          map((contact) => ContactsApiActions.createSuccess({ contact })),
          catchError((e) => of(ContactsApiActions.createFailure({ error: toApiError(e) }))),
        ),
      ),
    ),
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
