import Grid from '@mui/material/Grid'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'
import { StockBadge } from '@/features/stock'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import type { Product } from '../types'
import { ProductThumb } from './ProductThumb'

interface ProductDetailProps {
  product: Product
  action?: ReactNode
}

export function ProductDetail({ product, action }: ProductDetailProps) {
  return (
    <Grid container spacing={4}>
      <Grid size={{ xs: 12, md: 6 }}>
        <Paper sx={{ overflow: 'hidden' }}>
          <ProductThumb categoryId={product.categoryId} />
        </Paper>
      </Grid>
      <Grid size={{ xs: 12, md: 6 }}>
        <Stack spacing={2}>
          <Typography variant="h4" component="h2">
            {product.name}
          </Typography>
          <StockBadge inStock={product.inStock} stock={product.stock} />
          <Typography variant="h5" sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
            {formatCurrency(product.price)}
          </Typography>
          <Typography color="text.secondary">{product.categoryName ?? 'ไม่มีหมวดหมู่'}</Typography>
          <Typography>{product.description || 'ไม่มีรายละเอียด'}</Typography>
          {action}
        </Stack>
      </Grid>
    </Grid>
  )
}
