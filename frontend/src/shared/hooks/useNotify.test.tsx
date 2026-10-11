import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, it } from 'vitest'
import { NotifyProvider } from './NotifyProvider'
import { useNotify } from './useNotify'

function Trigger() {
  const notify = useNotify()
  return <button onClick={() => notify('Saved')}>go</button>
}

it('shows the message passed to notify', async () => {
  render(
    <NotifyProvider>
      <Trigger />
    </NotifyProvider>,
  )

  await userEvent.click(screen.getByRole('button', { name: 'go' }))

  expect(await screen.findByText('Saved')).toBeInTheDocument()
})
