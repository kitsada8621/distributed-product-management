import { render, screen } from '@testing-library/react'
import { expect, it } from 'vitest'
import { OrderStatusBadge } from './OrderStatusBadge'

it.each([
  ['Pending', 'Reserving', 'รอตรวจสอบสต็อก'],
  ['Pending', 'Reserved', 'รอการยืนยัน'],
  ['Confirmed', null, 'ยืนยันแล้ว'],
  ['Rejected', null, 'ถูกปฏิเสธ'],
  ['Cancelled', null, 'ยกเลิกแล้ว'],
] as const)('%s / %s shows "%s"', (status, stockStatus, label) => {
  render(<OrderStatusBadge status={status} stockStatus={stockStatus} />)

  expect(screen.getByText(label)).toBeInTheDocument()
})

it('has an icon for the terminal states, not colour alone', () => {
  render(<OrderStatusBadge status="Rejected" stockStatus={null} />)

  expect(screen.getByTestId('CancelIcon')).toBeInTheDocument()
})

it('renders a larger chip when size is medium', () => {
  render(<OrderStatusBadge status="Confirmed" stockStatus={null} size="medium" />)

  expect(screen.getByText('ยืนยันแล้ว').closest('.MuiChip-root')).toHaveClass('MuiChip-sizeMedium')
})
