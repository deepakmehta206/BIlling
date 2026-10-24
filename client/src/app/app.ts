import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toasts } from './pages/toasts';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toasts],
  templateUrl: './app.html',
})
export class App {}
