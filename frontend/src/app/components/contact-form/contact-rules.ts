import rules from './contact-rules.json';

/**
 * Validation limits shared with the backend domain. They live in contact-rules.json, which the
 * backend test FrontendRulesTests compares with Contact, Address, PhoneNumber and Iban, so the
 * form and the API cannot drift apart.
 */
export const CONTACT_RULES: {
  readonly maxLength: Readonly<{
    firstName: number;
    surname: number;
    street: number;
    postalCode: number;
    city: number;
    country: number;
  }>;
  readonly maxAgeYears: number;
  readonly phoneDigits: Readonly<{ min: number; max: number }>;
  /** Total IBAN length per country code. */
  readonly ibanLengths: Readonly<Record<string, number>>;
} = rules;
