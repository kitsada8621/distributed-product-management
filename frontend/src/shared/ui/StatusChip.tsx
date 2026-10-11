import Chip from '@mui/material/Chip'

type StatusColor = 'default' | 'success' | 'warning' | 'error' | 'info'

export function StatusChip({ label, color = 'default' }: { label: string; color?: StatusColor }) {
  return <Chip label={label} color={color} size="small" />
}
