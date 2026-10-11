import Divider from '@mui/material/Divider'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import type { DraftItem } from '../model/orderDraftStore'
import { isValidQuantity } from '../model/quantity'

export function OrderSummary({ items }: { items: DraftItem[] }) {
  let estimate = 0
  let totalQuantity = 0
  for (const item of items) {
    if (!isValidQuantity(item.quantity)) continue
    estimate += item.unitPrice * item.quantity
    totalQuantity += item.quantity
  }

  return (
    <Stack spacing={1.5}>
      <Typography variant="h6">สรุปคำสั่งซื้อ</Typography>
      <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
        <Typography color="text.secondary">จำนวนรายการ</Typography>
        <Typography>{items.length}</Typography>
      </Stack>
      <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
        <Typography color="text.secondary">จำนวนชิ้นรวม</Typography>
        <Typography>{totalQuantity}</Typography>
      </Stack>
      <Divider />
      <Typography color="text.secondary">ยอดรวมโดยประมาณ</Typography>
      <Typography variant="h5" sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
        {formatCurrency(estimate)}
      </Typography>
      <Typography variant="caption" color="text.secondary">
        ยอดรวมสุดท้ายจะยืนยันหลังตรวจสอบสต็อก
      </Typography>
    </Stack>
  )
}
