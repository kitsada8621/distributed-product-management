import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import LinearProgress from '@mui/material/LinearProgress'
import Paper from '@mui/material/Paper'
import Skeleton from '@mui/material/Skeleton'
import Stack from '@mui/material/Stack'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { formatDate } from '@/shared/lib/formatDate'
import { paymentMethodLabel } from '../model/paymentMethod'
import { needsPolling } from '../model/status'
import type { Order } from '../types'
import { OrderStatusBadge } from './OrderStatusBadge'
import { OrderStepper } from './OrderStepper'
import { RejectionReasonList } from './RejectionReasonList'

interface OrderDetailViewProps {
  order: Order
  pollingGaveUp?: boolean
  onRefresh: () => void
  actions?: ReactNode
}

export function OrderDetailView({ order, pollingGaveUp = false, onRefresh, actions }: OrderDetailViewProps) {
  const reserving = needsPolling(order)

  return (
    <Stack spacing={2}>
      <Paper sx={{ p: 3 }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={2} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
            <div>
              <Typography variant="overline" color="text.secondary">
                คำสั่งซื้อ
              </Typography>
              <Typography component="code" sx={{ display: 'block', fontFamily: 'monospace' }}>
                {order.id}
              </Typography>
            </div>
            <OrderStatusBadge size="medium" status={order.status} stockStatus={order.stockStatus} />
          </Stack>
          <Typography color="text.secondary">สั่งเมื่อ {formatDate(order.createdAt)}</Typography>
          <OrderStepper status={order.status} stockStatus={order.stockStatus} />
          {reserving && !pollingGaveUp ? <LinearProgress /> : null}
        </Stack>
      </Paper>

      {order.ownerEmail ? (
        <Paper sx={{ p: 2 }}>
          <Typography variant="subtitle2">ข้อมูลลูกค้า</Typography>
          <Typography>{order.ownerEmail}</Typography>
        </Paper>
      ) : null}

      <Paper sx={{ p: 2 }}>
        <Typography variant="subtitle2">ที่อยู่จัดส่ง</Typography>
        <Typography>{order.shippingAddress.recipientName}</Typography>
        <Typography>{order.shippingAddress.phone}</Typography>
        <Typography>{order.shippingAddress.addressLine}</Typography>
        <Typography>
          {order.shippingAddress.province} {order.shippingAddress.postalCode}
        </Typography>
        <Typography sx={{ mt: 1 }}>วิธีชำระเงิน: {paymentMethodLabel(order.paymentMethod)}</Typography>
      </Paper>

      {reserving && !pollingGaveUp ? <Alert severity="info">กำลังตรวจสอบสต็อกของคำสั่งซื้อ…</Alert> : null}
      {reserving && pollingGaveUp ? (
        <Alert severity="warning" action={<Button onClick={onRefresh}>รีเฟรชสถานะ</Button>}>
          การตรวจสอบสต็อกใช้เวลานานกว่าปกติ กดรีเฟรชเพื่อดูสถานะล่าสุด
        </Alert>
      ) : null}
      {order.status === 'Pending' && order.stockStatus === 'Reserved' ? (
        <Alert severity="info" action={<Button onClick={onRefresh}>รีเฟรชสถานะ</Button>}>
          จองสต็อกแล้ว รอผู้ดูแลระบบยืนยัน
        </Alert>
      ) : null}
      {order.rejection ? (
        <Alert severity="error" variant="outlined">
          <Typography variant="subtitle2">เหตุผลที่คำสั่งซื้อถูกปฏิเสธ</Typography>
          <RejectionReasonList rejection={order.rejection} items={order.items} />
        </Alert>
      ) : null}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>สินค้า</TableCell>
              <TableCell align="right">ราคา</TableCell>
              <TableCell align="right">จำนวน</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {order.items.map((item) => (
              <TableRow key={item.productId}>
                <TableCell>{item.name ?? <Skeleton width={120} />}</TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {item.unitPrice === null ? <Skeleton width={60} sx={{ ml: 'auto' }} /> : formatCurrency(item.unitPrice)}
                </TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {item.quantity}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      <Typography variant="h6" sx={{ fontVariantNumeric: 'tabular-nums' }}>
        ยอดรวม {order.total === null ? '-' : formatCurrency(order.total)}
      </Typography>
      {actions}
    </Stack>
  )
}
