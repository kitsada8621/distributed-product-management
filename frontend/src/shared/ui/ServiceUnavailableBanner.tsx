import Alert from '@mui/material/Alert'

export function ServiceUnavailableBanner({ message }: { message?: string }) {
  return (
    <Alert severity="warning" variant="outlined" role="alert">
      {message ?? 'ระบบไม่พร้อมให้บริการชั่วคราว กรุณาลองใหม่อีกครั้งในอีกสักครู่'}
    </Alert>
  )
}
