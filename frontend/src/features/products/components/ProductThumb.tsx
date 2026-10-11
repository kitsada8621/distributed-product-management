import Box from '@mui/material/Box'

const THUMBS = [
  { bg: '#F3E4D2', emoji: '📦' },
  { bg: '#F6EDC9', emoji: '🛒' },
  { bg: '#DDEFD3', emoji: '🌿' },
  { bg: '#FBDDD0', emoji: '🎁' },
]

function thumbIndex(categoryId: string | null) {
  if (!categoryId) return 0
  let sum = 0
  for (const char of categoryId) {
    sum += char.charCodeAt(0)
  }
  return sum % THUMBS.length
}

export function ProductThumb({ categoryId }: { categoryId: string | null }) {
  const thumb = THUMBS[thumbIndex(categoryId)]
  return (
    <Box sx={{ aspectRatio: '4 / 3', bgcolor: thumb.bg, display: 'grid', placeItems: 'center', fontSize: 48 }}>
      <span aria-hidden>{thumb.emoji}</span>
    </Box>
  )
}
