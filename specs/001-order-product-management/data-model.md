# Phase 1 Data Model: Order and Product Management Platform

**Input**: [spec.md § Key Entities](./spec.md#key-entities), [research.md](./research.md) | **Output**: consumed by `contracts/` and implementation

Two databases, one per microservice, per `architecture.md` §3.7 and `research.md` item 10. No foreign keys cross the `order_db`/`product_db` boundary — cross-database references are by opaque ID plus a denormalized snapshot where needed.

## `product_db` (owned by Product Service)

### Product

Represents a sellable catalog item (spec FR-001, FR-002).

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` (PK) | Generated on creation. |
| `Sku` | `string` | Required, unique within `product_db`. |
| `Name` | `string` | Required, max 200 chars. |
| `Description` | `string` | Optional, max 2000 chars. |
| `Price` | `decimal(18,2)` | Required, **must be ≥ 0** (FR-011). |
| `StockQuantity` | `int` | Required, **must be ≥ 0** (FR-011); decremented only via the atomic reservation path (`research.md` item 3). |
| `Status` | `enum { Active, Retired }` | Default `Active`. Retirement is a status change, never a row delete (FR-002). |
| `RowVersion` | `byte[]` (concurrency token) | EF Core optimistic concurrency, defense-in-depth alongside the conditional UPDATE. |
| `CreatedAt` / `UpdatedAt` | `timestamptz` | Audit timestamps (FR-001). |

**Invariants** (enforced in `ProductService.Domain`):
- `Price >= 0`, `StockQuantity >= 0` at all times — any command attempting to violate this is rejected (FR-011).
- A `Retired` product can never transition back to `Active` implicitly — reactivation, if ever needed, would be a distinct, explicit command (out of scope for v1).
- A `Retired` product is excluded from `SearchProducts` results used for ordering, but its row is never deleted, so historical orders can still resolve it if needed for display.

**State transitions**: `Active → Retired` (one-way, v1).

### ProductAudit *(supports FR-001's audit trail on price/stock changes)*

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` (PK) | |
| `ProductId` | `Guid` (FK → Product) | |
| `Field` | `string` | e.g., `"Price"`, `"StockQuantity"`. |
| `OldValue` / `NewValue` | `string` | Serialized previous/new value. |
| `ChangedAt` | `timestamptz` | |
| `ChangedBy` | `string` | Actor identity from the auth token. |

## `order_db` (owned by Order Service)

### Order

Represents a customer purchase request (spec FR-003, FR-006, FR-008).

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` (PK) | |
| `UserId` | `string` | Identity of the ordering user (from auth token), required. |
| `Status` | `enum { Placed, Confirmed, Shipped, Delivered, Cancelled }` | Default `Placed`. See state machine below. |
| `TotalAmount` | `decimal(18,2)` | Computed as the sum of `OrderLineItem.LineTotal`; **must be > 0** — an order with zero line items is rejected (FR-011). |
| `CreatedAt` / `UpdatedAt` | `timestamptz` | |

**Invariants** (enforced in `OrderService.Domain`):
- At least one `OrderLineItem` is required to place an order (FR-011 edge case).
- `Status` transitions only along the edges defined below; any other transition is rejected (FR-006).

**State transitions** (FR-006):

```text
Placed ──► Confirmed ──► Shipped ──► Delivered
   │             │
   └────► Cancelled ◄────┘
```

- `Placed → Confirmed`, `Confirmed → Shipped`, `Shipped → Delivered`: forward progression only.
- `Placed → Cancelled`, `Confirmed → Cancelled`: cancellation only allowed before shipment.
- Any transition out of `Delivered` or `Cancelled`: **rejected** (terminal states).
- Every transition appends an `OrderStatusHistory` row (FR-008) and publishes `OrderStatusChanged` (FR-009); `→ Cancelled` additionally publishes `OrderCancelled` to release reserved stock (FR-007).

### OrderLineItem

A single product-and-quantity entry within an order (spec Key Entities).

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` (PK) | |
| `OrderId` | `Guid` (FK → Order) | |
| `ProductId` | `Guid` | Reference only (no cross-DB FK) — resolved via Product Service at order-placement time. |
| `ProductNameSnapshot` | `string` | Captured at order time so historical orders render correctly even if the product is later retired/renamed (spec Edge Case). |
| `UnitPriceSnapshot` | `decimal(18,2)` | Captured at order time; **immutable after creation** — later catalog price changes never affect this row (FR-004). |
| `Quantity` | `int` | Required, must be ≥ 1. |
| `LineTotal` | `decimal(18,2)` | Computed: `UnitPriceSnapshot * Quantity`. |

### OrderStatusHistory

Auditable record of every status transition (spec Key Entities, FR-008).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` (PK) | |
| `OrderId` | `Guid` (FK → Order) | |
| `PreviousStatus` | `enum` | Nullable for the initial `Placed` entry. |
| `NewStatus` | `enum` | |
| `ChangedAt` | `timestamptz` | |
| `ChangedBy` | `string` | Actor identity, or `"system"` for automated transitions. |

## Cross-Service Reference Integrity

- `OrderLineItem.ProductId` is validated against Product Service **synchronously, once, at order placement** (via the price/availability read in `architecture.md` §4 step 3) — after that point, the Order Service never needs to re-resolve it, because `ProductNameSnapshot`/`UnitPriceSnapshot` are already captured.
- Product retirement (`ProductRetired` event) does not touch `order_db` at all in v1 — existing `OrderLineItem` snapshots are already self-sufficient. The event is documented in `contracts/events.md` for forward compatibility (e.g., a future "flag orders containing retired products" report) but has no required v1 consumer-side effect.

## Entity-Relationship Summary

```text
product_db                              order_db
┌───────────┐                           ┌───────────┐        ┌──────────────────┐
│ Product   │                           │ Order     │───1:N──│ OrderLineItem     │
│           │                           │           │        │ (ProductId ref,   │
│           │  (no FK — cross-service)  │           │        │  price snapshot)  │
└───────────┘◄╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌│           │        └──────────────────┘
      │                                 │           │
      │ 1:N                             │           │───1:N──┐
      ▼                                 └───────────┘        ▼
┌───────────────┐                                     ┌──────────────────────┐
│ ProductAudit   │                                     │ OrderStatusHistory    │
└───────────────┘                                     └──────────────────────┘
```
