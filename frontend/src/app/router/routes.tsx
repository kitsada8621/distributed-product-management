import { Navigate, type RouteObject } from 'react-router'
import { AppLayout } from '../layouts/AppLayout'
import { GuestOnly } from './guards/GuestOnly'
import { RequireAuth } from './guards/RequireAuth'
import { PATHS } from '@/shared/config/paths'

const guestOnlyRoutes: RouteObject[] = [
  {
    path: PATHS.login,
    lazy: async () => ({ Component: (await import('@/pages/LoginPage')).LoginPage }),
  },
  {
    path: PATHS.register,
    lazy: async () => ({ Component: (await import('@/pages/RegisterPage')).RegisterPage }),
  },
  {
    path: PATHS.forgotPassword,
    lazy: async () => ({ Component: (await import('@/pages/ForgotPasswordPage')).ForgotPasswordPage }),
  },
]
const publicRoutes: RouteObject[] = [
  {
    path: PATHS.products,
    lazy: async () => ({ Component: (await import('@/pages/ProductListPage')).ProductListPage }),
  },
  {
    path: '/products/:id',
    lazy: async () => ({ Component: (await import('@/pages/ProductDetailPage')).ProductDetailPage }),
  },
  {
    path: PATHS.resetPassword,
    lazy: async () => ({ Component: (await import('@/pages/ResetPasswordPage')).ResetPasswordPage }),
  },
]
const signedInRoutes: RouteObject[] = [
  {
    path: PATHS.orders,
    lazy: async () => ({ Component: (await import('@/pages/MyOrdersPage')).MyOrdersPage }),
  },
  {
    path: '/orders/:id',
    lazy: async () => ({ Component: (await import('@/pages/OrderDetailPage')).OrderDetailPage }),
  },
  {
    path: PATHS.newOrder,
    lazy: async () => ({ Component: (await import('@/pages/CreateOrderPage')).CreateOrderPage }),
  },
]
const adminRoutes: RouteObject[] = [
  {
    path: '/admin/orders/:id',
    lazy: async () => ({ Component: (await import('@/pages/admin/AdminOrderDetailPage')).AdminOrderDetailPage }),
  },
  {
    path: PATHS.adminOrders,
    lazy: async () => ({ Component: (await import('@/pages/admin/AllOrdersPage')).AllOrdersPage }),
  },
  {
    path: PATHS.adminProductNew,
    lazy: async () => ({ Component: (await import('@/pages/admin/ProductFormPage')).ProductFormPage }),
  },
  {
    path: '/admin/products/:id',
    lazy: async () => ({ Component: (await import('@/pages/admin/ProductFormPage')).ProductFormPage }),
  },
  {
    path: PATHS.adminProducts,
    lazy: async () => ({ Component: (await import('@/pages/admin/ManageProductsPage')).ManageProductsPage }),
  },
  {
    path: PATHS.adminCategories,
    lazy: async () => ({ Component: (await import('@/pages/admin/ManageCategoriesPage')).ManageCategoriesPage }),
  },
]
const fallbackRoutes: RouteObject[] = [
  {
    path: PATHS.unauthorized,
    lazy: async () => ({ Component: (await import('@/pages/errors/UnauthorizedPage')).UnauthorizedPage }),
  },
  {
    path: PATHS.forbidden,
    lazy: async () => ({ Component: (await import('@/pages/errors/ForbiddenPage')).ForbiddenPage }),
  },
  {
    path: '*',
    lazy: async () => ({ Component: (await import('@/pages/errors/NotFoundPage')).NotFoundPage }),
  },
]

export const routes: RouteObject[] = [
  { path: PATHS.home, element: <Navigate to={PATHS.products} replace /> },
  {
    element: <AppLayout />,
    children: [
      { element: <GuestOnly />, children: guestOnlyRoutes },
      ...publicRoutes,
      { element: <RequireAuth />, children: signedInRoutes },
      { element: <RequireAuth role="admin" />, children: adminRoutes },
      ...fallbackRoutes,
    ],
  },
]
