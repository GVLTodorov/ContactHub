import { Component, effect, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { getRouterSelectors } from '@ngrx/router-store';
import { Store } from '@ngrx/store';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { ContactInput } from '../../api/contacts-api';
import { ContactForm } from '../../components/contact-form/contact-form';
import { ContactsPageActions } from '../../store/contacts/contacts.actions';
import { contactsFeature } from '../../store/contacts/contacts.reducer';

@Component({
  selector: 'app-contact-edit',
  imports: [RouterLink, ContactForm, ButtonModule, MessageModule, SkeletonModule],
  templateUrl: './contact-edit.html',
})
export class ContactEdit {
  private readonly store = inject(Store);
  private readonly router = inject(Router);

  protected readonly id = this.store.selectSignal(getRouterSelectors().selectRouteParam('id'));
  protected readonly contact = this.store.selectSignal(contactsFeature.selectSelectedContact);
  protected readonly detailStatus = this.store.selectSignal(contactsFeature.selectDetailStatus);
  protected readonly detailError = this.store.selectSignal(contactsFeature.selectDetailError);
  protected readonly saveStatus = this.store.selectSignal(contactsFeature.selectSaveStatus);
  protected readonly error = this.store.selectSignal(contactsFeature.selectError);
  protected readonly fieldErrors = this.store.selectSignal(contactsFeature.selectFieldErrors);

  constructor() {
    this.store.dispatch(ContactsPageActions.formOpened());
    effect(() => {
      const id = this.id();
      if (id) this.store.dispatch(ContactsPageActions.detailOpened({ id }));
    });
  }

  protected save(input: ContactInput): void {
    const id = this.id();
    if (id) this.store.dispatch(ContactsPageActions.updateSubmitted({ id, input }));
  }

  protected cancel(): void {
    void this.router.navigate(['/contacts', this.id()]);
  }
}
