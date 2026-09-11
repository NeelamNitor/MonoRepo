# Quickstart: Validate Order and Product Management Platform

**Purpose**: Runnable steps to prove the feature works end-to-end once implemented, tracing back to the acceptance scenarios in [spec.md](./spec.md). This is a validation guide, not an implementation guide — see `plan.md`/`data-model.md`/`contracts/` for build details and `tasks.md` (Phase 2) for the implementation task breakdown.

## Prerequisites

- .NET 8 SDK
- Node.js 20+ and a package manager (npm/pnpm) with Nx (`npx nx --version`)
- PostgreSQL 15+ running locally or via container, with two databases created: `order_db`, `product_db`
- RabbitMQ running locally or via container (`docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management`)
- Valid OAuth2/OIDC test tokens for two roles: `catalog-manager` and a standard authenticated user (research.md item 5)

## Setup

```bash
# Backend — apply migrations for both services
dotnet ef database update --project backend/services/ProductService/ProductService.Infrastructure
dotnet ef database update --project backend/services/OrderService/OrderService.Infrastructure

# Backend — run both services
dotnet run --project backend/services/ProductService/ProductService.Api
dotnet run --project backend/services/OrderService/OrderService.Api

# Frontend — install and serve both apps from the Nx workspace
cd frontend
npm install
npx nx serve order-app     # Angular, e.g. http://localhost:4200
npx nx serve product-app   # Vue.js, e.g. http://localhost:4300
```

## Validation Scenarios

Each scenario below maps directly to an Acceptance Scenario in `spec.md`.

### 1. Create and view a product (User Story 1, Scenario 1)

```bash
curl -X POST http://localhost:5001/product-service/v1/products \
  -H "Authorization: Bearer $CATALOG_MANAGER_TOKEN" -H "Content-Type: application/json" \
  -d '{"sku":"WM-1001","name":"Wireless Mouse","price":19.99,"stockQuantity":50}'
```

**Expected**: `201 Created`, response body includes a generated `id` and `status: "Active"`. Product appears in `product-app`'s catalog list and in `GET /products`.

### 2. Update price/stock and confirm audit trail (User Story 1, Scenario 2)

```bash
curl -X PATCH http://localhost:5001/product-service/v1/products/$PRODUCT_ID \
  -H "Authorization: Bearer $CATALOG_MANAGER_TOKEN" -H "Content-Type: application/json" \
  -d '{"price":17.99}'
```

**Expected**: `200 OK` with updated price; a corresponding `ProductAudit` row exists recording the old and new price.

### 3. Place an order against available stock (User Story 2, Scenario 1)

```bash
curl -X POST http://localhost:5002/order-service/v1/orders \
  -H "Authorization: Bearer $USER_TOKEN" -H "Content-Type: application/json" \
  -d '{"lineItems":[{"productId":"'$PRODUCT_ID'","quantity":2}]}'
```

**Expected**: `201 Created`, order `status: "Placed"`, line item `unitPriceSnapshot` equals the product's current price, product's `stockQuantity` decremented by 2 in `product_db`.

### 4. Reject an order that exceeds available stock (User Story 2, Scenario 3 / Edge Case)

```bash
curl -X POST http://localhost:5002/order-service/v1/orders \
  -H "Authorization: Bearer $USER_TOKEN" -H "Content-Type: application/json" \
  -d '{"lineItems":[{"productId":"'$PRODUCT_ID'","quantity":100000}]}'
```

**Expected**: `409 Conflict`; product's `stockQuantity` is unchanged (verify via `GET /products/$PRODUCT_ID`).

### 5. Progress an order through its lifecycle (User Story 3, Scenarios 1–2)

```bash
curl -X PATCH http://localhost:5002/order-service/v1/orders/$ORDER_ID/status \
  -H "Authorization: Bearer $OPS_TOKEN" -H "Content-Type: application/json" \
  -d '{"newStatus":"Confirmed"}'

curl -X PATCH http://localhost:5002/order-service/v1/orders/$ORDER_ID/status \
  -H "Authorization: Bearer $OPS_TOKEN" -H "Content-Type: application/json" \
  -d '{"newStatus":"Shipped"}'
```

**Expected**: Each call returns `200 OK` with the new status; an `OrderStatusHistory` row is added per transition; an `OrderStatusChangedV1` event is observable on the RabbitMQ management UI (`http://localhost:15672`) after each call, within 5 seconds (SC-004).

### 6. Cancel an order and confirm stock release (User Story 3, Scenario 3)

```bash
curl -X PATCH http://localhost:5002/order-service/v1/orders/$ORDER_ID_2/status \
  -H "Authorization: Bearer $OPS_TOKEN" -H "Content-Type: application/json" \
  -d '{"newStatus":"Cancelled"}'
```

**Expected**: `200 OK`; `GET /products/$PRODUCT_ID` shows `stockQuantity` restored by the cancelled order's reserved quantity.

### 7. Reject an invalid status transition (User Story 3, Scenario 4)

```bash
curl -X PATCH http://localhost:5002/order-service/v1/orders/$ORDER_ID/status \
  -H "Authorization: Bearer $OPS_TOKEN" -H "Content-Type: application/json" \
  -d '{"newStatus":"Placed"}'
```

**Expected**: `409 Conflict` — no state change, no event published (confirm by re-fetching the order and confirming `status` is unchanged).

### 8. Search/filter products and orders (User Story 4)

```bash
curl "http://localhost:5001/product-service/v1/products?q=mouse" -H "Authorization: Bearer $USER_TOKEN"
curl "http://localhost:5002/order-service/v1/orders?status=Shipped&from=2026-09-01&to=2026-09-30" -H "Authorization: Bearer $USER_TOKEN"
```

**Expected**: Each returns only matching records (SC-006 target: under 1 second response time).

## Concurrency Check (SC-003 — no overselling)

Fire N concurrent `POST /orders` requests, each ordering the same product with a combined quantity exceeding available stock (e.g., 20 concurrent requests for 10 units of stock, each ordering 1 unit). **Expected**: exactly as many succeed as there is stock for; the rest return `409 Conflict`; final `stockQuantity` is `0`, never negative.
