import Button from "@mui/material/Button";
import { Link as RouterLink, useParams } from "react-router";
import {
  CancelOrderDialog,
  OrderDetailView,
  canCancel,
  useOrder,
} from "@/features/orders";
import { isNotFound } from "@/shared/api/errorMapper";
import { PATHS } from "@/shared/config/paths";
import { useNotify } from "@/shared/hooks";
import {
  EmptyState,
  PageContainer,
  QueryErrorState,
  TableSkeleton,
} from "@/shared/ui";

export function OrderDetailPage() {
  const { id = "" } = useParams();
  const notify = useNotify();
  const {
    data: order,
    error,
    isPending,
    refetch,
    pollingGaveUp,
  } = useOrder(id);

  return (
    <PageContainer title="รายละเอียดคำสั่งซื้อ" maxWidth="md">
      {isPending ? <TableSkeleton rows={4} /> : null}
      {error && isNotFound(error) ? (
        <EmptyState
          title="ไม่พบคำสั่งซื้อ"
          action={
            <Button
              component={RouterLink}
              to={PATHS.orders}
              variant="contained"
            >
              คำสั่งซื้อของฉัน
            </Button>
          }
        />
      ) : null}
      {error && !isNotFound(error) ? (
        <QueryErrorState
          error={error}
          fallbackMessage="โหลดคำสั่งซื้อไม่สำเร็จ"
          onRetry={refetch}
        />
      ) : null}
      {order ? (
        <OrderDetailView
          order={order}
          pollingGaveUp={pollingGaveUp}
          onRefresh={refetch}
          actions={
            canCancel(order) ? (
              <CancelOrderDialog
                orderId={order.id}
                onCancelled={() => notify("ยกเลิกคำสั่งซื้อแล้ว")}
                onNoLongerCancellable={(message) => notify(message, "warning")}
              />
            ) : null
          }
        />
      ) : null}
    </PageContainer>
  );
}
