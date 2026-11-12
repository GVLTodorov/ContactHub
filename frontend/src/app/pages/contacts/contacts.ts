import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { DatePipe } from '@angular/common';
import { Contact } from '../../api/contacts-api';
import { ContactsPageActions } from '../../store/contacts/contacts.actions';
import { contactsFeature } from '../../store/contacts/contacts.reducer';

@Component({
  selector: 'app-contacts',
  imports: [RouterLink, DatePipe, ButtonModule, InputTextModule, MessageModule, SkeletonModule, TableModule, ConfirmDialogModule],
  providers: [ConfirmationService],
  templateUrl: './contacts.html',
})
export class Contacts {
  private readonly store = inject(Store);
  private readonly confirmation = inject(ConfirmationService);

  protected readonly contacts = this.store.selectSignal(contactsFeature.selectFilteredContacts);
  protected readonly allContacts = this.store.selectSignal(contactsFeature.selectAllContacts);
  protected readonly query = this.store.selectSignal(contactsFeature.selectQuery);
  protected readonly noMatches = computed(
    () => this.query().trim() !== '' && this.allContacts().length > 0 && this.contacts().length === 0,
  );
  protected readonly loadStatus = this.store.selectSignal(contactsFeature.selectLoadStatus);
  protected readonly isEmpty = this.store.selectSignal(contactsFeature.selectIsEmpty);
  protected readonly error = this.store.selectSignal(contactsFeature.selectError);
  protected readonly skeletonRows = [1, 2, 3, 4, 5];

  constructor() {
    this.store.dispatch(ContactsPageActions.opened());
  }

  protected confirmDelete(c: Contact): void {
    this.confirmation.confirm({
      header: 'Delete contact',
      message: `Delete ${c.firstName} ${c.surname}?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', outlined: true },
      accept: () => this.store.dispatch(ContactsPageActions.deleteContact({ id: c.id })),
    });
  }

  protected search(value: string): void {
    this.store.dispatch(ContactsPageActions.searchChanged({ query: value }));
  }

  protected clearSearch(): void {
    this.search('');
  }

  protected retry(): void {
    this.store.dispatch(ContactsPageActions.retry());
  }
}
