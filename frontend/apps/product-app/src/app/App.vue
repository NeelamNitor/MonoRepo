<script setup lang="ts">
import { onMounted } from 'vue';
import { useAuthStore } from './stores/auth.store';

// Dev-only role switcher — stands in for a real login screen until an IdP is chosen (research.md item 5).
const authStore = useAuthStore();
onMounted(() => authStore.ensureToken());

function switchRole(role: 'catalog-manager' | 'viewer') {
  authStore.setUser({ userId: role === 'catalog-manager' ? 'catalog-user-1' : 'viewer-1', roles: role === 'catalog-manager' ? ['catalog-manager'] : [] });
}
</script>

<template>
  <div class="app-shell">
    <nav class="app-shell__nav">
      <div class="app-shell__nav-left">
        <router-link to="/dashboard" class="app-shell__brand">Product Catalog</router-link>
        <router-link to="/dashboard" class="app-shell__nav-link">Dashboard</router-link>
        <router-link to="/products" class="app-shell__nav-link">Products</router-link>
      </div>
      <div class="app-shell__role-switch">
        <span>Signed in as: <strong>{{ authStore.user.userId }}</strong> ({{ authStore.isCatalogManager ? 'catalog-manager' : 'viewer' }})</span>
        <button class="btn btn--sm" @click="switchRole('catalog-manager')">Act as catalog-manager</button>
        <button class="btn btn--sm" @click="switchRole('viewer')">Act as viewer</button>
      </div>
    </nav>
    <main class="app-shell__content">
      <router-view />
    </main>
  </div>
</template>
