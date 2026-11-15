import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-home',
  imports: [RouterLink, ButtonModule],
  templateUrl: './home.html',
})
export class Home {
  /** The /contacts route exists since Task 3. */
  protected readonly contactsEnabled = true;
}
