import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Layout } from './layout';

describe('Layout', () => {
  async function setup() {
    await TestBed.configureTestingModule({
      imports: [Layout],
      providers: [provideRouter([{ path: '**', children: [] }]), MessageService, provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(Layout);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    return { fixture, http, el: fixture.nativeElement as HTMLElement };
  }

  it('renders app name and nav', async () => {
    const { el } = await setup();
    expect(el.textContent).toContain('ContactHub');
  });

  it('renders both sidebar links with hrefs', async () => {
    const { el } = await setup();
    const links = Array.from(el.querySelectorAll('nav[aria-label="Main"] a'));
    expect(links.map((a) => a.getAttribute('href'))).toEqual(['/', '/contacts']);
    expect(links.map((a) => a.textContent?.trim())).toEqual(['Home', 'Contacts']);
  });

  it('has no nav links in the toolbar', async () => {
    const { el } = await setup();
    const toolbar = el.querySelector('p-toolbar') as HTMLElement;
    expect(toolbar.querySelector('nav')).toBeNull();
    expect(Array.from(toolbar.querySelectorAll('a')).map((a) => a.getAttribute('href'))).toEqual(['/']);
  });

  it('marks the active link with aria-current on /contacts', async () => {
    const { fixture, http, el } = await setup();
    http.expectOne('/api/health').flush({ status: 'ok' });
    await TestBed.inject(Router).navigateByUrl('/contacts');
    await fixture.whenStable();
    fixture.detectChanges();
    const links = el.querySelectorAll('nav[aria-label="Main"] a');
    expect(links[1].getAttribute('aria-current')).toBe('page');
    expect(links[0].getAttribute('aria-current')).toBeNull();
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


