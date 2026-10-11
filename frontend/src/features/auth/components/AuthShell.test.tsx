import { render, screen } from '@testing-library/react'
import { expect, it } from 'vitest'
import { AuthShell } from './AuthShell'

it('renders the title heading, the children and the tagline', () => {
  render(
    <AuthShell title="เข้าสู่ระบบ">
      <button type="button">Child action</button>
    </AuthShell>,
  )

  expect(screen.getByRole('heading', { level: 1, name: 'เข้าสู่ระบบ' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Child action' })).toBeInTheDocument()
  expect(screen.getByText('เห็นสต็อกจริง ติดตามทุกคำสั่งซื้อ')).toBeInTheDocument()
})
