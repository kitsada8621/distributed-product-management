import List from '@mui/material/List'
import ListItem from '@mui/material/ListItem'
import ListItemText from '@mui/material/ListItemText'
import { useProduct } from '@/features/products'
import type { OrderItem, Rejection, RejectionFailedItem } from '../types'

interface RejectionReasonListProps {
  rejection: Rejection
  items: OrderItem[]
}

export function RejectionReasonList({ rejection, items }: RejectionReasonListProps) {
  if (rejection.type === 'TIMEOUT') {
    return <ReasonLine text="ตรวจสอบสต็อกไม่ทันเวลา" />
  }
  if (rejection.type === 'ADMIN_REJECTED') {
    return <ReasonLine text={`ผู้ดูแลระบบปฏิเสธ: ${rejection.message ?? 'ไม่ระบุเหตุผล'}`} />
  }
  return (
    <List dense disablePadding>
      {rejection.failedItems.map((failed) => (
        <FailedItemLine key={failed.productId} failed={failed} items={items} />
      ))}
    </List>
  )
}

function ReasonLine({ text }: { text: string }) {
  return (
    <List dense disablePadding>
      <ListItem disableGutters>
        <ListItemText primary={text} />
      </ListItem>
    </List>
  )
}

function FailedItemLine({ failed, items }: { failed: RejectionFailedItem; items: OrderItem[] }) {
  const { data: product } = useProduct(failed.productId)
  const name = items.find((item) => item.productId === failed.productId)?.name ?? product?.name ?? failed.productId

  return (
    <ListItem disableGutters>
      <ListItemText
        primary={
          failed.reason === 'INSUFFICIENT_STOCK'
            ? `${name}: ขอ ${failed.requested} ชิ้น มีเพียง ${failed.available} ชิ้น`
            : `${name}: ไม่พบสินค้า`
        }
      />
    </ListItem>
  )
}
