import { Route } from '@angular/router';
import { routes } from './app.routes';

describe('routes', () => {
  it('lazy-loads a component for every route', async () => {
    const loaders = routes.map((r: Route) => r.loadComponent as () => Promise<unknown>);
    const components = await Promise.all(loaders.map((load) => load()));

    expect(components).toHaveLength(routes.length);
    expect(components.every((c) => typeof c === 'function')).toBe(true);
  }, 30000);
});
