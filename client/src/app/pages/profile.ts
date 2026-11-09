import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/http-error';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-profile',
  imports: [FormsModule],
  templateUrl: './profile.html',
})
export class ProfilePage {
  private readonly auth = inject(AuthService);
  private readonly toasts = inject(ToastService);
  readonly fullName = signal(this.auth.user()?.fullName ?? '');
  readonly email = signal(this.auth.user()?.email ?? '');
  readonly phone = signal(this.auth.user()?.phone ?? '');
  readonly currentPassword = signal('');
  readonly newPassword = signal('');
  readonly confirmPassword = signal('');
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);

  roleLabel() {
    const user = this.auth.user();
    if (!user) return '';
    if (user.isSuperAdmin) return 'Platform admin';
    return user.roles[0] ?? 'House';
  }

  houseLabel() {
    return this.auth.user()?.restaurantName ?? (this.auth.isSuperAdmin() ? 'Platform' : '—');
  }

  save() {
    if (this.busy()) return;
    const name = this.fullName().trim();
    const email = this.email().trim();
    const phone = this.phone().trim();
    const next = this.newPassword();
    const confirm = this.confirmPassword();

    if (name.length < 2) {
      this.error.set('Enter your name.');
      return;
    }
    if (!email.includes('@')) {
      this.error.set('Enter a valid email.');
      return;
    }
    if (next.length > 0 && next !== confirm) {
      this.error.set('New password and confirmation do not match.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.auth
      .updateProfile({
        fullName: name,
        email,
        phone: phone || null,
        currentPassword: next.length > 0 ? this.currentPassword() || null : null,
        newPassword: next.length > 0 ? next : null,
      })
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.currentPassword.set('');
          this.newPassword.set('');
          this.confirmPassword.set('');
          this.toasts.show('Profile saved.');
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
}
