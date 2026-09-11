import {
  ChangeOrderStatusRequest,
  Order,
  OrderStatus,
  OrderSummary,
  PagedResult,
  PlaceOrderRequest,
} from '@frontend/shared-models';
import { throwIfNotOk } from './api-error';

// Typed client for contracts/order-service.openapi.yaml (architecture.md §2.1's libs/shared/api-clients).
export class OrderApiClient {
  constructor(
    private readonly baseUrl: string,
    private readonly getAuthToken: () => string | null
  ) {}

  private headers(): HeadersInit {
    const token = this.getAuthToken();
    return {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    };
  }

  async search(status?: OrderStatus, from?: string, to?: string, page = 1, pageSize = 20): Promise<PagedResult<OrderSummary>> {
    const params = new URLSearchParams();
    if (status) params.set('status', status);
    if (from) params.set('from', from);
    if (to) params.set('to', to);
    params.set('page', String(page));
    params.set('pageSize', String(pageSize));

    const response = await fetch(`${this.baseUrl}/order-service/v1/orders?${params}`, {
      headers: this.headers(),
    });
    await throwIfNotOk(response);
    return response.json();
  }

  async getById(orderId: string): Promise<Order> {
    const response = await fetch(`${this.baseUrl}/order-service/v1/orders/${orderId}`, {
      headers: this.headers(),
    });
    await throwIfNotOk(response);
    return response.json();
  }

  async place(request: PlaceOrderRequest): Promise<Order> {
    const response = await fetch(`${this.baseUrl}/order-service/v1/orders`, {
      method: 'POST',
      headers: this.headers(),
      body: JSON.stringify(request),
    });
    await throwIfNotOk(response);
    return response.json();
  }

  async changeStatus(orderId: string, request: ChangeOrderStatusRequest): Promise<Order> {
    const response = await fetch(`${this.baseUrl}/order-service/v1/orders/${orderId}/status`, {
      method: 'PATCH',
      headers: this.headers(),
      body: JSON.stringify(request),
    });
    await throwIfNotOk(response);
    return response.json();
  }
}
