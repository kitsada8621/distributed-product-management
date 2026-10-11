import { useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { POLL_INTERVAL_MS, POLL_TIMEOUT_MS } from '@/shared/config/env'
import { getOrder } from '../api/ordersApi'
import { needsPolling } from '../model/status'

interface PollingOptions {
  intervalMs?: number
  timeoutMs?: number
}

export function useOrder(id: string, { intervalMs = POLL_INTERVAL_MS, timeoutMs = POLL_TIMEOUT_MS }: PollingOptions = {}) {
  const [timedOutId, setTimedOutId] = useState<string | null>(null)
  const timedOut = timedOutId === id

  useEffect(() => {
    const timer = setTimeout(() => setTimedOutId(id), timeoutMs)
    return () => clearTimeout(timer)
  }, [id, timeoutMs])

  const query = useQuery({
    queryKey: ['order', id],
    queryFn: () => getOrder(id),
    refetchInterval: (current) => {
      const order = current.state.data
      return !timedOut && order && needsPolling(order) ? intervalMs : false
    },
  })

  const pollingGaveUp = timedOut && query.data !== undefined && needsPolling(query.data)
  return { ...query, pollingGaveUp }
}
