import { Route } from '@angular/router';

export const appRoutes: Route[] = [
  { path: '', redirectTo: 'orders', pathMatch: 'full' },
  { path: 'orders', loadComponent: () => import('./features/order-list/order-list.component').then((m) => m.OrderListComponent) },
  { path: 'orders/new', loadComponent: () => import('./features/place-order/place-order.component').then((m) => m.PlaceOrderComponent) },
  { path: 'orders/:id', loadComponent: () => import('./features/order-detail/order-detail.component').then((m) => m.OrderDetailComponent) },
];
