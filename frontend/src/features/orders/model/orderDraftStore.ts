import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'
import { ORDER_DRAFT_STORAGE_KEY } from '@/shared/config/storageKeys'
import { onSessionCleared } from '@/shared/lib/sessionEvents'

export interface DraftItem {
  productId: string
  name: string
  unitPrice: number
  quantity: number
}

interface OrderDraftState {
  items: DraftItem[]
  add: (item: Omit<DraftItem, 'quantity'>, quantity?: number) => void
  setQuantity: (productId: string, quantity: number) => void
  remove: (productId: string) => void
  clear: () => void
}

export const useOrderDraftStore = create<OrderDraftState>()(
  persist(
    (set) => ({
      items: [],
      add: (item, quantity = 1) =>
        set((state) => {
          const existing = state.items.find((candidate) => candidate.productId === item.productId)
          if (!existing) return { items: [...state.items, { ...item, quantity }] }
          return {
            items: state.items.map((candidate) =>
              candidate.productId === item.productId
                ? { ...candidate, quantity: candidate.quantity + quantity }
                : candidate,
            ),
          }
        }),
      setQuantity: (productId, quantity) =>
        set((state) => ({
          items: state.items.map((item) => (item.productId === productId ? { ...item, quantity } : item)),
        })),
      remove: (productId) => set((state) => ({ items: state.items.filter((item) => item.productId !== productId) })),
      clear: () => set({ items: [] }),
    }),
    {
      name: ORDER_DRAFT_STORAGE_KEY,
      storage: createJSONStorage(() => sessionStorage),
    },
  ),
)

onSessionCleared(() => useOrderDraftStore.getState().clear())
