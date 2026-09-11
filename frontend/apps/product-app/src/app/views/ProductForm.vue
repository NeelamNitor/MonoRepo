<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { ApiError } from '@frontend/shared-api-clients';
import { useProductStore } from '../stores/product.store';

// User Story 1, Acceptance Scenario 1: create a product with name/SKU/description/price/stock (FR-001).
const productStore = useProductStore();
const router = useRouter();

const sku = ref('');
const name = ref('');
const description = ref('');
const price = ref<number | null>(null);
const stockQuantity = ref<number | null>(null);
const submitting = ref(false);
const error = ref<string | null>(null);

async function onSubmit() {
  if (!sku.value || !name.value || price.value === null || stockQuantity.value === null) {
    error.value = 'SKU, name, price, and stock quantity are required.';
    return;
  }

  submitting.value = true;
  error.value = null;
  try {
    const product = await productStore.create({
      sku: sku.value,
      name: name.value,
      description: description.value || undefined,
      price: price.value,
      stockQuantity: stockQuantity.value,
    });
    router.push(`/products/${product.id}`);
  } catch (err) {
    error.value = err instanceof ApiError ? `${err.title}: ${err.errors.join(', ')}` : 'Failed to create product.';
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <div class="product-form">
    <h1>New Product</h1>
    <p v-if="error" class="error">{{ error }}</p>

    <form @submit.prevent="onSubmit">
      <label>
        SKU
        <input v-model="sku" type="text" required />
      </label>
      <label>
        Name
        <input v-model="name" type="text" required />
      </label>
      <label>
        Description
        <textarea v-model="description"></textarea>
      </label>
      <label>
        Price
        <input v-model.number="price" type="number" min="0" step="0.01" required />
      </label>
      <label>
        Stock Quantity
        <input v-model.number="stockQuantity" type="number" min="0" step="1" required />
      </label>

      <div class="product-form__actions">
        <button type="submit" class="btn btn--primary" :disabled="submitting">Create Product</button>
        <button type="button" class="btn" @click="router.push('/products')">Cancel</button>
      </div>
    </form>
  </div>
</template>
