import CancelIcon from '@mui/icons-material/Cancel'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import { SoftChip } from '@/shared/ui'

interface StockBadgeProps {
  inStock: boolean
  stock?: number | null
}

export function StockBadge({ inStock, stock }: StockBadgeProps) {
  if (!inStock) {
    return <SoftChip tone="neutral" icon={<CancelIcon />} label="สินค้าหมด" />
  }
  const label = typeof stock === 'number' ? `มีของ · ${stock} ชิ้น` : 'มีของ'
  return <SoftChip tone="success" icon={<CheckCircleIcon />} label={label} />
}
