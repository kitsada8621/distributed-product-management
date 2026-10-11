import { z } from 'zod'
import type { Product } from '../types'

export const productSchema = z.object({
  name: z.string().trim().min(1, 'Name is required'),
  description: z.string(),
  price: z.string().refine((value) => Number(value) > 0, 'Price must be greater than 0'),
  categoryId: z.string().min(1, 'Select a category'),
  stock: z.string().regex(/^\d+$/, 'Stock must be a whole number of 0 or more'),
})

export type ProductFormValues = z.infer<typeof productSchema>

export const emptyProductForm: ProductFormValues = {
  name: '',
  description: '',
  price: '',
  categoryId: '',
  stock: '0',
}

export function toFormValues(product: Product): ProductFormValues {
  return {
    name: product.name,
    description: product.description,
    price: String(product.price),
    categoryId: product.categoryId ?? '',
    stock: String(product.stock ?? 0),
  }
}

export function toPayload(values: ProductFormValues) {
  return {
    name: values.name.trim(),
    description: values.description,
    price: Number(values.price),
    categoryId: values.categoryId,
    stock: Number(values.stock),
  }
}
