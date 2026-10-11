import AddIcon from '@mui/icons-material/Add'
import RemoveIcon from '@mui/icons-material/Remove'
import IconButton from '@mui/material/IconButton'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { isValidQuantity } from '../model/quantity'

interface QuantityStepperProps {
  value: number
  itemName: string
  onChange: (quantity: number) => void
}

export function QuantityStepper({ value, itemName, onChange }: QuantityStepperProps) {
  const valid = isValidQuantity(value)

  return (
    <Stack direction="row" spacing={0.5} sx={{ alignItems: 'flex-start', justifyContent: 'flex-end' }}>
      <IconButton aria-label={`ลดจำนวน ${itemName}`} disabled={!valid || value <= 1} onClick={() => onChange(value - 1)}>
        <RemoveIcon />
      </IconButton>
      <TextField
        size="small"
        type="number"
        value={String(value)}
        onChange={(event) => onChange(Number(event.target.value))}
        error={!valid}
        helperText={valid ? undefined : 'กรอกจำนวนเต็มที่มากกว่า 0'}
        slotProps={{ htmlInput: { min: 1, step: 1, 'aria-label': `จำนวน ${itemName}` } }}
        sx={{ width: 88 }}
      />
      <IconButton aria-label={`เพิ่มจำนวน ${itemName}`} onClick={() => onChange(valid ? value + 1 : 1)}>
        <AddIcon />
      </IconButton>
    </Stack>
  )
}
