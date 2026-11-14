import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ContactInput, ContactsApi, toApiError } from './contacts-api';

describe('ContactsApi', () => {
  let api: ContactsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(ContactsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists contacts', () => {
    let result: unknown;
    api.list().subscribe((r) => (result = r));
    const req = http.expectOne('/api/contacts');
    expect(req.request.method).toBe('GET');
    req.flush([]);
    expect(result).toEqual([]);
  });

  it('creates a contact', () => {
    const input: ContactInput = {
      firstName: 'A',
      surname: 'B',
      dateOfBirth: '1990-01-01',
      address: { street: 's', postalCode: 'p', city: 'c', country: 'x' },
      phoneNumber: '+4912345678',
      iban: 'DE89370400440532013000',
    };
    api.create(input).subscribe();
    const req = http.expectOne('/api/contacts');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(input);
    req.flush({ id: '1', ...input });
  });

  it('gets one contact', () => {
    let result: unknown;
    api.get('abc').subscribe((r) => (result = r));
    const req = http.expectOne('/api/contacts/abc');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'abc' });
    expect(result).toEqual({ id: 'abc' });
  });

  it('deletes a contact with DELETE', () => {
    let done = false;
    api.delete('abc').subscribe(() => (done = true));
    const req = http.expectOne('/api/contacts/abc');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
    expect(done).toBe(true);
  });

  it('updates a contact with PUT, sending the version it was read at', () => {
    const input = { firstName: 'A' } as ContactInput;
    api.update('abc', 4, input).subscribe();
    const req = http.expectOne('/api/contacts/abc');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ firstName: 'A', version: 4 });
    req.flush({ id: 'abc' });
  });

  describe('toApiError', () => {
    const fail = (body: unknown, status: number) => new HttpErrorResponse({ error: body, status });

    it('prefers detail, then title', () => {
      expect(toApiError(fail({ detail: 'D', title: 'T' }, 400)).message).toBe('D');
      expect(toApiError(fail({ title: 'T' }, 400)).message).toBe('T');
    });

    it('keeps field errors and the status', () => {
      const result = toApiError(fail({ title: 'T', errors: { iban: ['bad'] } }, 400));

      expect(result).toEqual({ message: 'T', fieldErrors: { iban: ['bad'] }, status: 400 });
    });

    it('falls back when there is no problem body', () => {
      expect(toApiError(fail(null, 0)).message).toBe('The server is not reachable.');
      expect(toApiError(fail('text', 500))).toEqual({ message: 'Request failed (500).', fieldErrors: {}, status: 500 });
    });

    it('handles non-HTTP errors', () => {
      expect(toApiError(new Error('x'))).toEqual({ message: 'Something went wrong.', fieldErrors: {} });
    });
  });
});
