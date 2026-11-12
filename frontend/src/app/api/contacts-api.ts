import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface Address {
  street: string;
  postalCode: string;
  city: string;
  country: string;
}

export interface ContactInput {
  firstName: string;
  surname: string;
  /** yyyy-MM-dd */
  dateOfBirth: string;
  address: Address;
  phoneNumber: string;
  iban: string;
}

export interface Contact extends ContactInput {
  id: string;
  ibanFormatted: string;
  createdAt: string;
  updatedAt: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export interface ApiError {
  message: string;
  fieldErrors: Record<string, string[]>;
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

@Injectable({ providedIn: 'root' })
export class ContactsApi {
  private readonly http = inject(HttpClient);

  list(): Observable<Contact[]> {
    return this.http.get<Contact[]>('/api/contacts');
  }

  create(input: ContactInput): Observable<Contact> {
    return this.http.post<Contact>('/api/contacts', input);
  }

  get(id: string): Observable<Contact> {
    return this.http.get<Contact>(`/api/contacts/${encodeURIComponent(id)}`);
  }

  update(id: string, input: ContactInput): Observable<Contact> {
    return this.http.put<Contact>(`/api/contacts/${encodeURIComponent(id)}`, input);
  }
}
