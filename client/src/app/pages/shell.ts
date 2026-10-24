import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html',
})
export class ShellPage {
  readonly auth = inject(AuthService);

  constructor() {
    this.auth.hydrate();
  }

  roleLabel() {
    return this.auth.isSuperAdmin() ? 'Platform admin' : 'Restaurant admin';
  }
}
