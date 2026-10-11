import { render, screen } from '@testing-library/react'
import { expect, it, vi } from 'vitest'
import type { Order } from '../types'
import { OrderDetailView } from './OrderDetailView'

const order: Order = {
  id: 'o-1',
  status: 'Pending',
  stockStatus: 'Reserved',
  items: [{ productId: 'p-2', name: 'Mouse', unitPrice: 150, quantity: 2 }],
  total: 300,
  shippingAddress: {
    recipientName: 'Somchai Jaidee',
    phone: '0812345678',
    addressLine: '99/9 Soi Sukhumvit 21',
    province: 'Bangkok',
    postalCode: '10110',
  },
  paymentMethod: 'CASH_ON_DELIVERY',
  rejection: null,
  createdAt: '2026-10-10T10:00:00Z',
  updatedAt: '2026-10-10T10:00:00Z',
}

it('shows the shipping address and the cash on delivery label', () => {
  render(<OrderDetailView order={order} onRefresh={vi.fn()} />)

  expect(screen.getByText('ที่อยู่จัดส่ง')).toBeInTheDocument()
  expect(screen.getByText('Somchai Jaidee')).toBeInTheDocument()
  expect(screen.getByText('0812345678')).toBeInTheDocument()
  expect(screen.getByText('99/9 Soi Sukhumvit 21')).toBeInTheDocument()
  expect(screen.getByText('Bangkok 10110')).toBeInTheDocument()
  expect(screen.getByText('วิธีชำระเงิน: เก็บเงินปลายทาง')).toBeInTheDocument()
})

it('shows the pay at store label', () => {
  render(<OrderDetailView order={{ ...order, paymentMethod: 'PAY_AT_STORE' }} onRefresh={vi.fn()} />)

  expect(screen.getByText('วิธีชำระเงิน: ชำระหน้าร้าน')).toBeInTheDocument()
})
