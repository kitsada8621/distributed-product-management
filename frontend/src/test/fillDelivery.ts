import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

async function fillField(label: string | RegExp, value: string) {
  await userEvent.click(await screen.findByLabelText(label))
  await userEvent.paste(value)
}

export async function fillDelivery(overrides: { phone?: string; payment?: string } = {}) {
  await fillField('ชื่อผู้รับ', 'Somchai Jaidee')
  await fillField('เบอร์โทรศัพท์', overrides.phone ?? '0812345678')
  await fillField(/ที่อยู่ \(บ้านเลขที่/, '99/9 Soi Sukhumvit 21')
  await fillField('จังหวัด', 'Bangkok')
  await fillField('รหัสไปรษณีย์', '10110')
  await userEvent.click(screen.getByRole('combobox', { name: 'วิธีชำระเงิน' }))
  await userEvent.click(within(await screen.findByRole('listbox')).getByRole('option', { name: overrides.payment ?? 'เก็บเงินปลายทาง' }))
}
