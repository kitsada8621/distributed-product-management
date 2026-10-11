import Card from '@mui/material/Card'
import CardActions from '@mui/material/CardActions'
import CardContent from '@mui/material/CardContent'
import Link from '@mui/material/Link'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router'
import { StockBadge } from '@/features/stock'
import { PATHS } from '@/shared/config/paths'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import type { Product } from '../types'
import { ProductThumb } from './ProductThumb'

interface ProductCardProps {
  product: Product
  action?: ReactNode
}

export function ProductCard({ product, action }: ProductCardProps) {
  const hoverSx = product.inStock ? { '&:hover': { transform: 'translateY(-2px)', boxShadow: 3 } } : {}
  return (
    <Card
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        transition: 'transform .15s, box-shadow .15s',
        opacity: product.inStock ? 1 : 0.6,
        ...hoverSx,
        '@media (prefers-reduced-motion: reduce)': { transition: 'none', '&:hover': { transform: 'none' } },
      }}
    >
      <ProductThumb categoryId={product.categoryId} />
      <CardContent sx={{ flexGrow: 1 }}>
        <StockBadge inStock={product.inStock} stock={product.stock} />
        <Link
          component={RouterLink}
          to={PATHS.productDetail(product.id)}
          variant="subtitle1"
          underline="hover"
          sx={{ display: 'block', fontWeight: 600, mt: 1 }}
        >
          {product.name}
        </Link>
        <Typography variant="body2" color="text.secondary">
          {product.categoryName ?? 'ไม่มีหมวดหมู่'}
        </Typography>
        <Typography sx={{ mt: 1, fontSize: 18, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
          {formatCurrency(product.price)}
        </Typography>
      </CardContent>
      {action ? <CardActions>{action}</CardActions> : null}
    </Card>
  )
}
