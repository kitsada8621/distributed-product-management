import { expect, it } from 'vitest'
import type { Product } from '../types'
import { emptyProductForm, productSchema, toFormValues, toPayload } from './productSchema'

const valid = { name: 'Keyboard', description: '', price: '1290', categoryId: 'c1', stock: '25' }

function failedFields(values: object) {
  const result = productSchema.safeParse(values)
  return result.success ? [] : result.error.issues.map((issue) => String(issue.path[0]))
}

it('accepts a valid product', () => {
  expect(productSchema.safeParse(valid).success).toBe(true)
})

it('requires a name, a positive price and a category', () => {
  expect(failedFields({ ...emptyProductForm })).toEqual(expect.arrayContaining(['name', 'price', 'categoryId']))
  expect(failedFields({ ...valid, price: '0' })).toEqual(['price'])
  expect(failedFields({ ...valid, price: '-5' })).toEqual(['price'])
})

it('requires stock to be a whole number of 0 or more', () => {
  expect(failedFields({ ...valid, stock: '-1' })).toEqual(['stock'])
  expect(failedFields({ ...valid, stock: '1.5' })).toEqual(['stock'])
  expect(failedFields({ ...valid, stock: '' })).toEqual(['stock'])
  expect(productSchema.safeParse({ ...valid, stock: '0' }).success).toBe(true)
})

it('converts form strings to the request payload', () => {
  expect(toPayload({ ...valid, name: ' Keyboard ' })).toEqual({
    name: 'Keyboard',
    description: '',
    price: 1290,
    categoryId: 'c1',
    stock: 25,
  })
})

it('opens a product without a category or a stock number', () => {
  const product: Product = {
    id: 'p1',
    name: 'Keyboard',
    description: 'Mechanical',
    price: 1290,
    categoryId: null,
    categoryName: null,
    inStock: true,
    stock: null,
  }

  expect(toFormValues(product)).toEqual({
    name: 'Keyboard',
    description: 'Mechanical',
    price: '1290',
    categoryId: '',
    stock: '0',
  })
})
