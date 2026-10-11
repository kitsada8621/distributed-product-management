import { http } from 'msw'
import { db, type MockOrder, type MockShippingAddress } from '../db'
import { apiUrl, authorize, fail, notFound, ok, paginate, validationFail } from '../http'

const ORDER_STATUSES = ['Pending', 'Confirmed', 'Rejected', 'Cancelled']

interface OrderItemBody {
  productId?: string
  quantity?: number
}

interface DeliveryBody {
  shippingAddress?: Partial<MockShippingAddress>
  paymentMethod?: string
}

type FieldProblem = { field: string; code: string; message: string }

const PAYMENT_METHODS = ['CASH_ON_DELIVERY', 'PAY_AT_STORE']
const PHONE_PATTERN = /^0\d{8,9}$/
const POSTAL_CODE_PATTERN = /^\d{5}$/

function touch(order: MockOrder) {
  order.updatedAt = new Date().toISOString()
}

function reserveStock(order: MockOrder) {
  const failedItems: { productId: string; reason: string; requested: number; available: number }[] = []
  for (const item of order.items) {
    const product = db.products.find((candidate) => candidate.id === item.productId && !candidate.isDeleted)
    if (!product) {
      failedItems.push({ productId: item.productId, reason: 'PRODUCT_NOT_FOUND', requested: item.quantity, available: 0 })
    } else if (product.stock < item.quantity) {
      failedItems.push({
        productId: item.productId,
        reason: 'INSUFFICIENT_STOCK',
        requested: item.quantity,
        available: product.stock,
      })
    }
  }

  if (failedItems.length > 0) {
    order.status = 'Rejected'
    order.stockStatus = null
    order.rejection = { type: 'STOCK_UNAVAILABLE', message: null, failedItems }
    touch(order)
    return
  }

  for (const item of order.items) {
    const product = db.products.find((candidate) => candidate.id === item.productId)!
    product.stock -= item.quantity
    item.name = product.name
    item.unitPrice = product.price
  }
  order.total = order.items.reduce((sum, item) => sum + item.unitPrice! * item.quantity, 0)
  order.stockStatus = 'Reserved'
  touch(order)
}

function releaseStock(order: MockOrder) {
  if (order.stockStatus !== 'Reserved') return
  for (const item of order.items) {
    const product = db.products.find((candidate) => candidate.id === item.productId)
    if (product) product.stock += item.quantity
  }
}

function toDetail(order: MockOrder, includeOwner: boolean) {
  return {
    id: order.id,
    status: order.status,
    stockStatus: order.stockStatus,
    items: order.items,
    total: order.total,
    shippingAddress: order.shippingAddress,
    paymentMethod: order.paymentMethod,
    rejection: order.rejection,
    ownerEmail: includeOwner ? order.ownerEmail : undefined,
    createdAt: order.createdAt,
    updatedAt: order.updatedAt,
  }
}

function toRow(order: MockOrder, includeOwner: boolean) {
  return {
    id: order.id,
    status: order.status,
    stockStatus: order.stockStatus,
    total: order.total,
    createdAt: order.createdAt,
    ownerEmail: includeOwner ? order.ownerEmail : undefined,
  }
}

function validateItems(items: OrderItemBody[] | undefined) {
  if (!Array.isArray(items) || items.length === 0) {
    return [{ field: 'items', code: 'REQUIRED', message: 'At least one item is required' }]
  }

  const errors: { field: string; code: string; message: string }[] = []
  const seen = new Set<string>()
  items.forEach((item, index) => {
    if (!item.productId) {
      errors.push({ field: `items[${index}].productId`, code: 'REQUIRED', message: 'Product is required' })
    } else if (seen.has(item.productId)) {
      errors.push({ field: `items[${index}].productId`, code: 'DUPLICATE', message: 'Duplicate product' })
    }
    seen.add(item.productId ?? '')
    if (!Number.isInteger(item.quantity) || (item.quantity as number) <= 0) {
      errors.push({ field: `items[${index}].quantity`, code: 'OUT_OF_RANGE', message: 'Quantity must be greater than 0' })
    }
  })
  return errors
}

