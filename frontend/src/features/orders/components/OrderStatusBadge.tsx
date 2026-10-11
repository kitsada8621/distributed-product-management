import CancelIcon from '@mui/icons-material/Cancel'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import HourglassTopIcon from '@mui/icons-material/HourglassTop'
import RemoveCircleOutlinedIcon from '@mui/icons-material/RemoveCircleOutlined'
import CircularProgress from '@mui/material/CircularProgress'
import { SoftChip } from '@/shared/ui'
import type { OrderStatus, StockStatus } from '../types'

interface OrderStatusBadgeProps {
  status: OrderStatus
  stockStatus: StockStatus | null
  size?: 'small' | 'medium'
}

export function OrderStatusBadge({ status, stockStatus, size = 'small' }: OrderStatusBadgeProps) {
  if (status === 'Confirmed') {
    return <SoftChip size={size} tone="success" icon={<CheckCircleIcon />} label="ยืนยันแล้ว" />
  }
  if (status === 'Rejected') {
    return <SoftChip size={size} tone="danger" icon={<CancelIcon />} label="ถูกปฏิเสธ" />
  }
  if (status === 'Cancelled') {
    return <SoftChip size={size} tone="outline" icon={<RemoveCircleOutlinedIcon />} label="ยกเลิกแล้ว" />
  }
  if (stockStatus === 'Reserved') {
    return <SoftChip size={size} tone="warning" icon={<HourglassTopIcon />} label="รอการยืนยัน" />
  }
  return <SoftChip size={size} tone="neutral" icon={<CircularProgress size={12} />} label="รอตรวจสอบสต็อก" />
}
