import Alert from '@mui/material/Alert'
import { useEffect, useState } from 'react'

function formatRemaining(ms: number): string {
  const totalSeconds = Math.max(0, Math.ceil(ms / 1000))
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = String(totalSeconds % 60).padStart(2, '0')
  return `${minutes}:${seconds}`
}

export function LockedNotice({ lockedUntil }: { lockedUntil: string }) {
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(timer)
  }, [])

  const remaining = new Date(lockedUntil).getTime() - now
  if (remaining <= 0) {
    return <Alert severity="info" variant="outlined">
        การล็อกสิ้นสุดแล้ว ลองเข้าสู่ระบบอีกครั้งได้
      </Alert>
  }
  return (
    <Alert severity="warning" variant="outlined">
      ใส่รหัสผิดหลายครั้ง บัญชีถูกล็อกชั่วคราว ลองใหม่ในอีก {formatRemaining(remaining)}
    </Alert>
  )
}
