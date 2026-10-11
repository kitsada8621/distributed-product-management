import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogContentText from '@mui/material/DialogContentText'
import DialogTitle from '@mui/material/DialogTitle'
import { useState } from 'react'
import { ApiError } from '@/shared/api/types'
import { useCancelOrder } from '../hooks/useCancelOrder'

interface CancelOrderDialogProps {
  orderId: string
  onCancelled: () => void
  onNoLongerCancellable: (message: string) => void
}

function describe(error: Error): string {
  if (error instanceof ApiError && error.code === 'CANNOT_CANCEL') {
    return 'คำสั่งซื้อนี้ยกเลิกไม่ได้แล้ว ระบบอัปเดตสถานะล่าสุดให้แล้ว'
  }
  return error.message || 'ยกเลิกคำสั่งซื้อไม่สำเร็จ'
}

export function CancelOrderDialog({ orderId, onCancelled, onNoLongerCancellable }: CancelOrderDialogProps) {
  const [open, setOpen] = useState(false)
  const cancel = useCancelOrder(orderId)

  function close() {
    cancel.reset()
    setOpen(false)
  }

  async function confirm() {
    try {
      await cancel.mutateAsync()
    } catch (error) {
      if (error instanceof ApiError && error.code === 'CANNOT_CANCEL') {
        onNoLongerCancellable(describe(error))
        close()
      }
      return
    }
    onCancelled()
    close()
  }

  return (
    <>
      <Button color="error" variant="outlined" onClick={() => setOpen(true)}>
        ยกเลิกคำสั่งซื้อ
      </Button>
      <Dialog open={open} onClose={close}>
        <DialogTitle>ยกเลิกคำสั่งซื้อนี้?</DialogTitle>
        <DialogContent>
          <DialogContentText>เมื่อยกเลิกแล้วจะย้อนกลับไม่ได้</DialogContentText>
          {cancel.error ? (
            <Alert severity="error" sx={{ mt: 2 }}>
              {describe(cancel.error)}
            </Alert>
          ) : null}
        </DialogContent>
        <DialogActions>
          <Button onClick={close}>ไม่ยกเลิก</Button>
          <Button color="error" variant="contained" onClick={confirm} disabled={cancel.isPending}>
            ยืนยันยกเลิก
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
