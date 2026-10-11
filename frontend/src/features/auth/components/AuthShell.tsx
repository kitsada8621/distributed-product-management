import Grid from '@mui/material/Grid'
import Paper from '@mui/material/Paper'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'

interface AuthShellProps {
  title: string
  children: ReactNode
}

export function AuthShell({ title, children }: AuthShellProps) {
  return (
    <Grid container sx={{ minHeight: 'calc(100vh - 64px)' }}>
      <Grid
        size={{ md: 5 }}
        sx={{
          display: { xs: 'none', md: 'flex' },
          flexDirection: 'column',
          justifyContent: 'center',
          bgcolor: 'background.hero',
          color: '#fff',
          p: 6,
        }}
      >
        <Typography variant="h4" component="p" sx={{ fontWeight: 600, mb: 2 }}>
          จัดการสินค้าและคำสั่งซื้อในที่เดียว
        </Typography>
        <Typography>เห็นสต็อกจริง ติดตามทุกคำสั่งซื้อ</Typography>
      </Grid>
      <Grid size={{ xs: 12, md: 7 }} sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', p: 2 }}>
        <Paper sx={{ p: { xs: 3, sm: 4 }, width: '100%', maxWidth: 440 }}>
          <Typography variant="h4" component="h1" sx={{ mb: 2 }}>
            {title}
          </Typography>
          {children}
        </Paper>
      </Grid>
    </Grid>
  )
}
