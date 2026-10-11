import Alert from '@mui/material/Alert'
import Snackbar from '@mui/material/Snackbar'
import { useCallback, useState, type ReactNode } from 'react'
import { NotifyContext, type NotifySeverity } from './notifyContext'

interface Message {
  text: string
  severity: NotifySeverity
}

export function NotifyProvider({ children }: { children: ReactNode }) {
  const [message, setMessage] = useState<Message | null>(null)

  const notify = useCallback((text: string, severity: NotifySeverity = 'success') => {
    setMessage({ text, severity })
  }, [])

  return (
    <NotifyContext.Provider value={notify}>
      {children}
      <Snackbar
        open={message !== null}
        autoHideDuration={4000}
        onClose={() => setMessage(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity={message?.severity ?? 'success'} onClose={() => setMessage(null)} variant="filled">
          {message?.text}
        </Alert>
      </Snackbar>
    </NotifyContext.Provider>
  )
}
