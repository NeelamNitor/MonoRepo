import { defineStore } from 'pinia';
import { createDevToken, getStoredDevUser, storeDevUser, type DevUser } from '@frontend/shared-util';

// FR-012: catalog-management actions require the catalog-manager role; browsing/search is open to anyone.
// See libs/shared/util/src/dev-auth.ts for why this mints tokens locally instead of a real IdP redirect.
export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: (getStoredDevUser() as DevUser | null) ?? { userId: 'catalog-user-1', roles: ['catalog-manager'] },
    token: null as string | null,
  }),
  getters: {
    isCatalogManager: (state) => state.user.roles.includes('catalog-manager'),
  },
  actions: {
    async ensureToken(): Promise<string> {
      if (!this.token) {
        this.token = await createDevToken(this.user);
      }
      return this.token;
    },
    async setUser(user: DevUser) {
      this.user = user;
      storeDevUser(user);
      this.token = await createDevToken(user);
    },
  },
});
