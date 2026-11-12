import { Injectable } from '@angular/core';
import { httpResource } from '@angular/common/http';

export interface HealthResponse {
  status: string;
}

@Injectable({ providedIn: 'root' })
export class HealthApi {
  readonly health = httpResource<HealthResponse>(() => '/api/health');
}
