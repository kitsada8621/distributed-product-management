import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, it, vi } from 'vitest'
import { QuantityStepper } from './QuantityStepper'

function renderStepper(value: number) {
  const onChange = vi.fn()
  render(<QuantityStepper value={value} itemName="Mouse" onChange={onChange} />)
  return onChange
}

it('increases the quantity by one', async () => {
  const onChange = renderStepper(2)

  await userEvent.click(screen.getByRole('button', { name: 'เพิ่มจำนวน Mouse' }))

  expect(onChange).toHaveBeenCalledWith(3)
})

it('decreases the quantity by one', async () => {
  const onChange = renderStepper(2)

  await userEvent.click(screen.getByRole('button', { name: 'ลดจำนวน Mouse' }))

  expect(onChange).toHaveBeenCalledWith(1)
})

it('disables the decrease button at quantity 1', () => {
  renderStepper(1)

  expect(screen.getByRole('button', { name: 'ลดจำนวน Mouse' })).toBeDisabled()
})

it('reports the typed quantity', () => {
  const onChange = renderStepper(2)

  fireEvent.change(screen.getByLabelText('จำนวน Mouse'), { target: { value: '5' } })

  expect(onChange).toHaveBeenCalledWith(5)
})

it('shows the helper text for an invalid quantity', () => {
  renderStepper(0)

  expect(screen.getByText('กรอกจำนวนเต็มที่มากกว่า 0')).toBeInTheDocument()
})
