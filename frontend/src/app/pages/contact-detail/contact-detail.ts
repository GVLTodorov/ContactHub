import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { getRouterSelectors } from '@ngrx/router-store';
import { Store } from '@ngrx/store';
import { AvatarModule } from 'primeng/avatar';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { ContactDetailPageActions } from '../../store/contacts/contacts.actions';
import { contactsFeature } from '../../store/contacts/contacts.reducer';

export function ageOf(dateOfBirth: string, today = new Date()): number {
  const [y, m, d] = dateOfBirth.split('-').map(Number);
  let age = today.getFullYear() - y;
  if (today.getMonth() + 1 < m || (today.getMonth() + 1 === m && today.getDate() < d)) age--;
  return age;
}

type CopyField = 'phone' | 'iban' | 'address';

@Component({
  selector: 'app-contact-detail',
  imports: [RouterLink, DatePipe, NgTemplateOutlet, AvatarModule, ButtonModule, CardModule, MessageModule, SkeletonModule, ConfirmDialogModule],
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

  protected readonly initials = computed(() => {
    const c = this.contact();
    return c ? ((c.firstName[0] ?? '') + (c.surname[0] ?? '')).toUpperCase() : '';
  });
  protected readonly copied = signal<CopyField | null>(null);
  private copiedTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    effect(() => {
      const id = this.id();
      if (id) this.store.dispatch(ContactDetailPageActions.opened({ id }));
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
      accept: () => this.store.dispatch(ContactDetailPageActions.deleteConfirmed({ id: c.id })),
    });
  }

  protected async copy(field: CopyField): Promise<void> {
    const c = this.contact();
    if (!c) return;
    const text = {
      phone: c.phoneNumber,
      iban: c.iban,
      address: `${c.address.street}, ${c.address.postalCode} ${c.address.city}, ${c.address.country}`,
    }[field];
    try {
      await navigator.clipboard.writeText(text);
    } catch {
      return;
    }
    clearTimeout(this.copiedTimer);
    this.copied.set(field);
    this.copiedTimer = setTimeout(() => this.copied.set(null), 2000);
  }

  protected retry(): void {
    const id = this.id();
    if (id) this.store.dispatch(ContactDetailPageActions.retry({ id }));
  }
}
