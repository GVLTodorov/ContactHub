import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { TagModule } from 'primeng/tag';
import { HealthApi } from '../../api/health-api';

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastModule, ToolbarModule, TagModule],
  templateUrl: './layout.html',
})
export class Layout {
  protected readonly navItems = [
    { path: '/', label: 'Home', icon: 'pi pi-home', exact: true },
    { path: '/contacts', label: 'Contacts', icon: 'pi pi-users', exact: false },
  ];

  private readonly api = inject(HealthApi);

  protected readonly status = computed<'loading' | 'online' | 'offline'>(() => {
    const h = this.api.health;
    if (h.isLoading()) return 'loading';
    if (h.error() || h.value()?.status !== 'ok') return 'offline';
    return 'online';
  });
}
