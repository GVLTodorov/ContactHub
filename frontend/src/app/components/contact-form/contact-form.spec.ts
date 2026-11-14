import { TestBed } from '@angular/core/testing';
import { Contact } from '../../api/contacts-api';
import { ContactForm, ContactFormValue } from './contact-form';

const ada: Contact = {
  id: '1',
  firstName: 'Ada',
  surname: 'Lovelace',
  dateOfBirth: '1990-04-12',
  address: { street: 'Main St 1', postalCode: '10115', city: 'Berlin', country: 'Germany' },
  phoneNumber: '+4930123456',
  iban: 'DE89370400440532013000',
  ibanFormatted: 'DE89 3704 0044 0532 0130 00',
  createdAt: '',
  updatedAt: '',
  version: 2,
};

describe('ContactForm', () => {
  async function setup() {
    await TestBed.configureTestingModule({ imports: [ContactForm] }).compileComponents();
    const fixture = TestBed.createComponent(ContactForm);
    fixture.detectChanges();
    await fixture.whenStable();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  function type(el: HTMLElement, id: string, value: string) {
    const input = el.querySelector<HTMLInputElement>('#' + id)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  it('shows required errors and does not emit when empty', async () => {
    const { fixture, el } = await setup();
    const emitted: ContactFormValue[] = [];
    fixture.componentInstance.submitted.subscribe((v) => emitted.push(v));
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(emitted).toEqual([]);
    expect(el.textContent).toContain('This field is required.');
  });

  it('flags an invalid IBAN', async () => {
    const { fixture, el } = await setup();
    type(el, 'iban', 'DE00370400440532013000');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(el.textContent).toContain('Enter a valid IBAN.');
  });

  it('emits a trimmed, formatted payload when valid', async () => {
    const { fixture, el } = await setup();
    const emitted: ContactFormValue[] = [];
    fixture.componentInstance.submitted.subscribe((v) => emitted.push(v));
    type(el, 'firstName', ' Ann ');
    type(el, 'surname', 'Lee');
    type(el, 'phoneNumber', '+49 170 1234567');
    type(el, 'iban', 'DE89 3704 0044 0532 0130 00');
    type(el, 'address-street', 'Main 1');
    type(el, 'address-postalCode', '10115');
    type(el, 'address-city', 'Berlin');
    type(el, 'address-country', 'Germany');
    (fixture.componentInstance as unknown as { form: { controls: { dateOfBirth: { setValue(v: Date): void } } } }).form.controls.dateOfBirth.setValue(
      new Date(1990, 4, 7),
    );
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    expect(emitted).toHaveLength(1);
    expect(emitted[0].input.firstName).toBe('Ann');
    expect(emitted[0].input.dateOfBirth).toBe('1990-05-07');
    expect(emitted[0].input.address.city).toBe('Berlin');
    expect(emitted[0].version).toBeNull();
  });

  it('fills from the initial contact and emits its version', async () => {
    const { fixture, el } = await setup();
    const emitted: ContactFormValue[] = [];
    fixture.componentInstance.submitted.subscribe((v) => emitted.push(v));
    fixture.componentRef.setInput('initial', ada);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(el.querySelector<HTMLInputElement>('#firstName')!.value).toBe('Ada');
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    expect(emitted[0].version).toBe(2);
    expect(emitted[0].input.iban).toBe('DE89370400440532013000');
  });

  it('keeps typed edits when a refreshed copy of the same contact arrives', async () => {
    const { fixture, el } = await setup();
    const emitted: ContactFormValue[] = [];
    fixture.componentInstance.submitted.subscribe((v) => emitted.push(v));
    fixture.componentRef.setInput('initial', ada);
    fixture.detectChanges();
    await fixture.whenStable();
    type(el, 'firstName', 'Augusta');
    fixture.componentRef.setInput('initial', { ...ada, surname: 'King', version: 3 });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(el.querySelector<HTMLInputElement>('#firstName')!.value).toBe('Augusta');
    expect(el.querySelector<HTMLInputElement>('#surname')!.value).toBe('Lovelace');
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    // Still the version the user started from, so the API can report a real conflict.
    expect(emitted[0].version).toBe(2);
  });

  it('takes a refreshed copy while the user has not typed anything', async () => {
    const { fixture, el } = await setup();
    fixture.componentRef.setInput('initial', ada);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentRef.setInput('initial', { ...ada, surname: 'King', version: 3 });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(el.querySelector<HTMLInputElement>('#surname')!.value).toBe('King');
  });

  it('shows server errors under matching fields', async () => {
    const { fixture, el } = await setup();
    fixture.componentRef.setInput('serverErrors', { 'address.city': ['City is unknown.'], firstName: ['Too odd.'] });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(el.textContent).toContain('City is unknown.');
    expect(el.textContent).toContain('Too odd.');
  });
});
