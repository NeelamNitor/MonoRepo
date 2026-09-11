import { InjectionToken, inject } from '@angular/core';
import { OrderApiClient, ProductApiClient } from '@frontend/shared-api-clients';
import { ORDER_SERVICE_BASE_URL, PRODUCT_SERVICE_BASE_URL } from './config';
import { AuthService } from './auth.service';

// Reuses the exact same framework-agnostic clients as product-app (architecture.md §2.1's
// libs/shared/api-clients) — Angular only supplies the DI wiring around them.
export const ORDER_API_CLIENT = new InjectionToken<OrderApiClient>('ORDER_API_CLIENT', {
  factory: () => {
    const auth = inject(AuthService);
    return new OrderApiClient(ORDER_SERVICE_BASE_URL, () => auth.getToken());
  },
});

export const PRODUCT_API_CLIENT = new InjectionToken<ProductApiClient>('PRODUCT_API_CLIENT', {
  factory: () => {
    const auth = inject(AuthService);
    return new ProductApiClient(PRODUCT_SERVICE_BASE_URL, () => auth.getToken());
  },
});
