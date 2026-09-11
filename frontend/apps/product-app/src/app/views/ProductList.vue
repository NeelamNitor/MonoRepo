<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { formatCurrency } from '@frontend/shared-util';
import { useProductStore } from '../stores/product.store';
import { useAuthStore } from '../stores/auth.store';

const productStore = useProductStore();
const authStore = useAuthStore();
const router = useRouter();
const query = ref('');

onMounted(() => productStore.search());

function onSearch() {
  productStore.search(query.value || undefined);
}
</script>

<template>
  <div class="product-list">
    <header class="product-list__header">
      <h1>Product Catalog</h1>
      <button v-if="authStore.isCatalogManager" class="btn btn--primary" @click="router.push('/products/new')">
        + New Product
      </button>
    </header>

    <div class="product-list__search">
      <input v-model="query" type="text" placeholder="Search by name or SKU..." @keyup.enter="onSearch" />
      <button class="btn" @click="onSearch">Search</button>
    </div>

    <p v-if="productStore.error" class="error">{{ productStore.error }}</p>
    <p v-if="productStore.loading">Loading...</p>

    <table v-else class="product-table">
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
          v-for="product in productStore.items"
          :key="product.id"
          class="product-table__row"
          @click="router.push(`/products/${product.id}`)"
        >
          <td>{{ product.sku }}</td>
          <td>{{ product.name }}</td>
          <td>{{ formatCurrency(product.price) }}</td>
          <td>{{ product.stockQuantity }}</td>
          <td>
            <span :class="['badge', product.status === 'Active' ? 'badge--active' : 'badge--retired']">
              {{ product.status }}
            </span>
          </td>
        </tr>
        <tr v-if="productStore.items.length === 0">
          <td colspan="5" class="product-table__empty">No products found.</td>
        </tr>
      </tbody>
    </table>
    <p class="product-list__total">{{ productStore.totalCount }} product(s)</p>
  </div>
</template>
