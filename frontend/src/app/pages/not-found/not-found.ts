import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  template: `
    <section class="flex flex-col items-start gap-4 py-8">
      <h1 class="text-3xl font-bold">Page not found</h1>
      <p>The page you are looking for does not exist.</p>
      <a routerLink="/" class="underline">Back to home</a>
    </section>
  `,
})
export class NotFound {}
