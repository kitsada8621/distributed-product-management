import MenuItem from '@mui/material/MenuItem'
import TextField from '@mui/material/TextField'
import { useCategories } from '@/features/categories'

interface CategoryFilterProps {
  value: string
  onChange: (categoryId: string) => void
}

export function CategoryFilter({ value, onChange }: CategoryFilterProps) {
  const { data: categories = [] } = useCategories()

  return (
    <TextField
      select
      label="หมวดหมู่"
      size="small"
      value={value}
      onChange={(event) => onChange(event.target.value)}
      sx={{ minWidth: 200 }}
    >
      <MenuItem value="">ทุกหมวดหมู่</MenuItem>
      {categories.map((category) => (
        <MenuItem key={category.id} value={category.id}>
          {category.name}
        </MenuItem>
      ))}
    </TextField>
  )
}
