import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { MessageModule } from 'primeng/message';
import { ContactInput } from '../../api/contacts-api';
import { ContactForm } from '../../components/contact-form/contact-form';
import { ContactsPageActions } from '../../store/contacts/contacts.actions';
import { contactsFeature } from '../../store/contacts/contacts.reducer';

@Component({
  selector: 'app-contact-new',
  imports: [ContactForm, MessageModule],
  templateUrl: './contact-new.html',
})
export class ContactNew {
  private readonly store = inject(Store);
  private readonly router = inject(Router);

  protected readonly saveStatus = this.store.selectSignal(contactsFeature.selectSaveStatus);
  protected readonly error = this.store.selectSignal(contactsFeature.selectError);
  protected readonly fieldErrors = this.store.selectSignal(contactsFeature.selectFieldErrors);

  constructor() {
    this.store.dispatch(ContactsPageActions.formOpened());
  }

  protected save(input: ContactInput): void {
    this.store.dispatch(ContactsPageActions.createSubmitted({ input }));
  }

  protected cancel(): void {
    void this.router.navigate(['/contacts']);
  }
}
