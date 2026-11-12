import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Layout } from './layout';

describe('Layout', () => {
  async function setup() {
    await TestBed.configureTestingModule({
      imports: [Layout],
      providers: [provideRouter([]), MessageService, provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(Layout);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    return { fixture, http, el: fixture.nativeElement as HTMLElement };
  }

  it('renders app name and nav', async () => {
    const { el } = await setup();
    expect(el.textContent).toContain('ContactHub');
    expect(el.querySelector('nav a')?.textContent).toContain('Home');
  });

  it('shows loading badge first', async () => {
    const { el } = await setup();
    expect(el.querySelector('footer')?.textContent).toContain('API checking');
  });

  it('shows online badge', async () => {
    const { fixture, http, el } = await setup();
    http.expectOne('/api/health').flush({ status: 'ok' });
    await fixture.whenStable();
    expect(el.querySelector('footer')?.textContent).toContain('API online');
  });

  it('shows offline badge on error', async () => {
    const { fixture, http, el } = await setup();
    http.expectOne('/api/health').flush('x', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    expect(el.querySelector('footer')?.textContent).toContain('API offline');
  });
});


