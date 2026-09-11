# Implementation Plan: Order and Product Management Platform

**Branch**: `001-order-product-management` | **Date**: 2026-09-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-order-product-management/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Build a two-context platform — **Product** (catalog: create/update/retire products, search) and **Order** (place orders, track/advance status, cancel with stock release) — as specified in `spec.md`. Frontend is an Nx monorepo containing an Angular app for Order and a Vue.js app for Product, sharing typed models and API clients. Backend is two independently deployable .NET Core microservices (Order Service, Product Service), each built with Clean Architecture layering and CQRS via MediatR, communicating synchronously (price/availability reads) and via domain events (stock reservation/release, order status notifications) over an event bus, each persisting to its own PostgreSQL database. Full technical detail lives in [architecture.md](./architecture.md); this plan sequences the Phase 0/1 design artifacts and records the technical decisions needed before implementation (`tasks.md`) can begin.

## Technical Context

**Language/Version**: TypeScript 5.x (Angular 18+/Vue 3.x, Nx workspace) for frontend; C# 12 / .NET 8 LTS for backend.

**Primary Dependencies**:
- Frontend: Angular (Order app, RxJS + Angular Reactive Forms), Vue 3 + Pinia (Product app), Nx (monorepo tooling), OpenAPI-generated typed clients in `libs/shared/api-clients`.
- Backend: ASP.NET Core Web API, MediatR (CQRS mediator + pipeline behaviors), FluentValidation (request validation), Entity Framework Core + Npgsql (PostgreSQL provider), a message-broker client SDK for the event bus (broker product selected in `research.md`).

**Storage**: PostgreSQL — one database per microservice (`order_db`, `product_db`); no shared schema or cross-database joins (see spec's Key Entities and `architecture.md` §3.7).

**Testing**: xUnit + FluentAssertions + Moq/NSubstitute for .NET unit/integration tests (Application-layer command/query handlers tested against fake repositories per the Repository pattern); Jest + Angular Testing Library for `order-app`; Vitest + Vue Testing Library for `product-app`; Postman/Newman or equivalent contract tests against the OpenAPI contracts in `/contracts`.

**Target Platform**: Linux containers (backend services), static web hosting/CDN (frontend SPAs) — matches the deployment topology in `architecture.md` §6.

**Project Type**: Web application — multi-frontend Nx monorepo + independently deployed microservices backend (spec-kit "Option 2/3 hybrid": two frontend apps, two backend services).

**Performance Goals**: Derived from spec Success Criteria — SC-004 (order status visible to interested parties within 5s of change), SC-005 (10,000 products / 100,000 orders without degradation), SC-006 (95% of searches under 1s).

**Constraints**: Stock decrement must be atomic under concurrent orders (spec Edge Case + FR-005) — no overselling in 100% of concurrent-order tests (SC-003). Order line items must snapshot product price/name at order time, immune to later catalog price changes (FR-004). Database-per-service — no cross-service DB access.

**Scale/Scope**: Two bounded contexts, two frontend apps, two backend microservices, ~4 user stories / 12 functional requirements per `spec.md`. Initial scale target per SC-005 (10k products, 100k orders).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` in this repository is still the unfilled starter template — no project-specific principles have been ratified yet. There are therefore no governance gates to check this plan against, and no violations to record in Complexity Tracking below.

**Recommendation**: Before or alongside `/speckit-tasks`, run `/speckit-constitution` to ratify project principles (this plan's architectural choices — Clean Architecture, CQRS, database-per-service, event-driven integration — are natural candidates to promote into the constitution so future features are held to the same bar). This plan does not block on that happening first, but flags it as outstanding.

## Project Structure

### Documentation (this feature)

```text
specs/001-order-product-management/
├── spec.md               # Feature specification (business-facing)
├── architecture.md        # Technical architecture (engineering-facing)
├── project-details.md     # Project-level scope/goals/status
├── plan.md                # This file (/speckit-plan command output)
├── research.md             # Phase 0 output
├── data-model.md           # Phase 1 output
├── quickstart.md            # Phase 1 output
├── contracts/                # Phase 1 output
│   ├── order-service.openapi.yaml
│   ├── product-service.openapi.yaml
│   └── events.md
└── tasks.md                # Phase 2 output (/speckit-tasks command — NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
frontend/                           # Nx monorepo
├── apps/
│   ├── order-app/                  # Angular — Order management UI (User Stories 2, 3)
│   │   ├── src/app/features/       # place-order, order-list, order-detail, order-status
│   │   ├── src/app/core/           # http interceptors, auth guards
│   │   └── src/app/shared/
│   └── product-app/                # Vue.js — Product catalog UI (User Stories 1, 4)
│       ├── src/views/              # ProductList, ProductDetail, ProductForm
│       ├── src/components/
│       └── src/stores/             # Pinia state
└── libs/
    └── shared/
        ├── models/                 # Product, Order, OrderLineItem, OrderStatusHistory DTOs
        ├── api-clients/            # Generated from /contracts OpenAPI specs
        ├── ui-tokens/
        └── util/

backend/
├── services/
│   ├── OrderService/
│   │   ├── OrderService.Domain/            # Order, OrderLineItem, OrderStatusHistory entities; domain events
│   │   ├── OrderService.Application/       # Commands (PlaceOrder, ChangeOrderStatus, CancelOrder), Queries (GetOrder, SearchOrders)
│   │   ├── OrderService.Infrastructure/    # EF Core + Npgsql, OrderRepository, event-bus publisher/consumer
│   │   └── OrderService.Api/               # ASP.NET Core Web API — thin controllers/minimal APIs → MediatR
│   └── ProductService/
│       ├── ProductService.Domain/          # Product entity (price/stock invariants); domain events
│       ├── ProductService.Application/     # Commands (CreateProduct, UpdateProduct, RetireProduct, ReserveStock, ReleaseStock), Queries (GetProduct, SearchProducts)
│       ├── ProductService.Infrastructure/  # EF Core + Npgsql, ProductRepository, event-bus publisher/consumer
│       └── ProductService.Api/
└── shared/
    └── Contracts/                          # Shared event schema types (e.g., StockReserved, OrderStatusChanged) referenced by both services

tests/
├── OrderService.Tests/            # unit (Application handlers via fake IOrderRepository) + integration
├── ProductService.Tests/          # unit + integration
├── order-app.spec/                # Jest/Angular Testing Library
└── product-app.spec/              # Vitest/Vue Testing Library
```

**Structure Decision**: Web application with a multi-app frontend monorepo (Option 2 pattern, doubled: two frontend apps instead of one) paired with two independently deployable backend microservices instead of a single backend. This directly reflects the two bounded contexts (Product Catalog, Order Management) identified in `spec.md` and detailed in `architecture.md` §1–§3. `frontend/libs/shared` and `backend/shared/Contracts` are the only code shared across the Product/Order seam, and both are contract-only (models/DTOs/event schemas), not business logic — keeping the two contexts independently releasable per the project goals in `project-details.md` §2.

## Complexity Tracking

*No constitution violations to justify — see Constitution Check above (constitution not yet ratified for this repository).*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
