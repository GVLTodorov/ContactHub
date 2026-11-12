import { FormControl } from '@angular/forms';
import { dateOfBirthValidator, ibanValidator, isValidIban, isValidPhone, phoneValidator } from './validators';

describe('validators', () => {
  it('accepts valid IBANs, with spaces and lower case', () => {
    expect(isValidIban('DE89370400440532013000')).toBe(true);
    expect(isValidIban('de89 3704 0044 0532 0130 00')).toBe(true);
    expect(isValidIban('GB82 WEST 1234 5698 7654 32')).toBe(true);
  });

  it('rejects a bad checksum or format', () => {
    expect(isValidIban('DE89370400440532013001')).toBe(false);
    expect(isValidIban('1234')).toBe(false);
    expect(ibanValidator(new FormControl('DE00370400440532013000'))?.['iban']).toBeTruthy();
    expect(ibanValidator(new FormControl(''))).toBeNull();
  });

  it('validates phone numbers', () => {
    expect(isValidPhone('+49 (170) 123-4567')).toBe(true);
    expect(isValidPhone('123')).toBe(false);
    expect(isValidPhone('abc1234567')).toBe(false);
    expect(phoneValidator(new FormControl('12'))?.['phone']).toBeTruthy();
  });

  it('validates date of birth range', () => {
    expect(dateOfBirthValidator(new FormControl<Date | null>(new Date(1990, 0, 1)))).toBeNull();
    expect(dateOfBirthValidator(new FormControl<Date | null>(new Date(Date.now() + 86_400_000)))).not.toBeNull();
    expect(dateOfBirthValidator(new FormControl<Date | null>(new Date(1800, 0, 1)))).not.toBeNull();
  });
});
