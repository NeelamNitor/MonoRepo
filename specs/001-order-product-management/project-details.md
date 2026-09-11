# Project Details: Order and Product Management Platform

**Related**: [`spec.md`](./spec.md) (feature specification) · [`architecture.md`](./architecture.md) (technical architecture)

## 1. Project Summary

A platform for managing a product catalog and the orders placed against it. The project is split into two independently developed and deployed slices — **Product** (catalog management) and **Order** (order placement and fulfillment tracking) — each with its own frontend application and backend microservice, sharing only well-defined API/event contracts and a common frontend monorepo.

## 2. Goals

- Give catalog managers a fast, reliable way to keep product data (price, stock, description) accurate.
- Let customers/sales users place orders against the catalog with correct, race-free pricing and stock handling.
- Give operations visibility and control over an order's lifecycle from placement through delivery or cancellation.
- Keep Product and Order as independently scalable, independently releasable services so either side can evolve without redeploying the other.

## 3. Scope

### In Scope (v1)

- Product CRUD + retirement, with audit history.
- Order placement (multi-line-item), pricing snapshot at time of order, atomic stock decrement.
- Order lifecycle management: Placed → Confirmed → Shipped → Delivered, plus Cancellation with stock release.
- Search/filter for both products and orders.
- Role-based access: catalog management restricted to authorized roles; order placement open to any authenticated user.

### Out of Scope (v1)

- Payment processing/settlement.
- Shipping carrier integration and label generation.
- Multi-currency and multi-warehouse/multi-stock-pool support.
- Customer-facing storefront/browsing experience beyond what's needed to place an order.

See [`spec.md` → Assumptions](./spec.md#assumptions) for the full list of assumptions underlying this scope.

## 4. Technology Stack

| Concern | Choice |
|---|---|
| Order frontend | Angular |
| Product frontend | Vue.js |
| Frontend architecture | Nx Monorepo (single repo, two independent apps + shared libs) |
| Backend | .NET Core |
| Backend architecture | Microservices, CQRS, MediatR, Clean Architecture, Event-Driven, Repository pattern, Singleton pattern |
| Database | PostgreSQL (one database per microservice) |

A full explanation of how each of these is used, and where in the codebase, is in [`architecture.md`](./architecture.md). A capability-to-technology mapping is also included at the end of [`spec.md`](./spec.md#technology-stack-mapping-as-requested--how-the-stack-is-used-and-where).

## 5. Repository Structure (proposed)

```text
Order-Product-Management/
├── frontend/                     # Nx monorepo
│   ├── apps/
│   │   ├── order-app/            # Angular
│   │   └── product-app/          # Vue.js
│   └── libs/
│       └── shared/{models,api-clients,ui-tokens,util}/
│
├── backend/
│   ├── services/
│   │   ├── OrderService/         # .NET Core microservice (Clean Architecture layers)
│   │   └── ProductService/       # .NET Core microservice (Clean Architecture layers)
│   └── shared/                   # Cross-service contracts (event schemas, common libs), if any
│
├── specs/
│   └── 001-order-product-management/
│       ├── spec.md
│       ├── architecture.md
│       └── project-details.md    # this file
│
└── .specify/                     # Spec Kit (SDD) tooling for this repo
```

## 6. Bounded Contexts / Ownership

| Context | Owns | Frontend | Backend Service | Database |
|---|---|---|---|---|
| Product Catalog | Product identity, pricing, stock levels | product-app (Vue.js) | Product Service | `product_db` |
| Order Management | Order lifecycle, line items, status history | order-app (Angular) | Order Service | `order_db` |

Each context is independently deployable; the only coupling between them is the synchronous "current price/availability" read at order-placement time and the asynchronous domain events described in `architecture.md` §3.4.

## 7. Development Approach

This project follows a **Spec-Driven Development (SDD)** workflow via [Spec Kit](https://github.com/github/spec-kit), already initialized in this repository under `.specify/`:

1. `spec.md` — what the system must do and why (business-facing, this document's sibling).
2. `architecture.md` — how it's built (technical, engineering-facing).
3. `project-details.md` — this file: project-level scope, goals, and structure.
4. Next steps in the SDD flow: `/speckit-clarify` (resolve any open questions), `/speckit-plan` (generate the phased implementation plan from `spec.md`), then `/speckit-tasks` and `/speckit-implement`.

## 8. Status

| Item | Status |
|---|---|
| Feature specification | Drafted (this iteration) |
| Architecture document | Drafted (this iteration) |
| Implementation plan (`plan.md`) | Not started |
| Task breakdown (`tasks.md`) | Not started |
| Implementation | Not started |

## 9. Open Questions / Future Decisions

- Choice of event bus/message broker (e.g., RabbitMQ, Kafka, Azure Service Bus) — not yet decided; `architecture.md` describes the pattern independent of the specific product.
- Whether an API Gateway/BFF sits in front of the two microservices or the frontends call each service directly — noted as optional in `architecture.md` §1.
- Identity provider for authentication (FR-012) — standard OAuth2/OIDC assumed, specific provider not yet chosen.
