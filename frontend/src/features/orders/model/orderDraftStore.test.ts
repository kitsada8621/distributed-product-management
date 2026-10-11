import { afterEach, expect, it } from 'vitest'
import { ORDER_DRAFT_STORAGE_KEY } from '@/shared/config/storageKeys'
import { emitSessionCleared } from '@/shared/lib/sessionEvents'
import { useOrderDraftStore } from './orderDraftStore'

const keyboard = { productId: 'p1', name: 'Keyboard', unitPrice: 100 }
const mouse = { productId: 'p2', name: 'Mouse', unitPrice: 50 }

afterEach(() => useOrderDraftStore.getState().clear())

it('merges a repeated product into its quantity', () => {
  const { add } = useOrderDraftStore.getState()

  add(keyboard)
  add(keyboard, 2)
  add(mouse)

  expect(useOrderDraftStore.getState().items).toEqual([
    { ...keyboard, quantity: 3 },
    { ...mouse, quantity: 1 },
  ])
})

it('sets a quantity and removes an item', () => {
  const { add, setQuantity, remove } = useOrderDraftStore.getState()
  add(keyboard)
  add(mouse)

  setQuantity('p1', 5)
  remove('p2')

  expect(useOrderDraftStore.getState().items).toEqual([{ ...keyboard, quantity: 5 }])
})

it('persists the draft in sessionStorage', () => {
  useOrderDraftStore.getState().add(keyboard)

  const saved = JSON.parse(sessionStorage.getItem(ORDER_DRAFT_STORAGE_KEY) ?? '{}')

  expect(saved.state.items).toEqual([{ ...keyboard, quantity: 1 }])
  expect(localStorage.getItem(ORDER_DRAFT_STORAGE_KEY)).toBeNull()
})

it('clears when the session ends', () => {
  useOrderDraftStore.getState().add(keyboard)

  emitSessionCleared()

  expect(useOrderDraftStore.getState().items).toEqual([])
})
