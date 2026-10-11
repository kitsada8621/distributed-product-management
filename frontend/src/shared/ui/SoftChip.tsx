import Chip from '@mui/material/Chip'
import type { ReactElement } from 'react'

export type SoftTone = 'success' | 'warning' | 'neutral' | 'danger' | 'outline'

const TONES: Record<SoftTone, { bg: string; fg: string; border: string }> = {
  success: { bg: '#D3F1E1', fg: '#17724A', border: 'none' },
  warning: { bg: '#FFEFC9', fg: '#8A5200', border: 'none' },
  neutral: { bg: '#E9EFED', fg: '#587270', border: 'none' },
  danger: { bg: '#FDDDD9', fg: '#9E2B22', border: 'none' },
  outline: { bg: 'transparent', fg: '#6F8280', border: '1px solid #C4D2CE' },
}

interface SoftChipProps {
  tone: SoftTone
  label: string
  icon?: ReactElement
  size?: 'small' | 'medium'
}

export function SoftChip({ tone, label, icon, size = 'small' }: SoftChipProps) {
  const { bg, fg, border } = TONES[tone]
  return (
    <Chip
      size={size}
      icon={icon}
      label={label}
      sx={{ bgcolor: bg, color: fg, border, fontVariantNumeric: 'tabular-nums', '& .MuiChip-icon': { color: fg } }}
    />
  )
}
