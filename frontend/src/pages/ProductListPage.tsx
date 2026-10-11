import SearchOffIcon from '@mui/icons-material/SearchOff'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import { AddToOrderButton } from '@/features/orders'
import {
  CategoryFilter,
  CategoryPills,
  ProductGrid,
  ProductGridSkeleton,
  ProductListHero,
  ProductSearchBar,
  useProductListParams,
  useProducts,
} from '@/features/products'
import { EmptyState, PageContainer, Pagination, QueryErrorState } from '@/shared/ui'

export function ProductListPage() {
  const { params, setParams, search, setSearch } = useProductListParams()
  const { data, error, isPending, refetch } = useProducts({
    q: params.q,
    categoryId: params.categoryId,
    page: params.page,
  })

  return (
    <PageContainer title="สินค้าทั้งหมด">
      <ProductListHero />
      <CategoryPills value={params.categoryId} onChange={(categoryId) => setParams({ categoryId })} />
      <Paper sx={{ position: 'sticky', top: 64, zIndex: 'appBar', p: 2, my: 2 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
          <ProductSearchBar value={search} onChange={setSearch} />
          <CategoryFilter value={params.categoryId} onChange={(categoryId) => setParams({ categoryId })} />
        </Stack>
      </Paper>
      {isPending ? <ProductGridSkeleton /> : null}
      {error ? <QueryErrorState error={error} fallbackMessage="โหลดรายการสินค้าไม่สำเร็จ" onRetry={refetch} /> : null}
      {data && data.items.length === 0 ? (
        <EmptyState
          icon={<SearchOffIcon fontSize="inherit" />}
          title="ไม่พบสินค้าที่ค้นหา"
          description="ลองเปลี่ยนคำค้นหรือเลือกหมวดหมู่อื่น"
        />
      ) : null}
      {data && data.items.length > 0 ? <ProductGrid products={data.items} renderAction={(product) => <AddToOrderButton product={product} />} /> : null}
      {data ? (
        <Pagination page={params.page} pageSize={data.pageSize} total={data.total} onChange={(page) => setParams({ page })} />
      ) : null}
    </PageContainer>
  )
}
