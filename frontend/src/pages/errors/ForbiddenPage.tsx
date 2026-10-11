import { PATHS } from '@/shared/config/paths'
import { ErrorPageLayout } from './ErrorPageLayout'

export function ForbiddenPage() {
  return (
    <ErrorPageLayout
      code="403"
      title="ไม่มีสิทธิ์เข้าถึง"
      message="บัญชีของคุณไม่มีสิทธิ์ดูหน้านี้"
      actionLabel="กลับไปหน้าสินค้า"
      actionTo={PATHS.products}
    />
  )
}
