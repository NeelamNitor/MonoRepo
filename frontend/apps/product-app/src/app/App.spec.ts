import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia } from 'pinia';
import { createRouter, createWebHistory } from 'vue-router';
import App from './App.vue';

describe('App', () => {
  it('renders the catalog nav shell', async () => {
    const router = createRouter({ history: createWebHistory(), routes: [{ path: '/:pathMatch(.*)*', component: { template: '<div />' } }] });
    const wrapper = mount(App, {
      global: { plugins: [createPinia(), router] },
    });
    expect(wrapper.text()).toContain('Product Catalog');
  });
});
