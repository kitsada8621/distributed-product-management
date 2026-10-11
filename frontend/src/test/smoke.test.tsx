import Button from '@mui/material/Button'
import { render, screen } from '@testing-library/react'
import { expect, it } from 'vitest'

it('renders a MUI button by role', () => {
  render(<Button>Save</Button>)

  expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
})
