<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { formatCurrency, formatDateTime } from '@frontend/shared-util';
import type { OrderStatus } from '@frontend/shared-models';
import { useProductStore } from '../stores/product.store';
import { useOrderStore } from '../stores/order.store';

// Combined Product + Order overview (architecture.md §1 / research.md item 4 — the "single frontend view
// needs data aggregated from both services" case). Read-only: calls both APIs directly, no writes here.
const productStore = useProductStore();
const orderStore = useOrderStore();
const router = useRouter();

onMounted(() => {
  productStore.search();
  orderStore.search();
});

const activeProductCount = computed(() => productStore.items.filter((p) => p.status === 'Active').length);
const totalRevenue = computed(() => orderStore.items.reduce((sum, o) => sum + o.totalAmount, 0));

const statusCounts = computed(() => {
  const counts: Record<OrderStatus, number> = { Placed: 0, Confirmed: 0, Shipped: 0, Delivered: 0, Cancelled: 0 };
  for (const order of orderStore.items) counts[order.status]++;
  return counts;
});

const recentProducts = computed(() => productStore.items.slice(0, 5));
const recentOrders = computed(() =>
  [...orderStore.items].sort((a, b) => b.createdAt.localeCompare(a.createdAt)).slice(0, 5)
);
</script>

<template>
  <div class="dashboard">
    <h1>Dashboard</h1>
    <p v-if="productStore.error" class="error">Products: {{ productStore.error }}</p>
    <p v-if="orderStore.error" class="error">Orders: {{ orderStore.error }}</p>

    <div class="stat-grid">
      <div class="stat-card">
        <span class="stat-card__label">Total Products</span>
        <span class="stat-card__value">{{ productStore.totalCount }}</span>
      </div>
      <div class="stat-card">
        <span class="stat-card__label">Active Products</span>
        <span class="stat-card__value">{{ activeProductCount }}</span>
      </div>
      <div class="stat-card">
        <span class="stat-card__label">Total Orders</span>
        <span class="stat-card__value">{{ orderStore.totalCount }}</span>
      </div>
      <div class="stat-card">
        <span class="stat-card__label">Order Revenue</span>
        <span class="stat-card__value">{{ formatCurrency(totalRevenue) }}</span>
      </div>
    </div>

    <div class="status-breakdown">
      <span v-for="(count, status) in statusCounts" :key="status" class="badge status-breakdown__item">
        {{ status }}: {{ count }}
      </span>
    </div>

    <div class="dashboard__panels">
      <section class="dashboard__panel">
        <header>
          <h2>Recent Products</h2>
          <button class="btn btn--sm" @click="router.push('/products')">View all</button>
        </header>
        <table class="product-table">
          <thead>
            <tr>
              <th>SKU</th>
              <th>Name</th>
              <th>Price</th>
              <th>Stock</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="product in recentProducts"
              :key="product.id"
              class="product-table__row"
              @click="router.push(`/products/${product.id}`)"
            >
              <td>{{ product.sku }}</td>
              <td>{{ product.name }}</td>
              <td>{{ formatCurrency(product.price) }}</td>
              <td>{{ product.stockQuantity }}</td>
              <td><span :class="['badge', product.status === 'Active' ? 'badge--active' : 'badge--retired']">{{ product.status }}</span></td>
            </tr>
            <tr v-if="recentProducts.length === 0">
              <td colspan="5" class="product-table__empty">No products yet.</td>
            </tr>
          </tbody>
        </table>
      </section>

      <section class="dashboard__panel">
        <header>
          <h2>Recent Orders</h2>
        </header>
        <table class="product-table">
          <thead>
            <tr>
              <th>Order</th>
              <th>Status</th>
              <th>Total</th>
              <th>Placed</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="order in recentOrders" :key="order.id">
              <td>{{ order.id.slice(0, 8) }}</td>
              <td><span class="badge">{{ order.status }}</span></td>
              <td>{{ formatCurrency(order.totalAmount) }}</td>
              <td>{{ formatDateTime(order.createdAt) }}</td>
            </tr>
            <tr v-if="recentOrders.length === 0">
              <td colspan="4" class="product-table__empty">No orders yet.</td>
            </tr>
          </tbody>
        </table>
      </section>
    </div>
  </div>
</template>
