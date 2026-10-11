import { FormField } from '@/shared/ui'

interface StockEditorProps {
  value: string
  onChange: (value: string) => void
  error?: string
}

export function StockEditor({ value, onChange, error }: StockEditorProps) {
  return (
    <FormField
      label="จำนวนสต็อกใหม่ (แทนที่ค่าปัจจุบัน)"
      type="number"
      value={value}
      onChange={(event) => onChange(event.target.value)}
      error={error}
      helperText="กำหนดสต็อกเป็นจำนวนนี้พอดี"
      slotProps={{ htmlInput: { min: 0, step: 1 } }}
    />
  )
}
