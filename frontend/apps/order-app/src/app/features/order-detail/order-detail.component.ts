import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiError } from '@frontend/shared-api-clients';
import { ALLOWED_ORDER_TRANSITIONS, type Order, type OrderStatus } from '@frontend/shared-models';
import { formatCurrency, formatDateTime } from '@frontend/shared-util';
import { ORDER_API_CLIENT } from '../../core/api-clients';
import { AuthService } from '../../core/auth.service';

// User Story 3: progress an order through its lifecycle or cancel it, and see its full status history
// (FR-006, FR-007, FR-008, FR-009). Status-change actions are restricted to the operations role (FR-012).
@Component({
  selector: 'app-order-detail',
  imports: [RouterLink],
  template: `
    <a routerLink="/orders" class="btn">&larr; Back to orders</a>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }

    @if (order(); as o) {
      <h1>Order {{ o.id.slice(0, 8) }}</h1>
      <span class="badge">{{ o.status }}</span>

      <table class="cart-table">
        <thead>
          <tr>
            <th>Product</th>
            <th>Qty</th>
            <th>Unit Price</th>
            <th>Line Total</th>
          </tr>
        </thead>
        <tbody>
          @for (line of o.lineItems; track line.productId) {
            <tr>
              <td>{{ line.productNameSnapshot }}</td>
              <td>{{ line.quantity }}</td>
              <td>{{ formatCurrency(line.unitPriceSnapshot) }}</td>
              <td>{{ formatCurrency(line.lineTotal) }}</td>
            </tr>
          }
        </tbody>
      </table>
      <p class="cart-total">Total: {{ formatCurrency(o.totalAmount) }}</p>

      @if (auth.isOperations() && nextActions(o.status).length > 0) {
        <div class="product-detail__actions">
          @for (next of nextActions(o.status); track next) {
            <button class="btn" [class.btn--danger]="next === 'Cancelled'" (click)="changeStatus(next)">
              Mark {{ next }}
            </button>
          }
        </div>
      }

      <h2>Status History</h2>
      <ul class="status-history">
        @for (entry of o.statusHistory; track entry.changedAt) {
          <li>
            {{ formatDateTime(entry.changedAt) }} —
            {{ entry.previousStatus ? entry.previousStatus + ' → ' : '' }}{{ entry.newStatus }}
            ({{ entry.changedBy }})
          </li>
        }
      </ul>
    }
  `,
})
export class OrderDetailComponent implements OnInit {
  private readonly orderApi = inject(ORDER_API_CLIENT);
  private readonly route = inject(ActivatedRoute);
  protected readonly auth = inject(AuthService);

  protected readonly formatCurrency = formatCurrency;
  protected readonly formatDateTime = formatDateTime;

  readonly order = signal<Order | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit() {
    this.load();
  }

  nextActions(status: OrderStatus): OrderStatus[] {
    return ALLOWED_ORDER_TRANSITIONS[status];
  }

  async load() {
    const orderId = this.route.snapshot.paramMap.get('id');
    if (!orderId) return;
    try {
      this.order.set(await this.orderApi.getById(orderId));
    } catch (err) {
      this.error.set(err instanceof ApiError ? err.title : 'Failed to load order.');
    }
  }

  async changeStatus(newStatus: OrderStatus) {
    const current = this.order();
    if (!current) return;
    try {
      this.order.set(await this.orderApi.changeStatus(current.id, { newStatus }));
    } catch (err) {
      this.error.set(err instanceof ApiError ? err.title : 'Failed to change order status.');
    }
  }
}
