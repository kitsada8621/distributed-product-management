import TextField from '@mui/material/TextField'

interface ProductSearchBarProps {
  value: string
  onChange: (value: string) => void
}

export function ProductSearchBar({ value, onChange }: ProductSearchBarProps) {
  return (
    <TextField
      label="ค้นหาสินค้า"
      size="small"
      value={value}
      onChange={(event) => onChange(event.target.value)}
      sx={{ minWidth: 240 }}
    />
  )
}
