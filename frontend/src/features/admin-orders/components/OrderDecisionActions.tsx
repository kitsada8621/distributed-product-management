import WarningAmberIcon from '@mui/icons-material/WarningAmber'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import Stack from '@mui/material/Stack'
import { useState } from 'react'
import { ApiError } from '@/shared/api/types'
import { FormField } from '@/shared/ui'
import { useConfirmOrder, useRejectOrder } from '../hooks/useOrderDecision'

interface OrderDecisionActionsProps {
  orderId: string
  onDone: (message: string) => void
  onFailed: (message: string) => void
}

function describe(error: unknown): string {
  if (error instanceof ApiError && error.code === 'INVALID_STATE') {
    return 'สถานะคำสั่งซื้อเปลี่ยนแล้ว ระบบอัปเดตข้อมูลล่าสุดให้แล้ว'
  }
  return error instanceof Error && error.message ? error.message : 'ดำเนินการไม่สำเร็จ'
}

export function OrderDecisionActions({ orderId, onDone, onFailed }: OrderDecisionActionsProps) {
  const confirm = useConfirmOrder(orderId)
  const reject = useRejectOrder(orderId)
  const [rejecting, setRejecting] = useState(false)
  const [reason, setReason] = useState('')
  const [reasonError, setReasonError] = useState<string | undefined>()

  async function handleConfirm() {
    try {
      await confirm.mutateAsync()
      onDone('ยืนยันคำสั่งซื้อแล้ว')
    } catch (error) {
      onFailed(describe(error))
    }
  }

  function closeRejectDialog() {
    setRejecting(false)
    setReason('')
    setReasonError(undefined)
  }

  async function handleReject() {
    if (!reason.trim()) {
      setReasonError('กรุณาระบุเหตุผล')
      return
    }
    try {
      await reject.mutateAsync(reason.trim())
    } catch (error) {
      onFailed(describe(error))
      closeRejectDialog()
      return
    }
    onDone('ปฏิเสธคำสั่งซื้อแล้ว')
    closeRejectDialog()
  }

  return (
    <Stack direction="row" spacing={2}>
      <Button variant="contained" onClick={handleConfirm} disabled={confirm.isPending}>
        ยืนยันคำสั่งซื้อ
      </Button>
      <Button color="error" variant="outlined" onClick={() => setRejecting(true)}>
        ปฏิเสธคำสั่งซื้อ
      </Button>
      <Dialog open={rejecting} onClose={closeRejectDialog} fullWidth maxWidth="sm">
        <DialogTitle>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <WarningAmberIcon color="warning" />
            <span>ปฏิเสธคำสั่งซื้อนี้?</span>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Alert severity="warning" variant="outlined" sx={{ mb: 1 }}>
            สต็อกที่จองไว้จะถูกปล่อยคืน และลูกค้าจะเห็นเหตุผลของคุณ
          </Alert>
          <FormField
            label="เหตุผล"
            multiline
            minRows={2}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            error={reasonError}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={closeRejectDialog}>ยกเลิก</Button>
          <Button color="error" variant="contained" onClick={handleReject} disabled={reject.isPending}>
            ยืนยันปฏิเสธ
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}
