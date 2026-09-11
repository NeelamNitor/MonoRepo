import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiError } from '@frontend/shared-api-clients';
import type { OrderStatus, OrderSummary } from '@frontend/shared-models';
import { formatCurrency, formatDateTime } from '@frontend/shared-util';
import { ORDER_API_CLIENT } from '../../core/api-clients';

const STATUS_OPTIONS: OrderStatus[] = ['Placed', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'];

// User Story 2, Acceptance Scenario 2 + User Story 4: view order history and filter by status (FR-010).
@Component({
  selector: 'app-order-list',
  imports: [FormsModule, RouterLink],
  template: `
    <h1>My Orders</h1>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }

    <div class="picker">
      <select [(ngModel)]="statusFilter" (ngModelChange)="load()">
        <option [ngValue]="undefined">All statuses</option>
        @for (status of statusOptions; track status) {
          <option [ngValue]="status">{{ status }}</option>
        }
      </select>
    </div>

    <table class="cart-table">
      <thead>
        <tr>
          <th>Order</th>
          <th>Status</th>
          <th>Total</th>
          <th>Placed</th>
        </tr>
      </thead>
      <tbody>
        @for (order of orders(); track order.id) {
          <tr class="order-row" [routerLink]="['/orders', order.id]">
            <td>{{ order.id.slice(0, 8) }}</td>
            <td><span class="badge">{{ order.status }}</span></td>
            <td>{{ formatCurrency(order.totalAmount) }}</td>
            <td>{{ formatDateTime(order.createdAt) }}</td>
          </tr>
        }
        @empty {
          <tr>
            <td colspan="4">No orders found.</td>
          </tr>
        }
      </tbody>
    </table>
  `,
})
export class OrderListComponent implements OnInit {
  private readonly orderApi = inject(ORDER_API_CLIENT);

  protected readonly formatCurrency = formatCurrency;
  protected readonly formatDateTime = formatDateTime;
  protected readonly statusOptions = STATUS_OPTIONS;

  statusFilter: OrderStatus | undefined = undefined;
  readonly orders = signal<OrderSummary[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit() {
    this.load();
  }

  async load() {
    try {
      const result = await this.orderApi.search(this.statusFilter);
      this.orders.set(result.items);
    } catch (err) {
      this.error.set(err instanceof ApiError ? err.title : 'Failed to load orders.');
    }
  }
}
