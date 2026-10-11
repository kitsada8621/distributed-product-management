import MuiPagination from '@mui/material/Pagination'
import Stack from '@mui/material/Stack'

interface PaginationProps {
  page: number
  pageSize: number
  total: number
  onChange: (page: number) => void
}

export function Pagination({ page, pageSize, total, onChange }: PaginationProps) {
  const pageCount = Math.ceil(total / pageSize)
  if (pageCount <= 1) return null

  return (
    <Stack sx={{ alignItems: 'center', mt: 3 }}>
      <MuiPagination count={pageCount} page={page} onChange={(_, next) => onChange(next)} />
    </Stack>
  )
}
