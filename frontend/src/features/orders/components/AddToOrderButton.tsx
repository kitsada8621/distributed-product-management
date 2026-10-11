import Button from '@mui/material/Button'
import { useLocation, useNavigate } from 'react-router'
import { useAuth } from '@/features/auth'
import { PATHS, loginPath } from '@/shared/config/paths'
import { useOrderDraftStore } from '../model/orderDraftStore'

interface AddToOrderButtonProps {
  product: { id: string; name: string; price: number; inStock: boolean }
}

export function AddToOrderButton({ product }: AddToOrderButtonProps) {
  const { status } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const add = useOrderDraftStore((state) => state.add)

  function handleClick() {
    if (status !== 'authed') {
      navigate(loginPath(location.pathname + location.search))
      return
    }
    add({ productId: product.id, name: product.name, unitPrice: product.price })
    navigate(PATHS.newOrder)
  }

  return (
    <Button variant="contained" size="small" fullWidth disabled={!product.inStock} onClick={handleClick}>
      {product.inStock ? 'เพิ่มในรายการ' : 'สินค้าหมด'}
    </Button>
  )
}
