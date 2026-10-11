import { http } from 'msw'
import { db, type MockProduct } from '../db'
import { apiUrl, authorize, fail, isAdminRequest, notFound, ok, paginate, validationFail } from '../http'

interface FieldError {
  field: string
  code: string
  message: string
}

interface ProductBody {
  name?: string
  description?: string
  price?: number
  categoryId?: string
  stock?: number
}

function toProduct(product: MockProduct, isAdmin: boolean) {
  const category = db.categories.find((candidate) => candidate.id === product.categoryId)
  return {
    id: product.id,
    name: product.name,
    description: product.description,
    price: product.price,
    categoryId: category ? product.categoryId : null,
    categoryName: category?.name ?? null,
    inStock: product.stock > 0,
    stock: isAdmin ? product.stock : null,
  }
}

function findActiveProduct(id: string | readonly string[] | undefined) {
  return db.products.find((product) => product.id === id && !product.isDeleted)
}

function validateProduct(body: ProductBody, requireStock: boolean): FieldError[] {
  const errors: FieldError[] = []
  if (!body.name?.trim()) errors.push({ field: 'name', code: 'REQUIRED', message: 'Name is required' })
  if (typeof body.price !== 'number' || body.price <= 0) {
    errors.push({ field: 'price', code: 'OUT_OF_RANGE', message: 'Price must be greater than 0' })
  }
  if (!db.categories.some((category) => category.id === body.categoryId)) {
    errors.push({ field: 'categoryId', code: 'INVALID_FORMAT', message: 'Category does not exist' })
  }
  if (requireStock && (!Number.isInteger(body.stock) || (body.stock as number) < 0)) {
    errors.push({ field: 'stock', code: 'OUT_OF_RANGE', message: 'Stock must be an integer of 0 or more' })
  }
  return errors
}

export const catalogHandlers = [
  http.get(apiUrl('/api/catalog/categories'), () => ok(db.categories)),

  http.post(apiUrl('/api/catalog/categories'), async ({ request }) => {
    const guard = authorize(request, 'admin')
    if (guard.error) return guard.error

    const body = (await request.json()) as { name?: string }
    if (!body.name?.trim()) {
      return validationFail([{ field: 'name', code: 'REQUIRED', message: 'Name is required' }])
    }
    const category = { id: crypto.randomUUID(), name: body.name.trim() }
    db.categories.push(category)
    return ok(category, { status: 201 })
  }),

  http.put(apiUrl('/api/catalog/categories/:id'), async ({ request, params }) => {
    const guard = authorize(request, 'admin')
    if (guard.error) return guard.error

    const category = db.categories.find((candidate) => candidate.id === params.id)
    if (!category) return notFound()

    const body = (await request.json()) as { name?: string }
    if (!body.name?.trim()) {
      return validationFail([{ field: 'name', code: 'REQUIRED', message: 'Name is required' }])
    }
    category.name = body.name.trim()
    return ok(category)
  }),

  http.delete(apiUrl('/api/catalog/categories/:id'), ({ request, params }) => {
    const guard = authorize(request, 'admin')
    if (guard.error) return guard.error

    const category = db.categories.find((candidate) => candidate.id === params.id)
    if (!category) return notFound()

    for (const product of db.products) {
      if (product.categoryId === category.id) product.categoryId = null
    }
    db.categories = db.categories.filter((candidate) => candidate.id !== category.id)
    return ok(null)
  }),

  http.get(apiUrl('/api/catalog/products'), ({ request }) => {
    const url = new URL(request.url)
    const q = (url.searchParams.get('q') ?? '').toLowerCase()
    const categoryId = url.searchParams.get('categoryId')

    const matches = db.products
      .filter((product) => !product.isDeleted)
      .filter((product) => product.name.toLowerCase().includes(q))
      .filter((product) => !categoryId || product.categoryId === categoryId)
      .sort((a, b) => a.name.localeCompare(b.name))

    const { rows, meta } = paginate(matches, url)
    const isAdmin = isAdminRequest(request)
    return ok(rows.map((product) => toProduct(product, isAdmin)), { meta })
  }),

  http.get(apiUrl('/api/catalog/products/:id'), ({ request, params }) => {
    const product = findActiveProduct(params.id)
    return product ? ok(toProduct(product, isAdminRequest(request))) : notFound()
  }),

  http.post(apiUrl('/api/catalog/products'), async ({ request }) => {
    const guard = authorize(request, 'admin')
    if (guard.error) return guard.error

    const body = (await request.json()) as ProductBody
    const errors = validateProduct(body, true)
    if (errors.length > 0) return validationFail(errors)

    const product: MockProduct = {
      id: crypto.randomUUID(),
      name: body.name!.trim(),
      description: body.description ?? '',
      price: body.price!,
      categoryId: body.categoryId!,
      stock: body.stock!,
      isDeleted: false,
    }
    db.products.push(product)
    return ok(toProduct(product, true), { status: 201 })
  }),

  http.put(apiUrl('/api/catalog/products/:id/stock'), async ({ request, params }) => {
    const guard = authorize(request, 'admin')
    if (guard.error) return guard.error

    const product = findActiveProduct(params.id)
    if (!product) return notFound()

    const body = (await request.json()) as { quantity?: number }
    if (!Number.isInteger(body.quantity)) {
      return validationFail([{ field: 'quantity', code: 'INVALID_FORMAT', message: 'Quantity must be an integer' }])
    }
    if ((body.quantity as number) < 0) {
      return fail(422, 'STOCK_NEGATIVE', 'Stock cannot be negative')
    }
    product.stock = body.quantity as number
    return ok({ stock: product.stock })
  }),

  http.put(apiUrl('/api/catalog/products/:id'), async ({ request, params }) => {
    const guard = authorize(request, 'admin')
    if (guard.error) return guard.error

    const product = findActiveProduct(params.id)
    if (!product) return notFound()

    const body = (await request.json()) as ProductBody
    const errors = validateProduct(body, false)
    if (errors.length > 0) return validationFail(errors)

    product.name = body.name!.trim()
    product.description = body.description ?? ''
    product.price = body.price!
    product.categoryId = body.categoryId!
    return ok(toProduct(product, true))
  }),

  http.delete(apiUrl('/api/catalog/products/:id'), ({ request, params }) => {
    const guard = authorize(request, 'admin')
    if (guard.error) return guard.error

    const product = findActiveProduct(params.id)
    if (!product) return notFound()

    product.isDeleted = true
    return ok(null)
  }),
]
