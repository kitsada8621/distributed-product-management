import { useLocation } from 'react-router'
import { loginPath } from '@/shared/config/paths'
import { ErrorPageLayout } from './ErrorPageLayout'

export function UnauthorizedPage() {
  const location = useLocation()
  const returnTo = (location.state as { returnTo?: string } | null)?.returnTo

  return (
    <ErrorPageLayout
      code="401"
      title="กรุณาเข้าสู่ระบบ"
      message="หน้านี้ต้องเข้าสู่ระบบก่อนใช้งาน"
      actionLabel="ไปหน้าเข้าสู่ระบบ"
      actionTo={loginPath(returnTo)}
    />
  )
}
