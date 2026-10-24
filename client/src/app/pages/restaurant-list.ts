import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { RestaurantListItem } from '../core/models';

@Component({
  selector: 'app-restaurant-list',
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './restaurant-list.html',
})
export class RestaurantListPage {
  private readonly api = inject(Api);
  private readonly forms = inject(FormBuilder);

  readonly houses = signal<RestaurantListItem[] | null>(null);
  readonly error = signal<string | null>(null);
  readonly search = this.forms.nonNullable.control('');

  constructor() {
    this.load();
    this.search.valueChanges.pipe(debounceTime(250), distinctUntilChanged()).subscribe(() => this.load());
  }

  private load() {
    this.api.restaurants(this.search.value.trim() || undefined).subscribe({
      next: (houses) => {
        this.houses.set(houses);
        this.error.set(null);
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }
}
