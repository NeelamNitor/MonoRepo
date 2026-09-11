import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiError } from '@frontend/shared-api-clients';
import type { Product } from '@frontend/shared-models';
import { formatCurrency } from '@frontend/shared-util';
import { ORDER_API_CLIENT, PRODUCT_API_CLIENT } from '../../core/api-clients';

interface CartLine {
  product: Product;
  quantity: number;
}

// User Story 2, Acceptance Scenarios 1 & 3: select products, submit an order, and surface a clear rejection
// when requested quantity exceeds available stock (FR-003, FR-005).
@Component({
  selector: 'app-place-order',
  imports: [FormsModule],
  template: `
    <h1>Place an Order</h1>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }

    <div class="picker">
      <input [(ngModel)]="query" placeholder="Search products by name or SKU..." (keyup.enter)="search()" />
      <button class="btn" (click)="search()">Search</button>
    </div>

    <ul class="picker__results">
      @for (product of results(); track product.id) {
        <li>
          <span>{{ product.name }} — {{ formatCurrency(product.price) }} ({{ product.stockQuantity }} in stock)</span>
          <button class="btn btn--sm" (click)="addToCart(product)" [disabled]="product.stockQuantity === 0">
            Add
          </button>
        </li>
      }
    </ul>

    <h2>Cart</h2>
    @if (cart().length > 0) {
      <table class="cart-table">
        <thead>
          <tr>
            <th>Product</th>
            <th>Quantity</th>
            <th>Line Total</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (line of cart(); track line.product.id) {
            <tr>
              <td>{{ line.product.name }}</td>
              <td>
                <input
                  type="number"
                  min="1"
                  [ngModel]="line.quantity"
                  (ngModelChange)="updateQuantity(line.product.id, $event)"
                />
              </td>
              <td>{{ formatCurrency(line.product.price * line.quantity) }}</td>
              <td><button class="btn btn--sm" (click)="removeFromCart(line.product.id)">Remove</button></td>
            </tr>
          }
        </tbody>
      </table>
      <p class="cart-total">Total: {{ formatCurrency(cartTotal()) }}</p>
    } @else {
      <p>No items added yet.</p>
    }

    <button class="btn btn--primary" [disabled]="cart().length === 0 || submitting()" (click)="placeOrder()">
      {{ submitting() ? 'Placing order...' : 'Place Order' }}
    </button>
  `,
})
export class PlaceOrderComponent {
  private readonly productApi = inject(PRODUCT_API_CLIENT);
  private readonly orderApi = inject(ORDER_API_CLIENT);
  private readonly router = inject(Router);

  protected readonly formatCurrency = formatCurrency;
  query = '';
  readonly results = signal<Product[]>([]);
  readonly cart = signal<CartLine[]>([]);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  readonly cartTotal = () => this.cart().reduce((sum, line) => sum + line.product.price * line.quantity, 0);

  async search() {
    try {
      const result = await this.productApi.search(this.query || undefined, 'Active');
      this.results.set(result.items);
    } catch (err) {
      this.error.set(err instanceof ApiError ? err.title : 'Failed to search products.');
    }
  }

  addToCart(product: Product) {
    const existing = this.cart().find((line) => line.product.id === product.id);
    if (existing) {
      this.updateQuantity(product.id, existing.quantity + 1);
    } else {
      this.cart.update((lines) => [...lines, { product, quantity: 1 }]);
    }
  }

  updateQuantity(productId: string, quantity: number) {
    this.cart.update((lines) =>
      lines.map((line) => (line.product.id === productId ? { ...line, quantity: Math.max(1, quantity) } : line))
    );
  }

  removeFromCart(productId: string) {
    this.cart.update((lines) => lines.filter((line) => line.product.id !== productId));
  }

  async placeOrder() {
    this.submitting.set(true);
    this.error.set(null);
    try {
      const order = await this.orderApi.place({
        lineItems: this.cart().map((line) => ({ productId: line.product.id, quantity: line.quantity })),
      });
      this.router.navigate(['/orders', order.id]);
    } catch (err) {
      this.error.set(
        err instanceof ApiError
          ? err.status === 409
            ? 'One or more items no longer have enough stock available.'
            : err.title
          : 'Failed to place order.'
      );
    } finally {
      this.submitting.set(false);
    }
  }
}
