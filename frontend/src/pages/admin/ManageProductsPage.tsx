import AddIcon from '@mui/icons-material/Add'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router'
import {
  CategoryFilter,
  DeleteProductDialog,
  ProductSearchBar,
  ProductTable,
  useProductListParams,
  useProducts,
  type Product,
} from '@/features/products'
import { PATHS } from '@/shared/config/paths'
import { useNotify } from '@/shared/hooks'
import { EmptyState, PageContainer, Pagination, QueryErrorState, TableSkeleton } from '@/shared/ui'

export function ManageProductsPage() {
  const notify = useNotify()
  const { params, setParams, search, setSearch } = useProductListParams()
  const [deleting, setDeleting] = useState<Product | null>(null)
  const { data, error, isPending, refetch } = useProducts({
    q: params.q,
    categoryId: params.categoryId,
    page: params.page,
  })

  return (
    <PageContainer
      title="จัดการสินค้า"
      actions={
        <Button component={RouterLink} to={PATHS.adminProductNew} variant="contained" startIcon={<AddIcon />}>
          สร้างสินค้า
        </Button>
      }
    >
      <Stack direction="row" spacing={2} sx={{ mb: 3 }}>
        <ProductSearchBar value={search} onChange={setSearch} />
        <CategoryFilter value={params.categoryId} onChange={(categoryId) => setParams({ categoryId })} />
      </Stack>
      {isPending ? <TableSkeleton /> : null}
      {error ? <QueryErrorState error={error} fallbackMessage="โหลดรายการสินค้าไม่สำเร็จ" onRetry={refetch} /> : null}
      {data && data.items.length === 0 ? <EmptyState title="ไม่พบสินค้า" /> : null}
      {data && data.items.length > 0 ? <ProductTable products={data.items} onDelete={setDeleting} /> : null}
      {data ? (
        <Pagination page={params.page} pageSize={data.pageSize} total={data.total} onChange={(page) => setParams({ page })} />
      ) : null}
      <DeleteProductDialog product={deleting} onClose={() => setDeleting(null)} onDeleted={notify} />
    </PageContainer>
  )
}
