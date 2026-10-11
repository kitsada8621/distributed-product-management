import { render, screen } from '@testing-library/react'
import { expect, it } from 'vitest'
import { OrderStepper } from './OrderStepper'

function labelOf(text: string) {
  return screen.getByText(text).closest('.MuiStepLabel-root') as HTMLElement
}

it('marks the stock check step active while Reserving', () => {
  render(<OrderStepper status="Pending" stockStatus="Reserving" />)

  expect(screen.getByText('ตรวจสต็อก')).toHaveClass('Mui-active')
})

it('marks the wait-for-admin step active once Reserved', () => {
  render(<OrderStepper status="Pending" stockStatus="Reserved" />)

  expect(screen.getByText('รอยืนยัน')).toHaveClass('Mui-active')
})

it('completes every step when Confirmed', () => {
  render(<OrderStepper status="Confirmed" stockStatus={null} />)

  expect(screen.getByText('เสร็จสิ้น')).toHaveClass('Mui-completed')
})

it('shows the last step as an error when Rejected', () => {
  render(<OrderStepper status="Rejected" stockStatus={null} />)

  expect(screen.getByText('ถูกปฏิเสธ')).toHaveClass('Mui-error')
})

it('shows a cancelled last step when Cancelled', () => {
  render(<OrderStepper status="Cancelled" stockStatus={null} />)

  expect(labelOf('ยกเลิกแล้ว')).toBeInTheDocument()
  expect(screen.queryByText('เสร็จสิ้น')).not.toBeInTheDocument()
})
