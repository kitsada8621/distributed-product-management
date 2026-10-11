import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogContentText from '@mui/material/DialogContentText'
import DialogTitle from '@mui/material/DialogTitle'
import { useDeleteProduct } from '../hooks/useDeleteProduct'
import type { Product } from '../types'

interface DeleteProductDialogProps {
  product: Product | null
  onClose: () => void
  onDeleted: (message: string) => void
}

export function DeleteProductDialog({ product, onClose, onDeleted }: DeleteProductDialogProps) {
  const remove = useDeleteProduct()

  function handleClose() {
    remove.reset()
    onClose()
  }

  async function confirm() {
    if (!product) return
    try {
      await remove.mutateAsync(product.id)
    } catch {
      return
    }
    onDeleted('ลบสินค้าแล้ว')
    handleClose()
  }

  return (
    <Dialog open={product !== null} onClose={handleClose}>
      <DialogTitle>ลบสินค้า?</DialogTitle>
      <DialogContent>
        <DialogContentText>
          สินค้าจะถูกซ่อนจากหน้าร้าน (soft delete) คำสั่งซื้อเดิมไม่ได้รับผลกระทบ
        </DialogContentText>
        {remove.error ? (
          <Alert severity="error" variant="outlined" sx={{ mt: 2 }}>
            {remove.error.message || 'ลบสินค้าไม่สำเร็จ'}
          </Alert>
        ) : null}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>ยกเลิก</Button>
        <Button color="error" variant="contained" onClick={confirm} disabled={remove.isPending}>
          ลบสินค้า
        </Button>
      </DialogActions>
    </Dialog>
  )
}
