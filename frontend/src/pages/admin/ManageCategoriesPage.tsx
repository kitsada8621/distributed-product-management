import AddIcon from '@mui/icons-material/Add'
import Button from '@mui/material/Button'
import { useState } from 'react'
import {
  CategoryFormDialog,
  CategoryTable,
  DeleteCategoryDialog,
  useCategories,
  type Category,
} from '@/features/categories'
import { useNotify } from '@/shared/hooks'
import { EmptyState, PageContainer, QueryErrorState, TableSkeleton } from '@/shared/ui'

export function ManageCategoriesPage() {
  const notify = useNotify()
  const { data: categories, error, isPending, refetch } = useCategories()
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Category | null>(null)
  const [deleting, setDeleting] = useState<Category | null>(null)

  function openCreate() {
    setEditing(null)
    setFormOpen(true)
  }

  function openEdit(category: Category) {
    setEditing(category)
    setFormOpen(true)
  }

  return (
    <PageContainer
      title="จัดการหมวดหมู่"
      actions={
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
          สร้างหมวดหมู่
        </Button>
      }
    >
      {isPending ? <TableSkeleton /> : null}
      {error ? <QueryErrorState error={error} fallbackMessage="โหลดหมวดหมู่ไม่สำเร็จ" onRetry={refetch} /> : null}
      {categories && categories.length === 0 ? <EmptyState title="ยังไม่มีหมวดหมู่" /> : null}
      {categories && categories.length > 0 ? (
        <CategoryTable categories={categories} onEdit={openEdit} onDelete={setDeleting} />
      ) : null}
      <CategoryFormDialog open={formOpen} category={editing} onClose={() => setFormOpen(false)} onSaved={notify} />
      <DeleteCategoryDialog category={deleting} onClose={() => setDeleting(null)} onDeleted={notify} />
    </PageContainer>
  )
}
