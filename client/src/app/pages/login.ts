import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/http-error';
import { apiOrigin, setApiOrigin } from '../../environments/environment';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly forms = inject(FormBuilder);

  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly wrapped = signal(shellKind() !== 'web');
  readonly server = signal(apiOrigin());
  readonly form = this.forms.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  submit() {
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.wrapped() && !setApiOrigin(this.server())) {
      this.error.set('The server address must start with http:// or https://.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => {
        void this.router.navigateByUrl('/');
      },
      error: (error) => {
        this.error.set(errorMessage(error));
        this.busy.set(false);
      },
    });
  }

  onServer(event: Event) {
    this.server.set((event.target as HTMLInputElement).value);
  }
}

function shellKind(): 'desktop' | 'mobile' | 'web' {
  const desktop = (window as Window & { aanganShell?: string }).aanganShell === 'desktop';
  if (desktop) return 'desktop';

  const native = (window as Window & { Capacitor?: { isNativePlatform?: () => boolean } }).Capacitor;
  return native?.isNativePlatform?.() ? 'mobile' : 'web';
}
