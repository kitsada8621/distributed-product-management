import { zodResolver } from '@hookform/resolvers/zod'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useForm } from 'react-hook-form'
import { expect, it } from 'vitest'
import { checkoutSchema, type CheckoutFormInput } from '../schemas/checkoutSchema'
import { CheckoutFields } from './CheckoutFields'

function TestForm() {
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<CheckoutFormInput>({ resolver: zodResolver(checkoutSchema) })

  return (
    <form onSubmit={handleSubmit(() => undefined)} noValidate>
      <CheckoutFields register={register} control={control} errors={errors} />
      <button type="submit">ส่ง</button>
    </form>
  )
}

it('shows the five address inputs and the payment dropdown', () => {
  render(<TestForm />)

  for (const label of ['ชื่อผู้รับ', 'เบอร์โทรศัพท์', /ที่อยู่ \(บ้านเลขที่/, 'จังหวัด', 'รหัสไปรษณีย์']) {
    expect(screen.getByLabelText(label)).toBeInTheDocument()
  }
  expect(screen.getByRole('combobox', { name: 'วิธีชำระเงิน' })).toBeInTheDocument()
})

it('lists only cash on delivery and pay at store', async () => {
  render(<TestForm />)

  await userEvent.click(screen.getByRole('combobox', { name: 'วิธีชำระเงิน' }))

  const options = within(await screen.findByRole('listbox')).getAllByRole('option')
  expect(options.map((option) => option.textContent)).toEqual(['เก็บเงินปลายทาง', 'ชำระหน้าร้าน'])
})

it('shows an error under every field when submitted empty', async () => {
  render(<TestForm />)

  await userEvent.click(screen.getByRole('button', { name: 'ส่ง' }))

  expect(await screen.findAllByText('กรุณากรอกข้อมูล')).toHaveLength(5)
  expect(screen.getByText('เลือกวิธีชำระเงิน')).toBeInTheDocument()
})
