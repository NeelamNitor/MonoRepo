// Mirrors ProductService's ProductDto (contracts/product-service.openapi.yaml, data-model.md).
export type ProductStatus = 'Active' | 'Retired';

export interface Product {
  id: string;
  sku: string;
  name: string;
  description: string | null;
  price: number;
  stockQuantity: number;
  status: ProductStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductRequest {
  sku: string;
  name: string;
  description?: string;
  price: number;
  stockQuantity: number;
}

export interface UpdateProductRequest {
  name?: string;
  description?: string;
  price?: number;
  stockQuantity?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}
