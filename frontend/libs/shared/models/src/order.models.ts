// Mirrors OrderService's OrderDto (contracts/order-service.openapi.yaml, data-model.md).
export type OrderStatus = 'Placed' | 'Confirmed' | 'Shipped' | 'Delivered' | 'Cancelled';

export interface OrderLineItem {
  productId: string;
  productNameSnapshot: string;
  unitPriceSnapshot: number;
  quantity: number;
  lineTotal: number;
}

export interface OrderStatusHistoryEntry {
  previousStatus: OrderStatus | null;
  newStatus: OrderStatus;
  changedAt: string;
  changedBy: string;
}

export interface Order {
  id: string;
  userId: string;
  status: OrderStatus;
  totalAmount: number;
  lineItems: OrderLineItem[];
  statusHistory: OrderStatusHistoryEntry[];
  createdAt: string;
  updatedAt: string;
}

export interface OrderSummary {
  id: string;
  status: OrderStatus;
  totalAmount: number;
  createdAt: string;
}

export interface PlaceOrderLineItemRequest {
  productId: string;
  quantity: number;
}

export interface PlaceOrderRequest {
  lineItems: PlaceOrderLineItemRequest[];
}

export interface ChangeOrderStatusRequest {
  newStatus: OrderStatus;
}

// The valid forward/cancel transitions per data-model.md's state machine — used by the UI to only
// offer actions that the backend will actually accept (FR-006).
export const ALLOWED_ORDER_TRANSITIONS: Record<OrderStatus, OrderStatus[]> = {
  Placed: ['Confirmed', 'Cancelled'],
  Confirmed: ['Shipped', 'Cancelled'],
  Shipped: ['Delivered'],
  Delivered: [],
  Cancelled: [],
};
