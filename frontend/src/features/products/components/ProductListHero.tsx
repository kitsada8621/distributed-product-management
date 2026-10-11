import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import Box from '@mui/material/Box'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'

const HIGHLIGHTS = ['เห็นสต็อกจริงก่อนสั่ง', 'ติดตามสถานะคำสั่งซื้อได้ทุกขั้น', 'สั่งซ้ำได้ง่าย']

export function ProductListHero() {
  return (
    <Box
      component="section"
      sx={{
        bgcolor: 'background.hero',
        color: '#fff',
        borderRadius: 3,
        p: { xs: 3, md: 5 },
        mb: 3,
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      <Typography variant="h4" component="h2" sx={{ mb: 2 }}>
        สั่งสินค้าได้ง่าย เห็นสต็อกจริง
      </Typography>
      <Stack component="ul" spacing={1} sx={{ listStyle: 'none', m: 0, p: 0 }}>
        {HIGHLIGHTS.map((text) => (
          <Stack component="li" key={text} direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <CheckCircleIcon fontSize="small" />
            <Typography variant="body1">{text}</Typography>
          </Stack>
        ))}
      </Stack>
      <Box
        aria-hidden
        sx={{
          display: { xs: 'none', md: 'block' },
          position: 'absolute',
          right: 40,
          top: '50%',
          transform: 'translateY(-50%)',
          fontSize: 64,
        }}
      >
        📦🛒🌿
      </Box>
    </Box>
  )
}
