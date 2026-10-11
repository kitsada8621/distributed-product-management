import { zodResolver } from '@hookform/resolvers/zod'
import ShoppingCartOutlinedIcon from '@mui/icons-material/ShoppingCartOutlined'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Grid from '@mui/material/Grid'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useForm, useWatch, type FieldPath } from 'react-hook-form'
import { Link as RouterLink, useNavigate } from 'react-router'
import {
  CheckoutFields,
  OrderItemsEditor,
  OrderSummary,
  checkoutSchema,
  isValidQuantity,
  useCreateOrder,
  useIdempotencyKey,
  useOrderDraftStore,
  type CheckoutFormInput,
} from '@/features/orders'
import { isServiceUnavailable } from '@/shared/api/errorMapper'
import { ApiError } from '@/shared/api/types'
import { PATHS } from '@/shared/config/paths'
import { useNotify } from '@/shared/hooks'
import { EmptyState, PageContainer, ServiceUnavailableBanner } from '@/shared/ui'

const EMPTY_ADDRESS = { recipientName: '', phone: '', addressLine: '', province: '', postalCode: '' }

function isDeliveryField(path: string): path is FieldPath<CheckoutFormInput> {
  return path.startsWith('shippingAddress.') || path === 'paymentMethod'
}

export function CreateOrderPage() {
  const navigate = useNavigate()
  const notify = useNotify()
  const { items, setQuantity, remove, clear } = useOrderDraftStore()
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<CheckoutFormInput>({
    resolver: zodResolver(checkoutSchema),
    defaultValues: { shippingAddress: EMPTY_ADDRESS },
  })
  const delivery = useWatch({ control })
  const idempotencyKey = useIdempotencyKey(items, delivery)
  const createOrder = useCreateOrder()
  const allValid = items.length > 0 && items.every((item) => isValidQuantity(item.quantity))

  function showServerFieldErrors(error: ApiError) {
    for (const [path, message] of Object.entries(error.fieldErrors)) {
      if (isDeliveryField(path)) setError(path, { message })
    }
  }

  async function confirm(values: CheckoutFormInput) {
    const order = {
      items: items.map((item) => ({ productId: item.productId, quantity: item.quantity })),
      shippingAddress: values.shippingAddress,
      paymentMethod: values.paymentMethod,
    }
    try {
      const created = await createOrder.mutateAsync({ order, idempotencyKey })
      clear()
      notify('ส่งคำสั่งซื้อแล้ว')
      navigate(PATHS.orderDetail(created.orderId))
    } catch (caught) {
      if (caught instanceof ApiError) showServerFieldErrors(caught)
    }
  }

  if (items.length === 0) {
    return (
      <PageContainer title="สร้างคำสั่งซื้อ" maxWidth="lg">
        <EmptyState
          icon={<ShoppingCartOutlinedIcon fontSize="inherit" />}
          title="ยังไม่มีสินค้าในรายการ"
          description="เลือกสินค้าก่อน แล้วกลับมายืนยันคำสั่งซื้อ"
          action={
            <Button component={RouterLink} to={PATHS.products} variant="contained">
              เลือกสินค้า
            </Button>
          }
        />
      </PageContainer>
    )
  }

  const error = createOrder.error
  const serviceDown = error instanceof ApiError && isServiceUnavailable(error)
  return (
    <PageContainer title="สร้างคำสั่งซื้อ" maxWidth="lg">
      <Stack spacing={2}>
        {serviceDown ? (
          <ServiceUnavailableBanner message='ติดต่อเซิร์ฟเวอร์ไม่ได้ คำสั่งซื้อของคุณยังอยู่ กด "ลองใหม่" เพื่อส่งอีกครั้งโดยไม่เกิดรายการซ้ำ' />
        ) : null}
        {error && !serviceDown ? (
          <Alert severity="error" variant="outlined">
            {error.message || 'ส่งคำสั่งซื้อไม่สำเร็จ'}
          </Alert>
        ) : null}
        <Grid container spacing={3}>
          <Grid size={{ xs: 12, md: 8 }}>
            <Stack spacing={3}>
              <OrderItemsEditor items={items} onQuantityChange={setQuantity} onRemove={remove} />
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" component="h2" gutterBottom>
                  ที่อยู่จัดส่งและวิธีชำระเงิน
                </Typography>
                <CheckoutFields register={register} control={control} errors={errors} />
              </Paper>
            </Stack>
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <Paper sx={{ p: 3, position: { md: 'sticky' }, top: 88 }}>
              <OrderSummary items={items} />
              <Stack spacing={1.5} sx={{ mt: 3 }}>
                <Button
                  variant="contained"
                  fullWidth
                  onClick={handleSubmit(confirm)}
                  disabled={!allValid || createOrder.isPending}
                >
                  {error ? 'ลองใหม่' : 'ยืนยันคำสั่งซื้อ'}
                </Button>
                <Button variant="outlined" fullWidth component={RouterLink} to={PATHS.products}>
                  เพิ่มสินค้า
                </Button>
              </Stack>
            </Paper>
          </Grid>
        </Grid>
      </Stack>
    </PageContainer>
  )
}
