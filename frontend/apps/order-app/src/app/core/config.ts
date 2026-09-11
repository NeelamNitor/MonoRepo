// Order Service is called directly (architecture.md §1 — no API Gateway in v1); Product Service is only used
// here for the product picker in place-order (a read-only browse, not the FR-005 reservation flow, which
// Order Service performs server-side via its own ProductServiceHttpClient).
export const ORDER_SERVICE_BASE_URL = 'http://localhost:5002';
export const PRODUCT_SERVICE_BASE_URL = 'http://localhost:5001';
