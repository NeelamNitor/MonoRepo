import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly auth = inject(AuthService);

  switchRole(role: 'operations' | 'buyer') {
    this.auth.setUser(
      role === 'operations' ? { userId: 'ops-1', roles: ['operations'] } : { userId: 'buyer-1', roles: [] }
    );
  }
}
