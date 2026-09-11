// Points at both services directly (architecture.md §1 — no API Gateway in v1). The Dashboard view is the
// "single frontend view needs data aggregated from both services" case research.md item 4 flagged as the
// trigger to revisit that decision — for now it just calls both APIs directly from product-app.
export const PRODUCT_SERVICE_BASE_URL = 'http://localhost:5001';
export const ORDER_SERVICE_BASE_URL = 'http://localhost:5002';
