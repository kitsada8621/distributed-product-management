import Link from '@mui/material/Link'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import Typography from '@mui/material/Typography'
import useMediaQuery from '@mui/material/useMediaQuery'
import { useTheme } from '@mui/material/styles'
import { Link as RouterLink } from 'react-router'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { formatDate } from '@/shared/lib/formatDate'
import type { OrderRow } from '../types'
import { OrderStatusBadge } from './OrderStatusBadge'

interface OrderTableProps {
  orders: OrderRow[]
  detailPath: (id: string) => string
  showOwner?: boolean
}

export function OrderTable({ orders, detailPath, showOwner = false }: OrderTableProps) {
  const compact = useMediaQuery(useTheme().breakpoints.down('md'))

  if (compact) {
    return (
      <Stack spacing={1.5}>
        {orders.map((order) => (
          <Paper key={order.id} sx={{ p: 2 }}>
            <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
              <Link component={RouterLink} to={detailPath(order.id)} sx={{ fontFamily: 'monospace' }}>
                {order.id.slice(0, 8)}
              </Link>
              <OrderStatusBadge status={order.status} stockStatus={order.stockStatus} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {formatDate(order.createdAt)}
            </Typography>
            {showOwner ? <Typography variant="body2">{order.ownerEmail}</Typography> : null}
            <Typography sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
              {order.total === null ? '-' : formatCurrency(order.total)}
            </Typography>
          </Paper>
        ))}
      </Stack>
    )
  }

  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>เลขที่คำสั่งซื้อ</TableCell>
            {showOwner ? <TableCell>ลูกค้า</TableCell> : null}
            <TableCell>วันที่</TableCell>
            <TableCell align="right">ยอดรวม</TableCell>
            <TableCell>สถานะ</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {orders.map((order) => (
            <TableRow key={order.id}>
              <TableCell>
                <Link component={RouterLink} to={detailPath(order.id)} sx={{ fontFamily: 'monospace' }}>
                  {order.id.slice(0, 8)}
                </Link>
              </TableCell>
              {showOwner ? <TableCell>{order.ownerEmail}</TableCell> : null}
              <TableCell>{formatDate(order.createdAt)}</TableCell>
              <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                {order.total === null ? '-' : formatCurrency(order.total)}
              </TableCell>
              <TableCell>
                <OrderStatusBadge status={order.status} stockStatus={order.stockStatus} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}
