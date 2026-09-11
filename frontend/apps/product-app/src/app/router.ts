import { createRouter, createWebHistory } from 'vue-router';
import Dashboard from './views/Dashboard.vue';
import ProductList from './views/ProductList.vue';
import ProductForm from './views/ProductForm.vue';
import ProductDetail from './views/ProductDetail.vue';

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/dashboard' },
    { path: '/dashboard', component: Dashboard },
    { path: '/products', component: ProductList },
    { path: '/products/new', component: ProductForm },
    { path: '/products/:id', component: ProductDetail },
  ],
});
