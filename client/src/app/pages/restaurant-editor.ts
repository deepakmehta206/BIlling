import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Api } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { ToastService } from '../core/toast.service';
import { ModuleGrant, RestaurantDetail } from '../core/models';

@Component({
  selector: 'app-restaurant-editor',
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './restaurant-editor.html',
})
export class RestaurantEditorPage {
  private readonly api = inject(Api);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly forms = inject(FormBuilder);
  private readonly toasts = inject(ToastService);
  private readonly desk = inject(HouseService);
  readonly auth = inject(AuthService);

  readonly house = signal<RestaurantDetail | null>(null);
  readonly modules = signal<ModuleGrant[]>([]);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly confirmArchive = signal(false);
  readonly isNew = signal(true);

  readonly form = this.forms.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    legalName: [''],
    code: [''],
    address: [''],
    city: [''],
    state: [''],
    phone: [''],
    email: [''],
    gstin: [''],
    ownerName: ['', [Validators.required, Validators.minLength(2)]],
    ownerPhone: ['', Validators.required],
    upiId: [''],
    isActive: [true],
    adminFullName: [''],
    adminEmail: [''],
    adminPassword: [''],
    adminPhone: [''],
  });

  constructor() {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      this.confirmArchive.set(false);
      this.error.set(null);
      if (!id) {
        this.isNew.set(true);
        this.house.set(null);
        this.modules.set([]);
        this.form.reset({ isActive: true });
        this.form.enable();
        return;
      }

      this.isNew.set(false);
      this.api.restaurant(id).subscribe({
        next: (house) => this.apply(house),
        error: (error) => this.error.set(errorMessage(error)),
      });
    });
  }

  runHouse() {
    const house = this.house();
    if (!house) return;
    this.desk.select(
      house.id,
      house.name,
      house.code,
      house.modules.filter((module) => module.isEnabled).map((module) => module.code),
    );
    void this.router.navigateByUrl('/floor');
  }

  save() {
    if (!this.auth.isSuperAdmin() || this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.busy.set(true);
    this.error.set(null);

    if (this.isNew()) {
      this.api.createRestaurant(value).subscribe({
        next: (house) => {
          this.busy.set(false);
          this.toasts.show(`${house.name} is open. Turn modules on or off below.`);
          void this.router.navigate(['/restaurants', house.id]);
        },
        error: (error) => {
          this.error.set(errorMessage(error));
          this.busy.set(false);
        },
      });
      return;
    }

    const id = this.house()?.id;
    if (!id) {
      return;
    }

    this.api
      .updateRestaurant(id, {
        name: value.name,
        legalName: value.legalName,
        code: value.code,
        address: value.address,
        city: value.city,
        state: value.state,
        phone: value.phone,
        email: value.email,
        gstin: value.gstin,
        ownerName: value.ownerName,
        ownerPhone: value.ownerPhone,
        upiId: value.upiId,
        isActive: value.isActive,
      })
      .subscribe({
        next: (house) => {
          this.apply(house);
          this.busy.set(false);
          this.toasts.show('House details saved.');
        },
        error: (error) => {
          this.error.set(errorMessage(error));
          this.busy.set(false);
        },
      });
  }

  toggle(module: ModuleGrant) {
    const house = this.house();
    if (!house || !this.auth.isSuperAdmin() || this.busy()) {
      return;
    }

    const next = this.modules().map((item) =>
      item.code === module.code ? { ...item, isEnabled: !item.isEnabled } : item,
    );
    this.modules.set(next);
    this.api
      .updateModules(
        house.id,
        next.map((item) => ({ code: item.code, isEnabled: item.isEnabled })),
      )
      .subscribe({
        next: (modules) => {
          this.modules.set(modules);
          this.toasts.show(`${module.name} ${module.isEnabled ? 'off' : 'on'}.`);
        },
        error: (error) => {
          this.modules.set(this.modules().map((item) => (item.code === module.code ? module : item)));
          this.error.set(errorMessage(error));
        },
      });
  }

  archive() {
    const house = this.house();
    if (!house) {
      return;
    }

    if (!this.confirmArchive()) {
      this.confirmArchive.set(true);
      return;
    }

    this.busy.set(true);
    this.api.deleteRestaurant(house.id).subscribe({
      next: (result) => {
        this.toasts.show(result.message);
        void this.router.navigateByUrl('/restaurants');
      },
      error: (error) => {
        this.error.set(errorMessage(error));
        this.busy.set(false);
        this.confirmArchive.set(false);
      },
    });
  }

  private apply(house: RestaurantDetail) {
    this.house.set(house);
    this.modules.set(house.modules);
    this.form.patchValue({
      name: house.name,
      legalName: house.legalName ?? '',
      code: house.code,
      address: house.address ?? '',
      city: house.city ?? '',
      state: house.state ?? '',
      phone: house.phone ?? '',
      email: house.email ?? '',
      gstin: house.gstin ?? '',
      ownerName: house.ownerName,
      ownerPhone: house.ownerPhone,
      upiId: house.upiId ?? '',
      isActive: house.isActive,
    });
    if (this.auth.isSuperAdmin()) {
      this.form.enable();
    } else {
      this.form.disable();
    }
  }
}
