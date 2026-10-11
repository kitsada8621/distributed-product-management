import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import { useMemo, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router'
import {
  ProductForm,
  emptyProductForm,
  toFormValues,
  toPayload,
  useCreateProduct,
  useProduct,
  useUpdateProduct,
  type ProductFormValues,
} from '@/features/products'
import { useSetStock } from '@/features/stock'
import { isNotFound } from '@/shared/api/errorMapper'
import { ApiError } from '@/shared/api/types'
import { PATHS } from '@/shared/config/paths'
import { useNotify } from '@/shared/hooks'
import { EmptyState, PageContainer, QueryErrorState, TableSkeleton } from '@/shared/ui'

interface StepResult {
  ok: boolean
  message: string
}

interface Results {
  product?: StepResult
  stock?: StepResult
}

function failure(label: string, error: unknown): StepResult {
  const detail = error instanceof ApiError && error.message ? error.message : 'กรุณาลองใหม่'
  return { ok: false, message: `${label}ไม่ได้บันทึก: ${detail}` }
}

export function ProductFormPage() {
  const { id = '' } = useParams()
  const isEdit = id !== ''
  const navigate = useNavigate()
  const notify = useNotify()
  const product = useProduct(id)
  const create = useCreateProduct()
  const update = useUpdateProduct()
  const setStock = useSetStock()
  const [results, setResults] = useState<Results>({})
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [submitting, setSubmitting] = useState(false)
  const defaultValues = useMemo(
    () => (product.data ? toFormValues(product.data) : emptyProductForm),
    [product.data],
  )

  function backToList() {
    navigate(PATHS.adminProducts)
  }

  async function saveNew(values: ProductFormValues) {
    try {
      await create.mutateAsync(toPayload(values))
    } catch (error) {
      if (error instanceof ApiError) setFieldErrors(error.fieldErrors)
      setResults({ product: failure('ข้อมูลสินค้า', error) })
      return
    }
    notify('สร้างสินค้าแล้ว')
    backToList()
  }

  async function saveExisting(values: ProductFormValues) {
    const { stock, ...payload } = toPayload(values)
    try {
      await update.mutateAsync({ id, payload })
    } catch (error) {
      if (error instanceof ApiError) setFieldErrors(error.fieldErrors)
      setResults({ product: failure('ข้อมูลสินค้า', error) })
      return
    }
    const productSaved = { ok: true, message: 'บันทึกข้อมูลสินค้าแล้ว' }

    if (stock === product.data?.stock) {
      notify('บันทึกสินค้าแล้ว')
      backToList()
      return
    }
    try {
      await setStock.mutateAsync({ productId: id, quantity: stock })
    } catch (error) {
      setResults({ product: productSaved, stock: failure('สต็อก', error) })
      return
    }
    notify('บันทึกสินค้าและสต็อกแล้ว')
    backToList()
  }

  async function handleSubmit(values: ProductFormValues) {
    setSubmitting(true)
    setResults({})
    setFieldErrors({})
    if (isEdit) await saveExisting(values)
    else await saveNew(values)
    setSubmitting(false)
  }

  const title = isEdit ? 'แก้ไขสินค้า' : 'สร้างสินค้า'

  if (isEdit && product.isPending) return <PageContainer title={title} maxWidth="sm"><TableSkeleton rows={4} /></PageContainer>
  if (isEdit && isNotFound(product.error)) {
    return (
      <PageContainer title={title} maxWidth="sm">
        <EmptyState
          title="ไม่พบสินค้า"
          description="สินค้านี้อาจถูกลบไปแล้ว"
          action={
            <Button component={RouterLink} to={PATHS.adminProducts} variant="contained">
              กลับไปหน้าจัดการสินค้า
            </Button>
          }
        />
      </PageContainer>
    )
  }
  if (isEdit && product.error) {
    return (
      <PageContainer title={title} maxWidth="sm">
        <QueryErrorState error={product.error} fallbackMessage="โหลดข้อมูลสินค้าไม่สำเร็จ" onRetry={product.refetch} />
      </PageContainer>
    )
  }

  const resultAlerts = (
    <>
      {results.product ? <Alert severity={results.product.ok ? 'success' : 'error'} variant="outlined">{results.product.message}</Alert> : null}
      {results.stock ? <Alert severity={results.stock.ok ? 'success' : 'error'} variant="outlined">{results.stock.message}</Alert> : null}
    </>
  )

  return (
    <PageContainer title={title} maxWidth="sm">
      <ProductForm
        defaultValues={defaultValues}
        submitting={submitting}
        fieldErrors={fieldErrors}
        results={resultAlerts}
        onSubmit={handleSubmit}
        onCancel={backToList}
      />
    </PageContainer>
  )
}