function textProblem(field: string, value: string, maxLength: number): FieldProblem[] {
  if (value === '') return [{ field, code: 'REQUIRED', message: `${field} is required` }]
  if (value.length > maxLength) return [{ field, code: 'TOO_LONG', message: `${field} must be at most ${maxLength} characters` }]
  return []
}

function patternProblem(field: string, value: string, pattern: RegExp): FieldProblem[] {
  if (value === '') return [{ field, code: 'REQUIRED', message: `${field} is required` }]
  if (!pattern.test(value)) return [{ field, code: 'INVALID_FORMAT', message: `${field} has an invalid format` }]
  return []
}

function trimmedAddress(body: DeliveryBody): MockShippingAddress {
  const address = body.shippingAddress ?? {}
  return {
    recipientName: (address.recipientName ?? '').trim(),
    phone: (address.phone ?? '').trim(),
    addressLine: (address.addressLine ?? '').trim(),
    province: (address.province ?? '').trim(),
    postalCode: (address.postalCode ?? '').trim(),
  }
}

function validateDelivery(address: MockShippingAddress, paymentMethod: string | undefined): FieldProblem[] {
  const problems = [
    ...textProblem('shippingAddress.recipientName', address.recipientName, 100),
    ...patternProblem('shippingAddress.phone', address.phone, PHONE_PATTERN),
    ...textProblem('shippingAddress.addressLine', address.addressLine, 255),
    ...textProblem('shippingAddress.province', address.province, 100),
    ...patternProblem('shippingAddress.postalCode', address.postalCode, POSTAL_CODE_PATTERN),
  ]
  if (!paymentMethod) {
    problems.push({ field: 'paymentMethod', code: 'REQUIRED', message: 'paymentMethod is required' })
  } else if (!PAYMENT_METHODS.includes(paymentMethod)) {
    problems.push({ field: 'paymentMethod', code: 'INVALID_FORMAT', message: 'paymentMethod is not supported' })
  }
  return problems
}

const isPendingAndReserved = (order: MockOrder) => order.status === 'Pending' && order.stockStatus === 'Reserved'

