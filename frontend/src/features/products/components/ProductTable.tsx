import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import IconButton from '@mui/material/IconButton'
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
import { StockBadge } from '@/features/stock'
import { PATHS } from '@/shared/config/paths'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import type { Product } from '../types'

interface ProductTableProps {
  products: Product[]
  onDelete: (product: Product) => void
}

function ProductActions({ product, onDelete }: { product: Product; onDelete: (product: Product) => void }) {
  return (
    <>
      <IconButton component={RouterLink} to={PATHS.adminProductEdit(product.id)} aria-label={`แก้ไข ${product.name}`}>
        <EditOutlinedIcon />
      </IconButton>
      <IconButton color="error" aria-label={`ลบ ${product.name}`} onClick={() => onDelete(product)}>
        <DeleteOutlinedIcon />
      </IconButton>
    </>
  )
}

export function ProductTable({ products, onDelete }: ProductTableProps) {
  const compact = useMediaQuery(useTheme().breakpoints.down('md'))

  if (compact) {
    return (
      <Stack spacing={1.5}>
        {products.map((product) => (
          <Paper key={product.id} sx={{ p: 2 }}>
            <Typography sx={{ fontWeight: 600 }}>{product.name}</Typography>
            <Typography sx={{ fontVariantNumeric: 'tabular-nums' }}>{formatCurrency(product.price)}</Typography>
            <Typography variant="body2" color="text.secondary">
              {product.categoryName ?? 'ไม่มีหมวดหมู่'}
            </Typography>
            <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mt: 1 }}>
              <StockBadge inStock={product.inStock} stock={product.stock} />
              <div>
                <ProductActions product={product} onDelete={onDelete} />
              </div>
            </Stack>
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
            <TableCell>ชื่อสินค้า</TableCell>
            <TableCell align="right">ราคา</TableCell>
            <TableCell>หมวดหมู่</TableCell>
            <TableCell>สต็อก</TableCell>
            <TableCell align="right">จัดการ</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {products.map((product) => (
            <TableRow key={product.id}>
              <TableCell>{product.name}</TableCell>
              <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                {formatCurrency(product.price)}
              </TableCell>
              <TableCell>{product.categoryName ?? 'ไม่มีหมวดหมู่'}</TableCell>
              <TableCell>
                <StockBadge inStock={product.inStock} stock={product.stock} />
              </TableCell>
              <TableCell align="right">
                <ProductActions product={product} onDelete={onDelete} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}
