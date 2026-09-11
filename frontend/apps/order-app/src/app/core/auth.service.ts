import { Injectable, computed, signal } from '@angular/core';
import { createDevToken, getStoredDevUser, storeDevUser, type DevUser } from '@frontend/shared-util';

// FR-012: order placement is open to any authenticated user; status transitions require the operations role.
// See libs/shared/util/src/dev-auth.ts for why this mints tokens locally instead of a real IdP redirect
// (research.md item 5 defers IdP selection to a future decision).
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userSignal = signal<DevUser>(getStoredDevUser() ?? { userId: 'buyer-1', roles: [] });
  private readonly tokenSignal = signal<string | null>(null);

  readonly user = this.userSignal.asReadonly();
  readonly isOperations = computed(() => this.userSignal().roles.includes('operations'));

  constructor() {
    void this.ensureToken();
  }

  async ensureToken(): Promise<string> {
    let token = this.tokenSignal();
    if (!token) {
      token = await createDevToken(this.userSignal());
      this.tokenSignal.set(token);
    }
    return token;
  }

  getToken(): string | null {
    return this.tokenSignal();
  }

  async setUser(user: DevUser): Promise<void> {
    this.userSignal.set(user);
    storeDevUser(user);
    this.tokenSignal.set(await createDevToken(user));
  }
}
