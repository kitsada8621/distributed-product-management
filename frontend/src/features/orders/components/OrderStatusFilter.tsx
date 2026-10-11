import MenuItem from '@mui/material/MenuItem'
import TextField from '@mui/material/TextField'
import type { OrderStatus } from '../types'

const STATUS_LABELS: Record<OrderStatus, string> = {
  Pending: 'รอดำเนินการ',
  Confirmed: 'ยืนยันแล้ว',
  Rejected: 'ถูกปฏิเสธ',
  Cancelled: 'ยกเลิกแล้ว',
}
const STATUSES = Object.keys(STATUS_LABELS) as OrderStatus[]

interface OrderStatusFilterProps {
  status: string
  onStatusChange: (status: string) => void
}

export function OrderStatusFilter({ status, onStatusChange }: OrderStatusFilterProps) {
  return (
    <TextField
      select
      label="สถานะ"
      size="small"
      value={status}
      onChange={(event) => onStatusChange(event.target.value)}
      sx={{ minWidth: 200 }}
    >
      <MenuItem value="">ทุกสถานะ</MenuItem>
      {STATUSES.map((value) => (
        <MenuItem key={value} value={value}>
          {STATUS_LABELS[value]}
        </MenuItem>
      ))}
    </TextField>
  )
}
