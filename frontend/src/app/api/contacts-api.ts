import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

/** Postal address of a contact; all four parts are required by the API. */
export interface Address {
  street: string;
  postalCode: string;
  city: string;
  country: string;
}

/** The editable fields of a contact: the body of a create request, and of an update plus `version`. */
export interface ContactInput {
  firstName: string;
  surname: string;
  /** yyyy-MM-dd */
  dateOfBirth: string;
  address: Address;
  phoneNumber: string;
  iban: string;
}

/** A stored contact as the API returns it. */
export interface Contact extends ContactInput {
  id: string;
  /** The IBAN split into groups of four for display; `iban` holds the compact form. */
  ibanFormatted: string;
  /** ISO 8601 UTC timestamp. */
  createdAt: string;
  /** ISO 8601 UTC timestamp. */
  updatedAt: string;
  /** Goes up on every change; sent back on update so the API can reject stale edits (409). */
  version: number;
}

/** RFC 9457 error body that ASP.NET Core sends for failed requests. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  /** Only on validation failures (400): messages per field, keyed like `firstName` or `address.city`. */
  errors?: Record<string, string[]>;
}

/** An API failure reduced to what the UI needs. */
export interface ApiError {
  /** One message for a banner or toast. */
  message: string;
  /** Messages per form field; empty unless the server rejected the input. */
  fieldErrors: Record<string, string[]>;
  /** HTTP status; `0` means the server could not be reached, `undefined` means it was not an HTTP error. */
  status?: number;
}

/** Converts an HTTP failure (ProblemDetails / ValidationProblem) into a displayable error. */
export function toApiError(err: unknown): ApiError {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as ProblemDetails | null;
    const problem = body && typeof body === 'object' ? body : null;
    const fieldErrors = problem?.errors ?? {};
    const message =
      problem?.detail ||
      problem?.title ||
      (err.status === 0 ? 'The server is not reachable.' : `Request failed (${err.status}).`);
    return { message, fieldErrors, status: err.status };
  }
  return { message: 'Something went wrong.', fieldErrors: {} };
}

/**
 * HTTP client for `/api/contacts`. Paths are relative because the dev server proxies `/api` to
 * the backend. Errors are passed on as-is; callers turn them into an {@link ApiError} with
 * {@link toApiError}.
 */
@Injectable({ providedIn: 'root' })
export class ContactsApi {
  private readonly http = inject(HttpClient);

  /** All contacts. Filtering and sorting happen in the store. */
  list(): Observable<Contact[]> {
    return this.http.get<Contact[]>('/api/contacts');
  }

  /** Creates a contact; a 400 carries field errors. */
  create(input: ContactInput): Observable<Contact> {
    return this.http.post<Contact>('/api/contacts', input);
  }

  /** One contact; a 404 means it does not exist (or was deleted). */
  get(id: string): Observable<Contact> {
    return this.http.get<Contact>(`/api/contacts/${encodeURIComponent(id)}`);
  }

  /**
   * Replaces a contact's fields. `version` is the one the edit started from; the API answers
   * 409 if the contact has changed since then.
   */
  update(id: string, version: number, input: ContactInput): Observable<Contact> {
    return this.http.put<Contact>(`/api/contacts/${encodeURIComponent(id)}`, { ...input, version });
  }

  /** Deletes a contact. */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/contacts/${encodeURIComponent(id)}`);
  }
}
