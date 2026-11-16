import { Injectable } from '@angular/core';
import { httpResource } from '@angular/common/http';

export interface HealthResponse {
  status: string;
}

/** Checks whether the backend is up; the layout shows the result as the online/offline tag. */
@Injectable({ providedIn: 'root' })
export class HealthApi {
  /** Fetched once when first read; `value().status` is `'ok'` when the backend is healthy. */
  readonly health = httpResource<HealthResponse>(() => '/api/health');
}
