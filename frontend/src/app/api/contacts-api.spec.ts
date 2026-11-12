import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ContactInput, ContactsApi } from './contacts-api';

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

  it('updates a contact with PUT', () => {
    const input = { firstName: 'A' } as ContactInput;
    api.update('abc', input).subscribe();
    const req = http.expectOne('/api/contacts/abc');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(input);
    req.flush({ id: 'abc' });
  });
});
