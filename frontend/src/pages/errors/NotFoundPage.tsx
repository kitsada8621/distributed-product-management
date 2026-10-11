import { PATHS } from '@/shared/config/paths'
import { ErrorPageLayout } from './ErrorPageLayout'

export function NotFoundPage() {
  return (
    <ErrorPageLayout
      code="404"
      title="ไม่พบหน้าที่ต้องการ"
      message="หน้านี้อาจถูกลบหรือลิงก์ไม่ถูกต้อง"
      actionLabel="กลับไปหน้าสินค้า"
      actionTo={PATHS.products}
    />
  )
}