export const orderHandlers = [
  http.post(apiUrl('/api/orders'), async ({ request }) => {
    const guard = authorize(request)
    if (guard.error) return guard.error

    const key = request.headers.get('Idempotency-Key')
    if (!key) return fail(400, 'IDEMPOTENCY_KEY_REQUIRED', 'Idempotency-Key header is required')

    const rawBody = await request.text()
    const body = JSON.parse(rawBody) as { items?: OrderItemBody[] } & DeliveryBody

    const existing = db.orders.find((order) => order.userId === guard.user.id && order.idempotencyKey === key)
    if (existing) {
      if (existing.requestBody !== rawBody) {
        return fail(409, 'IDEMPOTENCY_KEY_REUSED', 'This key was used with a different request')
      }
      return ok(
        { orderId: existing.id, status: existing.status, stockStatus: existing.stockStatus },
        { status: 200 },
      )
    }

    const address = trimmedAddress(body)
    const errors = [...validateItems(body.items), ...validateDelivery(address, body.paymentMethod)]
    if (errors.length > 0) return validationFail(errors)

    const now = new Date().toISOString()
    const order: MockOrder = {
      id: crypto.randomUUID(),
      userId: guard.user.id,
      ownerEmail: guard.user.email,
      idempotencyKey: key,
      requestBody: rawBody,
      status: 'Pending',
      stockStatus: 'Reserving',
      items: body.items!.map((item) => ({
        productId: item.productId!,
        name: null,
        unitPrice: null,
        quantity: item.quantity!,
      })),
      total: null,
      shippingAddress: address,
      paymentMethod: body.paymentMethod as MockOrder['paymentMethod'],
      rejection: null,
      createdAt: now,
      updatedAt: now,
    }
    db.orders.push(order)
    setTimeout(() => {
      if (order.status === 'Pending') reserveStock(order)
    }, db.reserveDelayMs)

    return ok({ orderId: order.id, status: 'Pending', stockStatus: 'Reserving' }, { status: 202 })
  }),

  http.get(apiUrl('/api/orders'), ({ request }) => {
    const guard = authorize(request)
    if (guard.error) return guard.error

    const url = new URL(request.url)
    const status = url.searchParams.get('status')
    if (status && !ORDER_STATUSES.includes(status)) {
      return validationFail([{ field: 'status', code: 'INVALID_FORMAT', message: 'Unknown status' }])
    }

    const own = db.orders
      .filter((order) => order.userId === guard.user.id)
      .filter((order) => !status || order.status === status)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
    const { rows, meta } = paginate(own, url)
    return ok(rows.map((order) => toRow(order, false)), { meta })
  }),

  http.get(apiUrl('/api/orders/admin'), ({ request }) => {
    const guard = authorize(request, 'admin')
    if (guard.error) return guard.error

    const url = new URL(request.url)
    const status = url.searchParams.get('status')
    if (status && !ORDER_STATUSES.includes(status)) {
      return validationFail([{ field: 'status', code: 'INVALID_FORMAT', message: 'Unknown status' }])
    }

    const all = db.orders
      .filter((order) => !status || order.status === status)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
    const { rows, meta } = paginate(all, url)
    return ok(rows.map((order) => toRow(order, true)), { meta })
  }),

  http.get(apiUrl('/api/orders/admin/:id'), ({ request, params }) => {
    const guard = authorize(request, 'admin')
    if (guard.error) return guard.error

    const order = db.orders.find((candidate) => candidate.id === params.id)
    return order ? ok(toDetail(order, true)) : notFound()
  }),

  http.get(apiUrl('/api/orders/:id'), ({ request, params }) => {
    const guard = authorize(request)
    if (guard.error) return guard.error

    const order = db.orders.find((candidate) => candidate.id === params.id && candidate.userId === guard.user.id)
    return order ? ok(toDetail(order, false)) : notFound()
  }),

  http.post(apiUrl('/api/orders/:id/cancel'), ({ request, params }) => {
    const guard = authorize(request)
    if (guard.error) return guard.error

    const order = db.orders.find((candidate) => candidate.id === params.id && candidate.userId === guard.user.id)
    if (!order) return notFound()
    if (order.status !== 'Pending') return fail(409, 'CANNOT_CANCEL', 'Only a pending order can be cancelled')

    releaseStock(order)
    order.status = 'Cancelled'
    order.stockStatus = null
    touch(order)
    return ok(toDetail(order, false))
  }),

  http.post(apiUrl('/api/orders/:id/confirm'), ({ request, params }) => {
    const guard = authorize(request, 'admin')
    if (guard.error) return guard.error

    const order = db.orders.find((candidate) => candidate.id === params.id)
    if (!order) return notFound()
    if (!isPendingAndReserved(order)) return fail(409, 'INVALID_STATE', 'The order cannot be confirmed now')

    order.status = 'Confirmed'
    order.stockStatus = null
    touch(order)
    return ok(toDetail(order, true))
  }),

  http.post(apiUrl('/api/orders/:id/reject'), async ({ request, params }) => {
    const guard = authorize(request, 'admin')
    if (guard.error) return guard.error

    const order = db.orders.find((candidate) => candidate.id === params.id)
    if (!order) return notFound()

    const body = (await request.json()) as { reason?: string }
    if (!body.reason?.trim()) {
      return validationFail([{ field: 'reason', code: 'REQUIRED', message: 'Reason is required' }])
    }
    if (!isPendingAndReserved(order)) return fail(409, 'INVALID_STATE', 'The order cannot be rejected now')

    releaseStock(order)
    order.status = 'Rejected'
    order.stockStatus = null
    order.rejection = { type: 'ADMIN_REJECTED', message: body.reason.trim(), failedItems: [] }
    touch(order)
    return ok(toDetail(order, true))
  }),
]
