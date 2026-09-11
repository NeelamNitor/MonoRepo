<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ApiError } from '@frontend/shared-api-clients';
import { formatCurrency, formatDateTime } from '@frontend/shared-util';
import type { Product } from '@frontend/shared-models';
import { useProductStore } from '../stores/product.store';
import { useAuthStore } from '../stores/auth.store';

// User Story 1, Acceptance Scenarios 2 & 3: edit price/stock (with audit trail server-side) and retire (FR-002).
const route = useRoute();
const router = useRouter();
const productStore = useProductStore();
const authStore = useAuthStore();

const product = ref<Product | null>(null);
const error = ref<string | null>(null);
const editing = ref(false);
const editPrice = ref<number | null>(null);
const editStock = ref<number | null>(null);

async function load() {
  try {
    product.value = await productStore.getById(route.params.id as string);
    editPrice.value = product.value.price;
    editStock.value = product.value.stockQuantity;
  } catch (err) {
    error.value = err instanceof ApiError ? err.title : 'Failed to load product.';
  }
}

onMounted(load);

async function saveEdits() {
  if (!product.value) return;
  try {
    product.value = await productStore.update(product.value.id, {
      price: editPrice.value ?? undefined,
      stockQuantity: editStock.value ?? undefined,
    });
    editing.value = false;
  } catch (err) {
    error.value = err instanceof ApiError ? `${err.title}: ${err.errors.join(', ')}` : 'Failed to update product.';
  }
}

async function retire() {
  if (!product.value) return;
  if (!confirm(`Retire "${product.value.name}"? It will no longer be sellable.`)) return;
  try {
    product.value = await productStore.retire(product.value.id);
  } catch (err) {
    error.value = err instanceof ApiError ? err.title : 'Failed to retire product.';
  }
}
</script>

<template>
  <div class="product-detail">
    <button class="btn" @click="router.push('/products')">&larr; Back to catalog</button>
    <p v-if="error" class="error">{{ error }}</p>

    <div v-if="product">
      <h1>{{ product.name }}</h1>
      <span :class="['badge', product.status === 'Active' ? 'badge--active' : 'badge--retired']">
        {{ product.status }}
      </span>

      <dl class="product-detail__facts">
        <dt>SKU</dt>
        <dd>{{ product.sku }}</dd>
        <dt>Description</dt>
        <dd>{{ product.description || '—' }}</dd>
        <dt>Price</dt>
        <dd v-if="!editing">{{ formatCurrency(product.price) }}</dd>
        <dd v-else><input v-model.number="editPrice" type="number" min="0" step="0.01" /></dd>
        <dt>Stock Quantity</dt>
        <dd v-if="!editing">{{ product.stockQuantity }}</dd>
        <dd v-else><input v-model.number="editStock" type="number" min="0" step="1" /></dd>
        <dt>Created</dt>
        <dd>{{ formatDateTime(product.createdAt) }}</dd>
        <dt>Last Updated</dt>
        <dd>{{ formatDateTime(product.updatedAt) }}</dd>
      </dl>

      <div v-if="authStore.isCatalogManager && product.status === 'Active'" class="product-detail__actions">
        <template v-if="editing">
          <button class="btn btn--primary" @click="saveEdits">Save</button>
          <button class="btn" @click="editing = false">Cancel</button>
        </template>
        <template v-else>
          <button class="btn" @click="editing = true">Edit Price / Stock</button>
          <button class="btn btn--danger" @click="retire">Retire Product</button>
        </template>
      </div>
    </div>
  </div>
</template>
