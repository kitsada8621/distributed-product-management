import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined'
import { AdminOrderTable, useAdminOrders } from '@/features/admin-orders'
import { OrderStatusFilter } from '@/features/orders'
import { usePagination } from '@/shared/hooks'
import { EmptyState, PageContainer, Pagination, QueryErrorState, TableSkeleton } from '@/shared/ui'

export function AllOrdersPage() {
  const { params, setParams } = usePagination()
  const { data, error, isPending, refetch } = useAdminOrders({ status: params.status, page: params.page })

  return (
    <PageContainer title="คำสั่งซื้อทั้งหมด">
      <OrderStatusFilter status={params.status} onStatusChange={(status) => setParams({ status })} />
      {isPending ? <TableSkeleton /> : null}
      {error ? <QueryErrorState error={error} fallbackMessage="โหลดคำสั่งซื้อไม่สำเร็จ" onRetry={refetch} /> : null}
      {data && data.items.length === 0 ? (
        <EmptyState icon={<ReceiptLongOutlinedIcon fontSize="inherit" />} title="ไม่มีคำสั่งซื้อในสถานะนี้" />
      ) : null}
      {data && data.items.length > 0 ? <AdminOrderTable orders={data.items} /> : null}
      {data ? (
        <Pagination page={params.page} pageSize={data.pageSize} total={data.total} onChange={(page) => setParams({ page })} />
      ) : null}
    </PageContainer>
  )
}
