import Container from '@mui/material/Container'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'

interface PageContainerProps {
  title: string
  description?: string
  actions?: ReactNode
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl'
  children: ReactNode
}

export function PageContainer({ title, description, actions, maxWidth = 'lg', children }: PageContainerProps) {
  return (
    <Container maxWidth={maxWidth} sx={{ py: 4 }}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 2, mb: 3 }}
      >
        <div>
          <Typography variant="h4" component="h1">
            {title}
          </Typography>
          {description ? <Typography color="text.secondary">{description}</Typography> : null}
        </div>
        {actions}
      </Stack>
      {children}
    </Container>
  )
}
