# Tasks: Order and Product Management Platform

**Input**: Design documents from `/specs/001-order-product-management/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required for user stories), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Included — `plan.md`'s Technical Context specifies concrete test frameworks per project (xUnit for both .NET services, Jest for `order-app`, Vitest for `product-app`, Newman for contract tests), so test tasks are part of this breakdown.

**Organization**: Tasks are grouped by user story (per `spec.md`'s priorities: US1/US2 = P1, US3 = P2, US4 = P3) so each can be implemented, tested, and demoed independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 = Manage Product Catalog, US2 = Place and Track an Order, US3 = Update Order Status & Notify, US4 = Search and Filter
- File paths below follow the structure defined in `plan.md` § Project Structure

## Path Conventions (from plan.md)

- Frontend: `frontend/apps/order-app` (Angular), `frontend/apps/product-app` (Vue.js), `frontend/libs/shared/*`
- Backend: `backend/services/OrderService/{OrderService.Domain,Application,Infrastructure,Api}`, `backend/services/ProductService/{ProductService.Domain,Application,Infrastructure,Api}`, `backend/shared/Contracts`
- Tests: `tests/OrderService.Tests`, `tests/ProductService.Tests`, `tests/order-app.spec`, `tests/product-app.spec`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Repository scaffolding for both frontend and backend before any feature code is written

- [ ] T001 Create the Nx workspace at `frontend/` with `@nx/angular` and `@nx/vue` plugins installed
- [ ] T002 [P] Generate `order-app` Angular application in `frontend/apps/order-app` (`nx g @nx/angular:app order-app`)
- [ ] T003 [P] Generate `product-app` Vue application in `frontend/apps/product-app` (`nx g @nx/vue:app product-app`)
- [ ] T004 [P] Generate framework-agnostic shared libs: `frontend/libs/shared/models`, `frontend/libs/shared/api-clients`, `frontend/libs/shared/ui-tokens`, `frontend/libs/shared/util`
- [ ] T005 [P] Configure ESLint + Prettier for the Nx workspace (`frontend/.eslintrc.json`)
- [ ] T006 Create the .NET solution `backend/OrderProductManagement.sln` and add project references for both services
- [ ] T007 [P] Scaffold `OrderService` Clean Architecture projects: `OrderService.Domain`, `OrderService.Application`, `OrderService.Infrastructure`, `OrderService.Api` under `backend/services/OrderService/`
- [ ] T008 [P] Scaffold `ProductService` Clean Architecture projects: `ProductService.Domain`, `ProductService.Application`, `ProductService.Infrastructure`, `ProductService.Api` under `backend/services/ProductService/`
- [ ] T009 [P] Create `backend/shared/Contracts` project for shared event schema types (`StockReservationRequestedV1`, `StockReservedV1`, `StockRejectedV1`, `OrderCancelledV1`, `OrderStatusChangedV1`, `ProductRetiredV1` per `contracts/events.md`)
- [ ] T010 [P] Add `.editorconfig` + `dotnet format`/analyzer configuration for the backend solution
- [ ] T011 Create `docker-compose.yml` at repo root provisioning PostgreSQL (`order_db`, `product_db`) and RabbitMQ (management UI enabled), matching `quickstart.md` Prerequisites
- [ ] T012 [P] Scaffold test projects: `tests/OrderService.Tests` (xUnit), `tests/ProductService.Tests` (xUnit), `tests/order-app.spec` (Jest), `tests/product-app.spec` (Vitest)

**Checkpoint**: Workspaces build (`nx build order-app`, `nx build product-app`, `dotnet build backend/OrderProductManagement.sln`) with no feature code yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cross-cutting infrastructure every user story depends on — CQRS/MediatR pipeline, persistence, messaging, auth. No user story task may start before this phase is complete.

**⚠️ CRITICAL**: Blocks all of Phase 3–6.

- [ ] T013 [P] Add MediatR + FluentValidation to `OrderService.Application` and `ProductService.Application`; register in each service's `Api` DI container
- [ ] T014 [P] Implement `ValidationBehavior<TRequest,TResponse>` (FluentValidation pipeline behavior) in `OrderService.Application/Common` and `ProductService.Application/Common` (enforces FR-011)
- [ ] T015 [P] Implement `LoggingBehavior<TRequest,TResponse>` in both services' `Application/Common`
- [ ] T016 Implement `UnitOfWorkBehavior<TRequest,TResponse>` (commits EF Core `SaveChangesAsync` + outbox flush after Commands only) in both services' `Application/Common`
- [ ] T017 [P] Add EF Core + Npgsql; create `OrderDbContext` in `OrderService.Infrastructure/Persistence` targeting `order_db`
- [ ] T018 [P] Add EF Core + Npgsql; create `ProductDbContext` in `ProductService.Infrastructure/Persistence` targeting `product_db`
- [ ] T019 [P] Add MassTransit + RabbitMQ transport to both services' `Infrastructure/Messaging`, with EF Core transactional outbox enabled (per `research.md` item 2)
- [ ] T020 Implement the Singleton-scoped bus connection, `IOptions<T>` configuration accessor, and reservation coordinator registrations in both services' `Api/Program.cs` (per `research.md` item 9 / `architecture.md` §3.6)
- [ ] T021 [P] Implement JWT bearer authentication/authorization middleware in `OrderService.Api/Program.cs` and `ProductService.Api/Program.cs`, validating OAuth2/OIDC tokens and role claims (FR-012, `research.md` item 5)
- [ ] T022 [P] Configure structured logging with correlation-ID propagation in both services (`architecture.md` §5)
- [ ] T023 [P] Add global exception-handling middleware (maps domain/validation exceptions to `400`/`404`/`409` responses) in both `Api` projects
- [ ] T024 Define base migration for `OrderDbContext` (empty baseline) in `OrderService.Infrastructure/Persistence/Migrations`
- [ ] T025 Define base migration for `ProductDbContext` (empty baseline) in `ProductService.Infrastructure/Persistence/Migrations`
- [ ] T026 [P] Generate typed API clients into `frontend/libs/shared/api-clients` from `contracts/order-service.openapi.yaml` and `contracts/product-service.openapi.yaml`
- [ ] T027 [P] Define shared TypeScript DTOs in `frontend/libs/shared/models` mirroring `data-model.md` (Product, Order, OrderLineItem, OrderStatusHistory)
- [ ] T028 [P] Implement an HTTP interceptor (Angular, `order-app/src/app/core`) and an API wrapper (Vue, `product-app/src/stores`) that attach the bearer token to every request

**Checkpoint**: Both services start, connect to their databases and the bus, and reject unauthenticated requests; both frontend apps can call a health endpoint through the generated clients. No business features exist yet.

---

## Phase 3: User Story 1 - Manage Product Catalog (Priority: P1) 🎯 MVP slice A

**Goal**: Catalog managers can create, view, update, and retire products (spec US1).

**Independent Test**: Create a product, edit its price/stock, retire it, and confirm catalog state via `product-app` and `GET /products` — no order functionality required.

### Tests for User Story 1

- [ ] T029 [P] [US1] Contract test for `POST/GET /products`, `GET/PATCH /products/{id}`, `POST /products/{id}/retire` in `tests/ProductService.Tests/Contract/ProductsContractTests.cs` (Newman or xUnit HTTP client against `contracts/product-service.openapi.yaml`)
- [ ] T030 [P] [US1] Unit tests for `CreateProductCommandHandler`, `UpdateProductCommandHandler`, `RetireProductCommandHandler` in `tests/ProductService.Tests/Application/ProductCommandHandlerTests.cs` (against a fake `IProductRepository`)
- [ ] T031 [P] [US1] Unit tests for `Product` domain invariants (price ≥ 0, stock ≥ 0, one-way Active→Retired) in `tests/ProductService.Tests/Domain/ProductTests.cs`
- [ ] T032 [P] [US1] Component tests for `ProductList`/`ProductForm` views in `tests/product-app.spec/views/`

### Implementation for User Story 1

- [ ] T033 [P] [US1] Implement `Product` entity + invariants in `ProductService.Domain/Entities/Product.cs`
- [ ] T034 [P] [US1] Implement `ProductAudit` entity in `ProductService.Domain/Entities/ProductAudit.cs`
- [ ] T035 [US1] Define `IProductRepository` (`GetByIdAsync`, `AddAsync`, `UpdateAsync`) in `ProductService.Domain/Interfaces/IProductRepository.cs` (depends on T033)
- [ ] T036 [US1] Implement `ProductRepository : IProductRepository` with EF Core in `ProductService.Infrastructure/Repositories/ProductRepository.cs` (depends on T035, T018)
- [ ] T037 [P] [US1] Implement `CreateProductCommand` + `Handler` + `CreateProductValidator` in `ProductService.Application/Commands/CreateProduct/`
- [ ] T038 [P] [US1] Implement `UpdateProductCommand` + `Handler` + `UpdateProductValidator` (writes `ProductAudit` rows on price/stock change) in `ProductService.Application/Commands/UpdateProduct/`
- [ ] T039 [P] [US1] Implement `RetireProductCommand` + `Handler` in `ProductService.Application/Commands/RetireProduct/`
- [ ] T040 [P] [US1] Implement `GetProductByIdQuery` + `Handler` in `ProductService.Application/Queries/GetProductById/`
- [ ] T041 [US1] Implement `ProductsController`/minimal API endpoints (`POST/GET /products`, `GET/PATCH /products/{id}`, `POST /products/{id}/retire`) in `ProductService.Api/Endpoints/ProductsEndpoints.cs` mapping to T037–T040 via MediatR (depends on T037–T040)
- [ ] T042 [US1] Add EF Core migration for `Product`/`ProductAudit` tables in `ProductService.Infrastructure/Persistence/Migrations` (depends on T033, T034)
- [ ] T043 [P] [US1] Build `ProductList` view + Pinia store in `frontend/apps/product-app/src/views/ProductList.vue`, `src/stores/productStore.ts`
- [ ] T044 [P] [US1] Build `ProductForm` view (create/update) in `frontend/apps/product-app/src/views/ProductForm.vue`
- [ ] T045 [US1] Build `ProductDetail` view with a Retire action in `frontend/apps/product-app/src/views/ProductDetail.vue` (depends on T043)
- [ ] T046 [US1] Wire role-based UI guard (catalog-manager only for create/update/retire) in `product-app` router (depends on T028, T041)

**Checkpoint**: User Story 1 fully functional and independently testable/demoable via `product-app`.

---

## Phase 4: User Story 2 - Place and Track an Order (Priority: P1) 🎯 MVP slice B

**Goal**: Users can place a multi-line-item order against the catalog with correct pricing and atomic, race-free stock handling (spec US2).

**Independent Test**: Select existing products (seeded via US1), submit an order, confirm line items/total/status via `order-app` and `GET /orders/{id}`, and confirm an over-quantity order is rejected without decrementing stock.

### Tests for User Story 2

- [ ] T047 [P] [US2] Contract test for `POST/GET /orders`, `GET /orders/{id}` in `tests/OrderService.Tests/Contract/OrdersContractTests.cs`
- [ ] T048 [P] [US2] Contract test for `POST /products/{id}/reserve-stock` (success + 409 insufficient-stock) in `tests/ProductService.Tests/Contract/ReserveStockContractTests.cs`
- [ ] T049 [P] [US2] Unit test for `PlaceOrderCommandHandler` (price snapshot capture, rejection on zero line items) in `tests/OrderService.Tests/Application/PlaceOrderCommandHandlerTests.cs`
- [ ] T050 [P] [US2] Concurrency test: N concurrent `ReserveStockAsync` calls against limited stock never oversell, in `tests/ProductService.Tests/Infrastructure/StockReservationConcurrencyTests.cs` (validates SC-003, per `quickstart.md` Concurrency Check)
- [ ] T051 [P] [US2] Component tests for the order-placement flow in `tests/order-app.spec/features/place-order/`

### Implementation for User Story 2

- [ ] T052 [P] [US2] Implement `Order` and `OrderLineItem` entities + invariants (≥1 line item, immutable price snapshot) in `OrderService.Domain/Entities/`
- [ ] T053 [US2] Define `IOrderRepository` (`GetByIdAsync`, `AddAsync`) in `OrderService.Domain/Interfaces/IOrderRepository.cs` (depends on T052)
- [ ] T054 [US2] Implement `OrderRepository : IOrderRepository` with EF Core in `OrderService.Infrastructure/Repositories/OrderRepository.cs` (depends on T053, T017)
- [ ] T055 [US2] Implement the atomic conditional-UPDATE stock reservation (`UPDATE products SET stock = stock - @qty WHERE id=@id AND stock >= @qty`) as `ProductRepository.ReserveStockAsync` in `ProductService.Infrastructure/Repositories/ProductRepository.cs` (depends on T036, `research.md` item 3)
- [ ] T056 [P] [US2] Implement `ReserveStockCommand` + `Handler` in `ProductService.Application/Commands/ReserveStock/`, returning `nameSnapshot`/`unitPriceSnapshot` or rejecting with insufficient-stock (depends on T055)
- [ ] T057 [US2] Implement `POST /products/{id}/reserve-stock` endpoint in `ProductService.Api/Endpoints/ProductsEndpoints.cs` (depends on T056)
- [ ] T058 [US2] Implement `PlaceOrderCommand` + `Handler` in `OrderService.Application/Commands/PlaceOrder/` — calls Product Service's reserve-stock endpoint per line item, builds `OrderLineItem` snapshots, persists `Order` (status `Placed`) (depends on T052, T054, T057)
- [ ] T059 [P] [US2] Implement `PlaceOrderValidator` (≥1 line item, quantity ≥ 1) in `OrderService.Application/Commands/PlaceOrder/PlaceOrderValidator.cs`
- [ ] T060 [P] [US2] Implement `GetOrderByIdQuery` + `Handler` in `OrderService.Application/Queries/GetOrderById/`
- [ ] T061 [US2] Implement `POST /orders`, `GET /orders/{id}` endpoints in `OrderService.Api/Endpoints/OrdersEndpoints.cs` (depends on T058, T060)
- [ ] T062 [US2] Add EF Core migration for `Order`/`OrderLineItem` tables in `OrderService.Infrastructure/Persistence/Migrations` (depends on T052)
- [ ] T063 [US2] Publish `StockReservationRequestedV1`/consume `StockReservedV1`/`StockRejectedV1` via the outbox for the async fallback path, per `contracts/events.md`, in both services' `Infrastructure/Messaging` (depends on T019, T056, T058)
- [ ] T064 [P] [US2] Build `place-order` feature (product picker, quantity input, submit) in `frontend/apps/order-app/src/app/features/place-order/`
- [ ] T065 [P] [US2] Build `order-list`/`order-detail` features in `frontend/apps/order-app/src/app/features/order-list/`, `order-detail/`
- [ ] T066 [US2] Wire `order-app` routing and auth guard for order placement (any authenticated user) (depends on T028, T061, T064)

**Checkpoint**: User Stories 1 AND 2 both independently functional — a full "browse catalog → place order" MVP is demoable.

---

## Phase 5: User Story 3 - Update Order Status and Notify Stakeholders (Priority: P2)

**Goal**: Operations users can progress an order through Placed → Confirmed → Shipped → Delivered, or cancel it, with stock release and notifications (spec US3).

**Independent Test**: Take a placed order (from US2) through each valid transition and cancellation, confirming status history, stock release on cancel, and rejection of invalid transitions.

### Tests for User Story 3

- [ ] T067 [P] [US3] Contract test for `PATCH /orders/{id}/status` (valid transitions + 409 on invalid) in `tests/OrderService.Tests/Contract/OrderStatusContractTests.cs`
- [ ] T068 [P] [US3] Unit test for the order status state machine (all valid/invalid transitions from `data-model.md`) in `tests/OrderService.Tests/Domain/OrderStatusTransitionTests.cs`
- [ ] T069 [P] [US3] Integration test: cancelling an order releases reserved stock in `product_db`, in `tests/OrderService.Tests/Integration/OrderCancellationReleasesStockTests.cs`
- [ ] T070 [P] [US3] Component tests for order-status UI actions in `tests/order-app.spec/features/order-status/`

### Implementation for User Story 3

- [ ] T071 [P] [US3] Implement `OrderStatusHistory` entity in `OrderService.Domain/Entities/OrderStatusHistory.cs`
- [ ] T072 [US3] Implement the status transition state machine (Placed→Confirmed→Shipped→Delivered; Placed/Confirmed→Cancelled; reject all else) on the `Order` aggregate in `OrderService.Domain/Entities/Order.cs` (depends on T052, T071)
- [ ] T073 [US3] Implement `ChangeOrderStatusCommand` + `Handler` (appends `OrderStatusHistory`, raises `OrderStatusChangedV1`, raises `OrderCancelledV1` on cancellation) in `OrderService.Application/Commands/ChangeOrderStatus/` (depends on T072)
- [ ] T074 [US3] Implement `PATCH /orders/{id}/status` endpoint in `OrderService.Api/Endpoints/OrdersEndpoints.cs` (depends on T073)
- [ ] T075 [US3] Add EF Core migration for `OrderStatusHistory` table in `OrderService.Infrastructure/Persistence/Migrations` (depends on T071)
- [ ] T076 [US3] Implement `OrderCancelledV1` consumer in `ProductService.Infrastructure/Messaging` that releases reserved stock via `ProductRepository` (depends on T055, T063)
- [ ] T077 [P] [US3] Implement `OrderStatusChangedV1` publish-side integration (outbox) confirmed end-to-end in `OrderService.Infrastructure/Messaging` (depends on T073, T019)
- [ ] T078 [P] [US3] Build order-status UI (status badge, transition actions for ops role, cancel action) in `frontend/apps/order-app/src/app/features/order-status/` (depends on T065)
- [ ] T079 [US3] Wire role-based UI guard (operations role for status transitions) in `order-app` router (depends on T028, T074, T078)

**Checkpoint**: All of US1–US3 independently functional; full order lifecycle demoable end-to-end.

---

## Phase 6: User Story 4 - Search and Filter Products/Orders (Priority: P3)

**Goal**: Catalog managers and operations users can search/filter products (name/SKU) and orders (status/date range) (spec US4).

**Independent Test**: Seed a set of products and orders; confirm search/filter queries return correctly narrowed result sets via both frontends.

### Tests for User Story 4

- [ ] T080 [P] [US4] Contract test for `GET /products?q=&status=` filtering in `tests/ProductService.Tests/Contract/SearchProductsContractTests.cs`
- [ ] T081 [P] [US4] Contract test for `GET /orders?status=&from=&to=` filtering in `tests/OrderService.Tests/Contract/SearchOrdersContractTests.cs`

### Implementation for User Story 4

- [ ] T082 [P] [US4] Implement `SearchProductsQuery` + `Handler` (name/SKU match, status filter, paging) in `ProductService.Application/Queries/SearchProducts/` (depends on T035)
- [ ] T083 [US4] Wire `GET /products` search parameters into `ProductsEndpoints.cs` (depends on T082, T041)
- [ ] T084 [P] [US4] Implement `SearchOrdersQuery` + `Handler` (status + date-range filter, paging) in `OrderService.Application/Queries/SearchOrders/` (depends on T053)
- [ ] T085 [US4] Wire `GET /orders` search parameters into `OrdersEndpoints.cs` (depends on T084, T061)
- [ ] T086 [P] [US4] Add search/filter controls to `ProductList` view in `frontend/apps/product-app/src/views/ProductList.vue` (depends on T043)
- [ ] T087 [P] [US4] Add search/filter controls to `order-list` feature in `frontend/apps/order-app/src/app/features/order-list/` (depends on T065)

**Checkpoint**: All four user stories independently functional and demoable.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Hardening and validation that spans multiple user stories

- [ ] T088 [P] Verify FR-012 role enforcement (catalog-manager vs. any-authenticated-user vs. operations) end-to-end across both services
- [ ] T089 [P] Load/perf check against SC-005 (10,000 products / 100,000 orders) and SC-006 (95% of searches < 1s)
- [ ] T090 [P] Verify SC-004 (order status visible to interested parties within 5s) by timing `OrderStatusChangedV1` publish-to-consume latency
- [ ] T091 Run the full `quickstart.md` validation script end-to-end (Scenarios 1–8 + Concurrency Check) against a locally running stack
- [ ] T092 [P] Add API documentation generation (Swagger UI) for both services from `contracts/*.openapi.yaml`
- [ ] T093 [P] Security review: confirm JWT validation, role checks, and input validation (FR-011) reject all documented invalid-input edge cases from `spec.md`
- [ ] T094 Code cleanup pass across both services and both frontend apps; remove scaffolding TODOs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 only
- **User Story 2 (Phase 4)**: Depends on Phase 2; T055–T058 also depend on Product Service repository work from US1 (T036) — so in practice, start US2 after US1's T033–T036 land, even though US2 is otherwise independent
- **User Story 3 (Phase 5)**: Depends on Phase 2 and on `Order`/`OrderLineItem` existing from US2 (T052–T054)
- **User Story 4 (Phase 6)**: Depends on Phase 2 and on the repositories from US1 (T035) and US2 (T053) existing; purely additive (new queries), does not modify US1–US3 code
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### Critical Path for MVP (US1 + US2 only)

Phase 1 → Phase 2 → Phase 3 (US1, through T036 at minimum) → Phase 4 (US2) → **STOP and validate** (quickstart.md Scenarios 1–4) → Deploy/demo.

### Parallel Opportunities

- All `[P]`-marked Setup tasks (T002–T005, T007–T010, T012)
- All `[P]`-marked Foundational tasks (most of T013–T028, except T016 which depends on T013–T015 conceptually and T020 which depends on T019)
- Within US1: T033/T034 in parallel, then T037–T040 in parallel once T035/T036 land
- Within US2: T052 parallel with nothing yet (first task), T056/T059/T060 in parallel once their dependencies land
- Across stories: once Phase 2 is done, US1 (Phase 3) and the entity work in US2 (T052) can proceed in parallel by different developers, though the reserve-stock integration (T055–T058) needs US1's `ProductRepository` (T036) first

---

## Implementation Strategy

### MVP First

1. Phase 1 (Setup) → Phase 2 (Foundational, blocking)
2. Phase 3 (US1) → validate independently (create/update/retire a product)
3. Phase 4 (US2) → validate independently (place an order, confirm stock decrement and rejection on overselling)
4. **STOP and demo** — this is the MVP per `spec.md`'s P1 stories

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. + US1 → demo catalog management
3. + US2 → demo end-to-end ordering (MVP complete)
4. + US3 → demo full order lifecycle + notifications
5. + US4 → demo search/filter at scale
6. Polish (Phase 7) → production-readiness pass

---

## Notes

- `[P]` tasks touch different files with no dependency on each other — safe to parallelize across developers or agents
- `[Story]` labels trace every task back to a `spec.md` user story for MVP-scoping decisions
- Commit after each task or logical group; run the story's tests before moving to the next task
- Reserve-stock (T055–T057) intentionally lives in Product Service files but is labeled `[US2]` because Order placement is what requires it — see Dependencies note above
- Avoid: combining tasks across services/files into one commit in a way that breaks the "independently testable per story" property called out in `spec.md`
