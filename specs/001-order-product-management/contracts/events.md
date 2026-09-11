# Event Contracts

**Transport**: RabbitMQ via MassTransit, transactional outbox on the publishing side (`research.md` items 1–2).

All events are versioned by type name suffix (`V1`) and are additive-only once published (new optional fields only, never remove/rename a field) to keep consumers backward compatible.

## StockReservationRequestedV1

**Published by**: Order Service (synchronously awaited during `PlaceOrderCommand` in v1 — see `architecture.md` §4; documented as an event for the async fallback/retry path and for future decoupling).

**Consumed by**: Product Service.

```json
{
  "eventId": "uuid",
  "occurredAt": "2026-09-04T10:15:00Z",
  "orderId": "uuid",
  "productId": "uuid",
  "quantity": 3
}
```

## StockReservedV1 / StockRejectedV1

**Published by**: Product Service, in response to a reservation attempt.

**Consumed by**: Order Service (finalizes the order as `Placed` on `StockReservedV1`, or aborts order creation on `StockRejectedV1`, per spec Edge Case "insufficient stock").

```json
// StockReservedV1
{
  "eventId": "uuid",
  "occurredAt": "2026-09-04T10:15:00Z",
  "orderId": "uuid",
  "productId": "uuid",
  "reservedQuantity": 3,
  "nameSnapshot": "Wireless Mouse",
  "unitPriceSnapshot": 19.99
}
```

```json
// StockRejectedV1
{
  "eventId": "uuid",
  "occurredAt": "2026-09-04T10:15:00Z",
  "orderId": "uuid",
  "productId": "uuid",
  "requestedQuantity": 3,
  "availableQuantity": 1,
  "reason": "InsufficientStock"
}
```

## OrderCancelledV1

**Published by**: Order Service, when an order transitions to `Cancelled` (from `Placed` or `Confirmed`).

**Consumed by**: Product Service — releases previously reserved stock for every line item on the order (FR-007).

```json
{
  "eventId": "uuid",
  "occurredAt": "2026-09-04T10:15:00Z",
  "orderId": "uuid",
  "lineItems": [
    { "productId": "uuid", "quantity": 3 }
  ]
}
```

## OrderStatusChangedV1

**Published by**: Order Service, on every status transition (FR-008, FR-009).

**Consumed by**: Any interested internal system/notification consumer (FR-009) — Order Service does not know or care who subscribes.

```json
{
  "eventId": "uuid",
  "occurredAt": "2026-09-04T10:15:00Z",
  "orderId": "uuid",
  "previousStatus": "Placed",
  "newStatus": "Confirmed",
  "changedBy": "user-or-system-id"
}
```

## ProductRetiredV1

**Published by**: Product Service, when a product transitions to `Retired`.

**Consumed by**: No required v1 consumer (Order Service line items already carry a name/price snapshot per `data-model.md` and need no live product data). Documented now so a future consumer (e.g., a "flag historical orders referencing retired products" report) can subscribe without a Product Service change.

```json
{
  "eventId": "uuid",
  "occurredAt": "2026-09-04T10:15:00Z",
  "productId": "uuid",
  "sku": "WM-1001"
}
```

## Delivery Guarantees

- **At-least-once delivery** — every consumer handler must be idempotent (keyed on `eventId` or the natural key of the operation, e.g., re-applying `StockReservedV1` for the same `orderId`+`productId` must be a no-op if already applied).
- **Ordering** is only guaranteed per-`orderId` routing key, not globally — handlers must not assume cross-order ordering.
