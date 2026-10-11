import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { FormField } from '@/shared/ui'
import { useCreateCategory, useUpdateCategory } from '../hooks/useCategoryMutations'
import type { Category } from '../types'

const categorySchema = z.object({ name: z.string().trim().min(1, 'Name is required') })
type CategoryInput = z.infer<typeof categorySchema>

interface CategoryFormDialogProps {
  open: boolean
  category: Category | null
  onClose: () => void
  onSaved: (message: string) => void
}

export function CategoryFormDialog({ open, category, onClose, onSaved }: CategoryFormDialogProps) {
  const create = useCreateCategory()
  const update = useUpdateCategory()
  const mutation = category ? update : create
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CategoryInput>({ resolver: zodResolver(categorySchema) })

  useEffect(() => {
    if (open) reset({ name: category?.name ?? '' })
    create.reset()
    update.reset()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, category])

  async function submit(values: CategoryInput) {
    if (category) await update.mutateAsync({ id: category.id, name: values.name })
    else await create.mutateAsync(values.name)
    onSaved(category ? 'แก้ไขหมวดหมู่แล้ว' : 'สร้างหมวดหมู่แล้ว')
    onClose()
  }

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <form onSubmit={handleSubmit((values) => submit(values).catch(() => {}))} noValidate>
        <DialogTitle>{category ? 'แก้ไขหมวดหมู่' : 'สร้างหมวดหมู่'}</DialogTitle>
        <DialogContent>
          {mutation.error ? <Alert severity="error" variant="outlined">
              {mutation.error.message}
            </Alert> : null}
          <FormField label="ชื่อหมวดหมู่" autoFocus error={errors.name?.message} {...register('name')} />
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>ยกเลิก</Button>
          <Button type="submit" variant="contained" disabled={mutation.isPending}>
            บันทึก
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
