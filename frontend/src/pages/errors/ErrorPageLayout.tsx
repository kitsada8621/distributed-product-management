import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Typography from '@mui/material/Typography'
import { Link as RouterLink, useLocation } from 'react-router'
import { CorrelationId, PageContainer } from '@/shared/ui'

interface ErrorPageLayoutProps {
  code: string
  title: string
  message: string
  actionLabel: string
  actionTo: string
}

export function ErrorPageLayout({ code, title, message, actionLabel, actionTo }: ErrorPageLayoutProps) {
  const location = useLocation()
  const correlationId = (location.state as { correlationId?: string } | null)?.correlationId

  return (
    <PageContainer title={title} maxWidth="sm">
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography aria-hidden sx={{ fontSize: 96, fontWeight: 700, lineHeight: 1, color: 'primary.light' }}>
          {code}
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 3 }}>
          {message}
        </Typography>
        {correlationId ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', mb: 3 }}>
            <CorrelationId id={correlationId} />
          </Box>
        ) : null}
        <Button component={RouterLink} to={actionTo} variant="contained">
          {actionLabel}
        </Button>
      </Box>
    </PageContainer>
  )
}
