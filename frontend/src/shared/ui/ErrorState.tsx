import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Typography from '@mui/material/Typography'
import { CorrelationId } from './CorrelationId'

interface ErrorStateProps {
  message: string
  correlationId?: string | null
  onRetry?: () => void
}

export function ErrorState({ message, correlationId, onRetry }: ErrorStateProps) {
  const retryButton = onRetry ? (
    <Button color="inherit" size="small" onClick={onRetry}>
      ลองใหม่
    </Button>
  ) : undefined

  return (
    <Alert severity="error" variant="outlined" action={retryButton}>
      <Typography variant="body2">{message}</Typography>
      {correlationId ? <CorrelationId id={correlationId} /> : null}
    </Alert>
  )
}
