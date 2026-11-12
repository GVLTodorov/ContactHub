import { Component, computed, effect, input, output, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { FieldsetModule } from 'primeng/fieldset';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { ContactInput } from '../../api/contacts-api';
import { dateOfBirthValidator, ibanValidator, phoneValidator } from './validators';

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
  /** Server validation errors keyed like `firstName` or `address.city`. */
  readonly serverErrors = input<Record<string, string[]>>({});
  readonly submitted = output<ContactInput>();
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
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    surname: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
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
        validators: [Validators.required, Validators.maxLength(200)],
      }),
      postalCode: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(20)],
      }),
      city: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(100)],
      }),
      country: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(100)],
      }),
    }),
  });

  /** Server errors that do not match any field. */
  protected readonly unmatchedErrors = computed(() =>
    Object.entries(this.serverErrors())
      .filter(([key]) => !this.form.get(key))
      .flatMap(([, messages]) => messages),
  );

  constructor() {
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
    return this.messagesOf(control);
  }

  private messagesOf(control: AbstractControl): string[] {
    const errors = control.errors ?? {};
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
    this.submitted.emit({
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
    });
  }
}
