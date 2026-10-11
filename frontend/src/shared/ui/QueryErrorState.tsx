import { isServiceUnavailable } from '@/shared/api/errorMapper'
import { ApiError } from '@/shared/api/types'
import { ErrorState } from './ErrorState'
import { ServiceUnavailableBanner } from './ServiceUnavailableBanner'

interface QueryErrorStateProps {
  error: Error
  fallbackMessage: string
  onRetry?: () => void
}

export function QueryErrorState({ error, fallbackMessage, onRetry }: QueryErrorStateProps) {
  if (error instanceof ApiError && isServiceUnavailable(error)) return <ServiceUnavailableBanner />

  const correlationId = error instanceof ApiError ? error.correlationId : null
  return <ErrorState message={error.message || fallbackMessage} correlationId={correlationId} onRetry={onRetry} />
}
