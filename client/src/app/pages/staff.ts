import { Component, DestroyRef, HostListener, inject, signal } from '@angular/core';
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
  readonly open = signal<string | null>(null);
  readonly calCursor = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  readonly weekdays = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];
  readonly kinds = ['Casual', 'Sick', 'Unpaid'];

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

  personLabel() {
    return this.board()?.employees.find((person) => person.id === this.leaveUser())?.fullName ?? 'Choose a person';
  }

  dayLabel(value: string) {
    return new Intl.DateTimeFormat('en-IN', { day: 'numeric', month: 'short', year: 'numeric' }).format(this.parseDay(value));
  }

  monthLabel() {
    return new Intl.DateTimeFormat('en-IN', { month: 'long', year: 'numeric' }).format(this.calCursor());
  }

  days() {
    const cursor = this.calCursor();
    const first = new Date(cursor.getFullYear(), cursor.getMonth(), 1);
    const offset = (first.getDay() + 6) % 7;
    const start = new Date(cursor.getFullYear(), cursor.getMonth(), 1 - offset);
    const selected = this.parseDay(this.open() === 'to' ? this.to() : this.from());
    const today = new Date();
    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(start);
      date.setDate(start.getDate() + index);
      return {
        key: `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`,
        label: date.getDate(),
        inMonth: date.getMonth() === cursor.getMonth(),
        selected: this.sameDay(date, selected),
        today: this.sameDay(date, today),
        date,
      };
    });
  }

  onOpen(event: Event, which: string) {
    event.stopPropagation();
    const target = event.target as HTMLElement;
    if (target.closest('.pick-list') || target.closest('.when-panel')) return;
    this.open.update((current) => (current === which ? null : which));
    if (this.open() !== 'from' && this.open() !== 'to') return;
    const selected = this.parseDay(this.open() === 'to' ? this.to() : this.from());
    this.calCursor.set(new Date(selected.getFullYear(), selected.getMonth(), 1));
  }

  shiftMonth(delta: number) {
    const cursor = this.calCursor();
    this.calCursor.set(new Date(cursor.getFullYear(), cursor.getMonth() + delta, 1));
  }

  chooseRole(role: string) {
    this.role.set(role);
    this.open.set(null);
  }

  chooseEditRole(role: string) {
    this.editRole.set(role);
    this.open.set(null);
  }

  choosePerson(id: string) {
    this.leaveUser.set(id);
    this.open.set(null);
  }

  chooseKind(kind: string) {
    this.kind.set(kind);
    this.open.set(null);
  }

  chooseDay(date: Date) {
    const value = this.isoDay(date);
    if (this.open() === 'from') this.from.set(value);
    if (this.open() === 'to') this.to.set(value);
    this.open.set(null);
  }

  @HostListener('document:click')
  closeOpen() {
    this.open.set(null);
  }

  @HostListener('document:keydown.escape')
  closeOnEscape() {
    this.open.set(null);
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

  private parseDay(value: string) {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year || 2026, (month || 1) - 1, day || 1);
  }

  private isoDay(date: Date) {
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }

  private sameDay(left: Date, right: Date) {
    return left.getFullYear() === right.getFullYear() && left.getMonth() === right.getMonth() && left.getDate() === right.getDate();
  }

  private day(offset: number) {
    const date = new Date();
    date.setDate(date.getDate() + offset);
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }
}
