import { AbstractControl, ValidationErrors } from '@angular/forms';
import { CONTACT_RULES } from './contact-rules';

const { ibanLengths, maxAgeYears, phoneDigits } = CONTACT_RULES;

/**
 * Checks an IBAN the way the backend does (ISO 13616): format, the length for its country, and
 * the mod-97 checksum. Spaces and lower case are accepted.
 */
export function isValidIban(raw: string): boolean {
  const iban = raw.replace(/\s+/g, '').toUpperCase();
  if (!/^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$/.test(iban)) return false;
  const expected = Object.hasOwn(ibanLengths, iban.slice(0, 2)) ? ibanLengths[iban.slice(0, 2)] : undefined;
  if (expected === undefined || iban.length !== expected) return false;
  // Move the country code and check digits to the end, turn letters into numbers (A=10 … Z=35)
  // and take the result mod 97 one digit at a time, since the full number is too big for a JS number.
  const rearranged = iban.slice(4) + iban.slice(0, 4);
  let remainder = 0;
  for (const ch of rearranged) {
    const digits = ch >= 'A' && ch <= 'Z' ? String(ch.charCodeAt(0) - 55) : ch;
    for (const d of digits) remainder = (remainder * 10 + Number(d)) % 97;
  }
  return remainder === 1;
}

/** Accepts spaces, dashes, dots, brackets and one leading `+`; the rest must be a digit count within the shared limits. */
export function isValidPhone(raw: string): boolean {
  const digits = raw.replace(/[\s\-.()]/g, '').replace(/^\+/, '');
  return /^[0-9]+$/.test(digits) && digits.length >= phoneDigits.min && digits.length <= phoneDigits.max;
}

// The form validators below skip empty values and leave those to `Validators.required`, so a
// blank field shows one message, not two. Each error value is the message the form displays.

export function ibanValidator(control: AbstractControl<string | null>): ValidationErrors | null {
  const v = control.value;
  return !v || isValidIban(v) ? null : { iban: 'Enter a valid IBAN.' };
}

export function phoneValidator(control: AbstractControl<string | null>): ValidationErrors | null {
  const v = control.value;
  return !v || isValidPhone(v)
    ? null
    : { phone: `Use ${phoneDigits.min} to ${phoneDigits.max} digits, optionally starting with +.` };
}

export function dateOfBirthValidator(control: AbstractControl<Date | null>): ValidationErrors | null {
  const v = control.value;
  if (!v) return null;
  const now = new Date();
  if (v.getTime() > now.getTime()) return { dateOfBirth: 'Date of birth cannot be in the future.' };
  const oldest = new Date(now.getFullYear() - maxAgeYears, now.getMonth(), now.getDate());
  if (v.getTime() < oldest.getTime()) {
    return { dateOfBirth: `Date of birth cannot be over ${maxAgeYears} years ago.` };
  }
  return null;
}
