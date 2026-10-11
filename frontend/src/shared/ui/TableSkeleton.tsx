import Paper from '@mui/material/Paper'
import Skeleton from '@mui/material/Skeleton'
import Stack from '@mui/material/Stack'

export function TableSkeleton({ rows = 5 }: { rows?: number }) {
  const lines = []
  for (let index = 0; index < rows; index++) {
    lines.push(<Skeleton key={index} variant="rectangular" height={40} />)
  }

  return (
    <Paper role="status" aria-label="กำลังโหลด" sx={{ p: 2 }}>
      <Stack spacing={1}>{lines}</Stack>
    </Paper>
  )
}
