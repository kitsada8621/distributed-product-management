export const PATHS = {
  home: '/',
  login: '/login',
  register: '/register',
  forgotPassword: '/forgot-password',
  resetPassword: '/reset-password',
  products: '/products',
  productDetail: (id: string) => `/products/${id}`,
  newOrder: '/orders/new',
  orders: '/orders',
  orderDetail: (id: string) => `/orders/${id}`,
  adminProducts: '/admin/products',
  adminProductNew: '/admin/products/new',
  adminProductEdit: (id: string) => `/admin/products/${id}`,
  adminCategories: '/admin/categories',
  adminOrders: '/admin/orders',
  adminOrderDetail: (id: string) => `/admin/orders/${id}`,
  unauthorized: '/401',
  forbidden: '/403',
} as const

export function loginPath(returnTo?: string, sessionExpired = false): string {
  const params = new URLSearchParams()
  if (returnTo) params.set('returnTo', returnTo)
  if (sessionExpired) params.set('expired', '1')
  const query = params.toString()
  return query ? `${PATHS.login}?${query}` : PATHS.login
}

export function safeReturnTo(value: string | null): string {
  if (!value || !value.startsWith('/') || value.startsWith('//')) return PATHS.products
  return value
}
