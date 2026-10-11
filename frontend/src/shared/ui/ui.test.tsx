import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, it, vi } from 'vitest'
import { ConfirmDialog, EmptyState, ErrorState, FormField, PageContainer, Pagination, PasswordField, TableSkeleton } from '.'

it('ErrorState shows the correlation id and calls retry', async () => {
  const onRetry = vi.fn()
  render(<ErrorState message="Failed" correlationId="cid-1" onRetry={onRetry} />)

  expect(screen.getByText('cid-1')).toBeInTheDocument()
  await userEvent.click(screen.getByRole('button', { name: 'ลองใหม่' }))
  expect(onRetry).toHaveBeenCalledOnce()
})

it('ConfirmDialog calls confirm and cancel', async () => {
  const onConfirm = vi.fn()
  const onCancel = vi.fn()
  render(<ConfirmDialog open title="Delete" message="Sure?" onConfirm={onConfirm} onCancel={onCancel} />)

  await userEvent.click(screen.getByRole('button', { name: 'ยืนยัน' }))
  await userEvent.click(screen.getByRole('button', { name: 'ยกเลิก' }))

  expect(onConfirm).toHaveBeenCalledOnce()
  expect(onCancel).toHaveBeenCalledOnce()
})

it('FormField shows the field error text', () => {
  render(<FormField label="Email" error="Invalid email format" />)

  expect(screen.getByText('Invalid email format')).toBeInTheDocument()
  expect(screen.getByLabelText('Email')).toBeInvalid()
})

it('Pagination reports the chosen page', async () => {
  const onChange = vi.fn()
  render(<Pagination page={1} pageSize={20} total={60} onChange={onChange} />)

  await userEvent.click(screen.getByRole('button', { name: /page 2/i }))

  expect(onChange).toHaveBeenCalledWith(2)
})

it('Pagination renders nothing for a single page', () => {
  const { container } = render(<Pagination page={1} pageSize={20} total={5} onChange={() => {}} />)

  expect(container).toBeEmptyDOMElement()
})

it('TableSkeleton announces loading', () => {
  render(<TableSkeleton />)

  expect(screen.getByRole('status', { name: 'กำลังโหลด' })).toBeInTheDocument()
})

it('EmptyState renders the icon', () => {
  render(<EmptyState title="Empty" icon={<span data-testid="icon" />} />)

  expect(screen.getByTestId('icon')).toBeInTheDocument()
})

it('PageContainer renders the description', () => {
  render(
    <PageContainer title="Title" description="d">
      body
    </PageContainer>,
  )

  expect(screen.getByText('d')).toBeInTheDocument()
})

it('CorrelationId copies the id to the clipboard', async () => {
  const writeText = vi.fn()
  Object.assign(navigator, { clipboard: { writeText } })
  render(<ErrorState message="Failed" correlationId="cid-1" />)

  await userEvent.click(screen.getByRole('button', { name: 'คัดลอกรหัสอ้างอิง' }))

  expect(writeText).toHaveBeenCalledWith('cid-1')
})

it('ConfirmDialog renders an error-coloured confirm when destructive', () => {
  render(<ConfirmDialog open destructive title="Delete" message="Sure?" onConfirm={() => {}} onCancel={() => {}} />)

  expect(screen.getByRole('button', { name: 'ยืนยัน' })).toHaveClass('MuiButton-colorError')
})

it('PasswordField starts hidden and toggles visibility', async () => {
  render(<PasswordField label="รหัสผ่าน" />)
  const input = screen.getByLabelText('รหัสผ่าน')
  expect(input).toHaveAttribute('type', 'password')

  await userEvent.click(screen.getByRole('button', { name: 'แสดงรหัสผ่าน' }))

  expect(input).toHaveAttribute('type', 'text')
  expect(screen.getByRole('button', { name: 'ซ่อนรหัสผ่าน' })).toBeInTheDocument()
})
