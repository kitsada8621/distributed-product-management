import Box from '@mui/material/Box'
import type { ReactNode } from 'react'
import type { Product } from '../types'
import { ProductCard } from './ProductCard'

interface ProductGridProps {
  products: Product[]
  renderAction?: (product: Product) => ReactNode
}

export function ProductGrid({ products, renderAction }: ProductGridProps) {
  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))', gap: 2 }}>
      {products.map((product) => (
        <ProductCard key={product.id} product={product} action={renderAction?.(product)} />
      ))}
    </Box>
  )
}
