import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { getRouterSelectors } from '@ngrx/router-store';
import { Store } from '@ngrx/store';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { ContactsPageActions } from '../../store/contacts/contacts.actions';
import { contactsFeature } from '../../store/contacts/contacts.reducer';

export function ageOf(dateOfBirth: string, today = new Date()): number {
  const [y, m, d] = dateOfBirth.split('-').map(Number);
  let age = today.getFullYear() - y;
  if (today.getMonth() + 1 < m || (today.getMonth() + 1 === m && today.getDate() < d)) age--;
  return age;
}

@Component({
  selector: 'app-contact-detail',
  imports: [RouterLink, DatePipe, ButtonModule, CardModule, MessageModule, SkeletonModule, ConfirmDialogModule],
  providers: [ConfirmationService],
  templateUrl: './contact-detail.html',
})
export class ContactDetail {
  private readonly store = inject(Store);
  private readonly confirmation = inject(ConfirmationService);

  protected readonly id = this.store.selectSignal(getRouterSelectors().selectRouteParam('id'));
  protected readonly contact = this.store.selectSignal(contactsFeature.selectSelectedContact);
  protected readonly status = this.store.selectSignal(contactsFeature.selectDetailStatus);
  protected readonly error = this.store.selectSignal(contactsFeature.selectDetailError);
  protected readonly age = computed(() => {
    const c = this.contact();
    return c ? ageOf(c.dateOfBirth) : null;
  });

  constructor() {
    effect(() => {
      const id = this.id();
      if (id) this.store.dispatch(ContactsPageActions.detailOpened({ id }));
    });
  }

  protected confirmDelete(): void {
    const c = this.contact();
    if (!c) return;
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

  protected retry(): void {
    const id = this.id();
    if (id) this.store.dispatch(ContactsPageActions.detailOpened({ id }));
  }
}
