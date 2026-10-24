import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { LiveService } from '../core/live.service';
import { Employee, StaffDesk } from '../core/models';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-staff',
  imports: [FormsModule],
  templateUrl: './staff.html',
})
export class StaffPage {
  private readonly api = inject(Api);
  private readonly toasts = inject(ToastService);
  readonly desk = inject(HouseService);
  readonly board = signal<StaffDesk | null>(null);
  readonly error = signal<string | null>(null);
  readonly name = signal('');
  readonly email = signal('');
  readonly phone = signal('');
  readonly role = signal('Waiter');
  readonly password = signal('');
  readonly leaveUser = signal('');
  readonly kind = signal('Casual');
  readonly from = signal(this.day(1));
  readonly to = signal(this.day(1));
  readonly note = signal('');
  readonly editing = signal<Employee | null>(null);
  readonly editName = signal('');
  readonly editPhone = signal('');
  readonly editRole = signal('Waiter');
  readonly editActive = signal(true);
  readonly editPassword = signal('');

  constructor() {
    const live = inject(LiveService);
    const id = this.desk.activeId();
    if (id) live.connect(id);
    this.load();
    let last = live.tick();
    const timer = setInterval(() => {
      const next = live.tick();
      if (next !== last) {
        last = next;
        this.load();
      }
    }, 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  load() {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.staff(id).subscribe({
      next: (board) => {
        this.board.set(board);
        this.error.set(null);
        if (!this.leaveUser()) {
          const staff = board.employees.find((person) => person.role !== 'RestaurantAdmin');
          if (staff) this.leaveUser.set(staff.id);
        }
        if (!board.roles.includes(this.role()) && board.roles[0]) this.role.set(board.roles[0]);
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  hire() {
    const id = this.desk.activeId();
    if (!id || this.name().trim().length < 2 || this.password().length < 8) return;
    this.api
      .hire(id, {
        fullName: this.name().trim(),
        email: this.email().trim(),
        phone: this.phone().trim(),
        role: this.role(),
        password: this.password(),
        isActive: true,
      })
      .subscribe({
        next: (person) => {
          this.toasts.show(`${person.fullName} can sign in.`);
          this.name.set('');
          this.email.set('');
          this.phone.set('');
          this.password.set('');
          this.leaveUser.set(person.id);
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  edit(person: Employee) {
    if (person.role === 'RestaurantAdmin') return;
    this.editing.set(person);
    this.editName.set(person.fullName);
    this.editPhone.set(person.phone ?? '');
    this.editRole.set(person.role);
    this.editActive.set(person.isActive);
    this.editPassword.set('');
  }

  saveEdit() {
    const id = this.desk.activeId();
    const person = this.editing();
    if (!id || !person) return;
    this.api
      .updateEmployee(id, person.id, {
        fullName: this.editName().trim(),
        email: person.email,
        phone: this.editPhone().trim(),
        role: this.editRole(),
        password: this.editPassword() || null,
        isActive: this.editActive(),
      })
      .subscribe({
        next: () => {
          this.toasts.show('Staff card updated.');
          this.editing.set(null);
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  archive(person: Employee) {
    const id = this.desk.activeId();
    if (!id || person.role === 'RestaurantAdmin') return;
    this.api.archiveEmployee(id, person.id).subscribe({
      next: () => {
        this.toasts.show(`${person.fullName} archived.`);
        if (this.editing()?.id === person.id) this.editing.set(null);
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  askLeave() {
    const id = this.desk.activeId();
    if (!id || !this.leaveUser()) return;
    this.api
      .requestLeave(id, {
        userId: this.leaveUser(),
        kind: this.kind(),
        from: this.from(),
        to: this.to(),
        note: this.note().trim() || null,
      })
      .subscribe({
        next: () => {
          this.toasts.show('Leave is waiting for a decision.');
          this.note.set('');
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  decide(leaveId: string, status: string) {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.decideLeave(id, leaveId, status).subscribe({
      next: () => {
        this.toasts.show(status === 'Approved' ? 'Leave approved.' : 'Leave rejected.');
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  private day(offset: number) {
    const date = new Date();
    date.setDate(date.getDate() + offset);
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }
}
