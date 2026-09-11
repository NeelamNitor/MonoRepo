import { defineStore } from 'pinia';
import { ApiError, OrderApiClient } from '@frontend/shared-api-clients';
import type { OrderStatus, OrderSummary } from '@frontend/shared-models';
import { ORDER_SERVICE_BASE_URL } from '../config';
import { useAuthStore } from './auth.store';

let client: OrderApiClient | null = null;
function getClient(): OrderApiClient {
  if (!client) {
    const authStore = useAuthStore();
    client = new OrderApiClient(ORDER_SERVICE_BASE_URL, () => authStore.token);
  }
  return client;
}

// Read-only view of Order Service data for the cross-service Dashboard (see config.ts) — product-app never
// writes to orders, it only reads them for the combined summary/list view.
export const useOrderStore = defineStore('orders', {
  state: () => ({
    items: [] as OrderSummary[],
    totalCount: 0,
    loading: false,
    error: null as string | null,
  }),
  actions: {
    async search(status?: OrderStatus) {
      this.loading = true;
      this.error = null;
      try {
        const result = await getClient().search(status, undefined, undefined, 1, 100);
        this.items = result.items;
        this.totalCount = result.totalCount;
      } catch (err) {
        this.error = err instanceof ApiError ? err.title : 'Failed to load orders.';
      } finally {
        this.loading = false;
      }
    },
  },
});
