import { defineStore } from 'pinia';
import { ApiError, ProductApiClient } from '@frontend/shared-api-clients';
import type { CreateProductRequest, Product, ProductStatus, UpdateProductRequest } from '@frontend/shared-models';
import { PRODUCT_SERVICE_BASE_URL } from '../config';
import { useAuthStore } from './auth.store';

let client: ProductApiClient | null = null;
function getClient(): ProductApiClient {
  if (!client) {
    const authStore = useAuthStore();
    client = new ProductApiClient(PRODUCT_SERVICE_BASE_URL, () => authStore.token);
  }
  return client;
}

// Backs User Story 1 (catalog management) and User Story 4 (search) — see plan.md's ProductService.Application
// command/query mapping; this store is a thin wrapper that just calls the typed API client.
export const useProductStore = defineStore('products', {
  state: () => ({
    items: [] as Product[],
    totalCount: 0,
    loading: false,
    error: null as string | null,
  }),
  actions: {
    async search(query?: string, status?: ProductStatus) {
      this.loading = true;
      this.error = null;
      try {
        const result = await getClient().search(query, status);
        this.items = result.items;
        this.totalCount = result.totalCount;
      } catch (err) {
        this.error = err instanceof ApiError ? err.title : 'Failed to load products.';
      } finally {
        this.loading = false;
      }
    },
    async getById(productId: string): Promise<Product> {
      return getClient().getById(productId);
    },
    async create(request: CreateProductRequest): Promise<Product> {
      return getClient().create(request);
    },
    async update(productId: string, request: UpdateProductRequest): Promise<Product> {
      return getClient().update(productId, request);
    },
    async retire(productId: string): Promise<Product> {
      return getClient().retire(productId);
    },
  },
});
