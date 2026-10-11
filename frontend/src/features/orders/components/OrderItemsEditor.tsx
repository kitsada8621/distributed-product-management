import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import IconButton from '@mui/material/IconButton'
import Paper from '@mui/material/Paper'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import type { DraftItem } from '../model/orderDraftStore'
import { isValidQuantity } from '../model/quantity'
import { QuantityStepper } from './QuantityStepper'

interface OrderItemsEditorProps {
  items: DraftItem[]
  onQuantityChange: (productId: string, quantity: number) => void
  onRemove: (productId: string) => void
}

export function OrderItemsEditor({ items, onQuantityChange, onRemove }: OrderItemsEditorProps) {
  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>สินค้า</TableCell>
            <TableCell align="right">ราคา</TableCell>
            <TableCell align="right">จำนวน</TableCell>
            <TableCell align="right">รวมย่อย</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {items.map((item) => {
            const valid = isValidQuantity(item.quantity)
            return (
              <TableRow key={item.productId}>
                <TableCell>{item.name}</TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {formatCurrency(item.unitPrice)}
                </TableCell>
                <TableCell align="right">
                  <QuantityStepper
                    value={item.quantity}
                    itemName={item.name}
                    onChange={(quantity) => onQuantityChange(item.productId, quantity)}
                  />
                </TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {valid ? formatCurrency(item.unitPrice * item.quantity) : '-'}
                </TableCell>
                <TableCell align="right">
                  <IconButton color="error" onClick={() => onRemove(item.productId)} aria-label={`ลบ ${item.name}`}>
                    <DeleteOutlinedIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </TableContainer>
  )
}
