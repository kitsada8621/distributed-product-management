import Button from '@mui/material/Button'
import { Link as RouterLink, useParams } from 'react-router'
import { AddToOrderButton } from '@/features/orders'
import { ProductDetail, useProduct } from '@/features/products'
import { isNotFound } from '@/shared/api/errorMapper'
import { PATHS } from '@/shared/config/paths'
import { EmptyState, PageContainer, QueryErrorState, TableSkeleton } from '@/shared/ui'

export function ProductDetailPage() {
  const { id = '' } = useParams()
  const { data: product, error, isPending, refetch } = useProduct(id)

  return (
    <PageContainer title="สินค้า" maxWidth="md">
      {isPending ? <TableSkeleton rows={3} /> : null}
      {error && isNotFound(error) ? (
        <EmptyState
          title="ไม่พบสินค้า"
          description="สินค้านี้อาจถูกนำออกแล้ว"
          action={
            <Button component={RouterLink} to={PATHS.products} variant="contained">
              กลับไปหน้าสินค้า
            </Button>
          }
        />
      ) : null}
      {error && !isNotFound(error) ? (
        <QueryErrorState error={error} fallbackMessage="โหลดข้อมูลสินค้าไม่สำเร็จ" onRetry={refetch} />
      ) : null}
      {product ? <ProductDetail product={product} action={<AddToOrderButton product={product} />} /> : null}
    </PageContainer>
  )
}
