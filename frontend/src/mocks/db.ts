export type Role = 'user' | 'admin'

export interface MockUser {
  id: string
  email: string
  name: string
  role: Role
  password: string
}

export interface MockCategory {
  id: string
  name: string
}

export interface MockProduct {
  id: string
  name: string
  description: string
  price: number
  categoryId: string | null
  stock: number
  isDeleted: boolean
}

export interface MockOrderItem {
  productId: string
  name: string | null
  unitPrice: number | null
  quantity: number
}

export interface MockShippingAddress {
  recipientName: string
  phone: string
  addressLine: string
  province: string
  postalCode: string
}

export interface MockOrder {
  id: string
  userId: string
  ownerEmail: string
  idempotencyKey: string
  requestBody: string
  status: 'Pending' | 'Confirmed' | 'Rejected' | 'Cancelled'
  stockStatus: 'Reserving' | 'Reserved' | null
  items: MockOrderItem[]
  total: number | null
  shippingAddress: MockShippingAddress
  paymentMethod: 'CASH_ON_DELIVERY' | 'PAY_AT_STORE'
  rejection: unknown
  createdAt: string
  updatedAt: string
}

export interface MockSession {
  userId: string
  refreshToken: string
  csrfToken: string
}

interface LoginFailure {
  count: number
  lockedUntil: string | null
}

const SESSION_STORAGE_KEY = 'mock.session'

function seedUsers(): MockUser[] {
  return [
    { id: 'u-admin', email: 'admin@example.com', name: 'Admin Anna', role: 'admin', password: 'Admin123!' },
    { id: 'u-user', email: 'user@example.com', name: 'Somchai', role: 'user', password: 'Passw0rd!' },
  ]
}

function seedCategories(): MockCategory[] {
  return [
    { id: 'c-electronics', name: 'Electronics' },
    { id: 'c-books', name: 'Books' },
    { id: 'c-accessories', name: 'Accessories' },
    { id: 'c-empty', name: 'Empty category' },
  ]
}

function seedProducts(): MockProduct[] {
  const names = [
    'Keyboard', 'Mouse', 'Monitor', 'Webcam', 'Headset', 'USB Hub', 'Laptop Stand', 'Desk Lamp',
    'Notebook', 'Pen Set', 'Novel', 'Cookbook', 'Dictionary', 'Atlas', 'Mouse Pad', 'Cable Tie',
    'Phone Case', 'Charger', 'Power Bank', 'Speaker', 'Microphone', 'Tablet', 'Router', 'Flash Drive',
  ]
  const categoryIds = ['c-electronics', 'c-books', 'c-accessories']
  return names.map((name, index) => ({
    id: `p-${index + 1}`,
    name,
    description: `${name} description`,
    price: 100 + index * 50,
    categoryId: categoryIds[index % categoryIds.length],
    stock: index % 7 === 0 ? 0 : 5 + index,
    isDeleted: false,
  }))
}

function loadSession(): MockSession | null {
  try {
    const raw = localStorage.getItem(SESSION_STORAGE_KEY)
    return raw ? (JSON.parse(raw) as MockSession) : null
  } catch {
    return null
  }
}

export const db = {
  users: seedUsers(),
  categories: seedCategories(),
  products: seedProducts(),
  orders: [] as MockOrder[],
  session: loadSession(),
  loginFailures: new Map<string, LoginFailure>(),
  resetTokens: new Map<string, string>(),
  accessTokenVersion: 1,
  reserveDelayMs: 800,
}

export function saveSession(session: MockSession | null) {
  db.session = session
  try {
    if (session) localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
    else localStorage.removeItem(SESSION_STORAGE_KEY)
  } catch {
    return
  }
}

export function expireAccessTokens() {
  db.accessTokenVersion += 1
}

export function resetDb() {
  db.users = seedUsers()
  db.categories = seedCategories()
  db.products = seedProducts()
  db.orders = []
  db.loginFailures.clear()
  db.resetTokens.clear()
  db.accessTokenVersion = 1
  db.reserveDelayMs = 800
  saveSession(null)
}
