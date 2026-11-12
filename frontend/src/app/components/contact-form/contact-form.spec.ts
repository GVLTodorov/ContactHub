import { TestBed } from '@angular/core/testing';
import { ContactInput } from '../../api/contacts-api';
import { ContactForm } from './contact-form';

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
    const emitted: ContactInput[] = [];
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
    const emitted: ContactInput[] = [];
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
    expect(emitted[0].firstName).toBe('Ann');
    expect(emitted[0].dateOfBirth).toBe('1990-05-07');
    expect(emitted[0].address.city).toBe('Berlin');
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
