import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined'
import Button from '@mui/material/Button'
import { Link as RouterLink } from 'react-router'
import { OrderStatusFilter, OrderTable, useMyOrders } from '@/features/orders'
import { PATHS } from '@/shared/config/paths'
import { usePagination } from '@/shared/hooks'
import { EmptyState, PageContainer, Pagination, QueryErrorState, TableSkeleton } from '@/shared/ui'

export function MyOrdersPage() {
  const { params, setParams } = usePagination()
  const { data, error, isPending, refetch } = useMyOrders({ status: params.status, page: params.page })

  return (
    <PageContainer title="คำสั่งซื้อของฉัน">
      <OrderStatusFilter status={params.status} onStatusChange={(status) => setParams({ status })} />
      {isPending ? <TableSkeleton /> : null}
      {error ? <QueryErrorState error={error} fallbackMessage="โหลดคำสั่งซื้อไม่สำเร็จ" onRetry={refetch} /> : null}
      {data && data.items.length === 0 ? (
        <EmptyState
          icon={<ReceiptLongOutlinedIcon fontSize="inherit" />}
          title="ยังไม่มีคำสั่งซื้อ"
          description="เมื่อคุณสั่งซื้อสินค้า รายการจะแสดงที่นี่"
          action={
            <Button component={RouterLink} to={PATHS.products} variant="contained">
              เลือกซื้อสินค้า
            </Button>
          }
        />
      ) : null}
      {data && data.items.length > 0 ? <OrderTable orders={data.items} detailPath={PATHS.orderDetail} /> : null}
      {data ? (
        <Pagination page={params.page} pageSize={data.pageSize} total={data.total} onChange={(page) => setParams({ page })} />
      ) : null}
    </PageContainer>
  )
}
