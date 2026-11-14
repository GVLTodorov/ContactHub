import { Component, computed, effect, input, output, signal } from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { FieldsetModule } from 'primeng/fieldset';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { Contact, ContactInput } from '../../api/contacts-api';
import { CONTACT_RULES } from './contact-rules';
import { dateOfBirthValidator, ibanValidator, phoneValidator } from './validators';

const { maxLength } = CONTACT_RULES;

/** What the form emits on submit. */
export interface ContactFormValue {
  input: ContactInput;
  /** Version of the contact the form was filled from (`null` for a new contact). */
  version: number | null;
}

export function formatDate(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

@Component({
  selector: 'app-contact-form',
  imports: [ReactiveFormsModule, ButtonModule, DatePickerModule, FieldsetModule, InputTextModule, MessageModule],
  templateUrl: './contact-form.html',
})
export class ContactForm {
  readonly saving = input(false);
  /** Existing contact used to prefill the form. */
  readonly initial = input<Contact | null>(null);
  /** Server validation errors keyed like `firstName` or `address.city`. */
  readonly serverErrors = input<Record<string, string[]>>({});
  readonly submitted = output<ContactFormValue>();
  readonly cancelled = output<void>();

  protected readonly maxDate = new Date();
  protected readonly submitAttempted = signal(false);

  protected readonly addressFields = [
    { name: 'street', label: 'Street' },
    { name: 'postalCode', label: 'Postal code' },
    { name: 'city', label: 'City' },
    { name: 'country', label: 'Country' },
  ] as const;

  protected readonly form = new FormGroup({
    firstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(maxLength.firstName)],
    }),
    surname: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(maxLength.surname)],
    }),
    dateOfBirth: new FormControl<Date | null>(null, [Validators.required, dateOfBirthValidator]),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, phoneValidator],
    }),
    iban: new FormControl('', { nonNullable: true, validators: [Validators.required, ibanValidator] }),
    address: new FormGroup({
      street: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(maxLength.street)],
      }),
      postalCode: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(maxLength.postalCode)],
      }),
      city: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(maxLength.city)],
      }),
      country: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(maxLength.country)],
      }),
    }),
  });

  /** Server errors that do not match any field. */
  protected readonly unmatchedErrors = computed(() =>
    Object.entries(this.serverErrors())
      .filter(([key]) => !this.form.get(key))
      .flatMap(([, messages]) => messages),
  );

  /** The contact the form was last filled from. */
  private filledFrom: Contact | null = null;

  constructor() {
    effect(() => {
      const c = this.initial();
      if (!c) return;
      // The edit page shows the cached contact at once and then re-fetches it. A refreshed copy of
      // the same contact must not wipe what the user has typed; the stale version it keeps makes
      // the API answer 409 if someone else really changed the contact in the meantime.
      if (c.id === this.filledFrom?.id && this.form.dirty) return;
      this.filledFrom = c;
      const [y, m, d] = c.dateOfBirth.split('-').map(Number);
      this.form.reset({
        firstName: c.firstName,
        surname: c.surname,
        dateOfBirth: new Date(y, m - 1, d),
        phoneNumber: c.phoneNumber,
        iban: c.iban,
        address: { ...c.address },
      });
    });
    effect(() => {
      const errors = this.serverErrors();
      for (const [key, messages] of Object.entries(errors)) {
        const control = this.form.get(key);
        if (control && messages.length > 0) {
          control.setErrors({ ...control.errors, server: messages.join(' ') });
          control.markAsTouched();
        }
      }
    });
  }

  /** Messages to show under a field. */
  protected errorsFor(path: string): string[] {
    const control = this.form.get(path);
    if (!control?.errors) return [];
    if (!(control.touched || control.dirty || this.submitAttempted())) return [];
    return this.messagesOf(control.errors);
  }

  private messagesOf(errors: ValidationErrors): string[] {
    return Object.entries(errors).map(([key, value]) => {
      if (key === 'required') return 'This field is required.';
      if (key === 'maxlength') return `At most ${(value as { requiredLength: number }).requiredLength} characters.`;
      return typeof value === 'string' ? value : 'Invalid value.';
    });
  }

  protected submit(): void {
    this.submitAttempted.set(true);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const input: ContactInput = {
      firstName: v.firstName.trim(),
      surname: v.surname.trim(),
      dateOfBirth: formatDate(v.dateOfBirth as Date),
      address: {
        street: v.address.street.trim(),
        postalCode: v.address.postalCode.trim(),
        city: v.address.city.trim(),
        country: v.address.country.trim(),
      },
      phoneNumber: v.phoneNumber.trim(),
      iban: v.iban.trim(),
    };
    this.submitted.emit({ input, version: this.filledFrom?.version ?? null });
  }
}
