import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { DatePipe } from '@angular/common';
import { ContactsPageActions } from '../../store/contacts/contacts.actions';
import { contactsFeature } from '../../store/contacts/contacts.reducer';

@Component({
  selector: 'app-contacts',
  imports: [RouterLink, DatePipe, ButtonModule, MessageModule, SkeletonModule, TableModule],
  templateUrl: './contacts.html',
})
export class Contacts {
  private readonly store = inject(Store);

  protected readonly contacts = this.store.selectSignal(contactsFeature.selectAllContacts);
  protected readonly loadStatus = this.store.selectSignal(contactsFeature.selectLoadStatus);
  protected readonly isEmpty = this.store.selectSignal(contactsFeature.selectIsEmpty);
  protected readonly error = this.store.selectSignal(contactsFeature.selectError);
  protected readonly skeletonRows = [1, 2, 3, 4, 5];

  constructor() {
    this.store.dispatch(ContactsPageActions.opened());
  }

  protected retry(): void {
    this.store.dispatch(ContactsPageActions.retry());
  }
}
