import Box from '@mui/material/Box'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Skeleton from '@mui/material/Skeleton'

export function ProductGridSkeleton({ count = 8 }: { count?: number }) {
  const cards = []
  for (let i = 0; i < count; i++) {
    cards.push(
      <Card key={i}>
        <Skeleton variant="rectangular" sx={{ aspectRatio: '4 / 3', width: '100%', height: 'auto' }} />
        <CardContent>
          <Skeleton width="70%" />
          <Skeleton width="40%" />
        </CardContent>
      </Card>,
    )
  }
  return (
    <Box
      role="status"
      aria-label="กำลังโหลดสินค้า"
      sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))', gap: 2 }}
    >
      {cards}
    </Box>
  )
}
