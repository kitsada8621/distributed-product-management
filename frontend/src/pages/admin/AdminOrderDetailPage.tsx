import Button from "@mui/material/Button";
import { Link as RouterLink, useParams } from "react-router";
import { OrderDecisionActions, useAdminOrder } from "@/features/admin-orders";
import { OrderDetailView, canConfirmOrReject } from "@/features/orders";
import { isNotFound } from "@/shared/api/errorMapper";
import { PATHS } from "@/shared/config/paths";
import { useNotify } from "@/shared/hooks";
import {
  EmptyState,
  PageContainer,
  QueryErrorState,
  TableSkeleton,
} from "@/shared/ui";

export function AdminOrderDetailPage() {
  const { id = "" } = useParams();
  const notify = useNotify();
  const { data: order, error, isPending, refetch } = useAdminOrder(id);

  return (
    <PageContainer title="รายละเอียดคำสั่งซื้อ" maxWidth="md">
      {isPending ? <TableSkeleton rows={4} /> : null}
      {error && isNotFound(error) ? (
        <EmptyState
          title="ไม่พบคำสั่งซื้อ"
          action={
            <Button
              component={RouterLink}
              to={PATHS.adminOrders}
              variant="contained"
            >
              คำสั่งซื้อทั้งหมด
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
          onRefresh={refetch}
          actions={
            canConfirmOrReject(order) ? (
              <OrderDecisionActions
                orderId={order.id}
                onDone={notify}
                onFailed={(message) => notify(message, "warning")}
              />
            ) : null
          }
        />
      ) : null}
    </PageContainer>
  );
}
