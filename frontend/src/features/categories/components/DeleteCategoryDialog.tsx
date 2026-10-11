import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogContentText from '@mui/material/DialogContentText'
import DialogTitle from '@mui/material/DialogTitle'
import { useDeleteCategory } from '../hooks/useCategoryMutations'
import type { Category } from '../types'

interface DeleteCategoryDialogProps {
  category: Category | null
  onClose: () => void
  onDeleted: (message: string) => void
}

function describe(error: Error): string {
  return error.message || 'ลบหมวดหมู่ไม่สำเร็จ'
}

export function DeleteCategoryDialog({ category, onClose, onDeleted }: DeleteCategoryDialogProps) {
  const remove = useDeleteCategory()

  function handleClose() {
    remove.reset()
    onClose()
  }

  async function confirm() {
    if (!category) return
    try {
      await remove.mutateAsync(category.id)
    } catch {
      return
    }
    onDeleted('ลบหมวดหมู่แล้ว')
    handleClose()
  }

  return (
    <Dialog open={category !== null} onClose={handleClose}>
      <DialogTitle>ลบหมวดหมู่?</DialogTitle>
      <DialogContent>
        <DialogContentText>สินค้าในหมวดนี้จะยังอยู่ แต่ไม่มีหมวดหมู่</DialogContentText>
        {remove.error ? (
          <Alert severity="error" variant="outlined" sx={{ mt: 2 }}>
            {describe(remove.error)}
          </Alert>
        ) : null}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>ยกเลิก</Button>
        <Button color="error" variant="contained" onClick={confirm} disabled={remove.isPending}>
          ลบ
        </Button>
      </DialogActions>
    </Dialog>
  )
}
