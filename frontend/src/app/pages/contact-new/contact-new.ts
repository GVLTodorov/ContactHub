import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { MessageModule } from 'primeng/message';
import { ContactForm, ContactFormValue } from '../../components/contact-form/contact-form';
import { NewContactPageActions } from '../../store/contacts/contacts.actions';
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
    this.store.dispatch(NewContactPageActions.opened());
  }

  protected save({ input }: ContactFormValue): void {
    this.store.dispatch(NewContactPageActions.createSubmitted({ input }));
  }

  protected cancel(): void {
    void this.router.navigate(['/contacts']);
  }
}
