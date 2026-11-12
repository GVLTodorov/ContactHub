import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./pages/home/home').then((m) => m.Home) },
  {
    path: 'contacts',
    pathMatch: 'full',
    loadComponent: () => import('./pages/contacts/contacts').then((m) => m.Contacts),
  },
  {
    path: 'contacts/new',
    loadComponent: () => import('./pages/contact-new/contact-new').then((m) => m.ContactNew),
  },
  {
    path: 'contacts/:id/edit',
    loadComponent: () => import('./pages/contact-edit/contact-edit').then((m) => m.ContactEdit),
  },
  {
    path: 'contacts/:id',
    loadComponent: () => import('./pages/contact-detail/contact-detail').then((m) => m.ContactDetail),
  },
  {
    path: '**',
    loadComponent: () => import('./pages/not-found/not-found').then((m) => m.NotFound),
  },
];
