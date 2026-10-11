import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'
import { useCategories } from '@/features/categories'

interface CategoryPillsProps {
  value: string
  onChange: (categoryId: string) => void
}

export function CategoryPills({ value, onChange }: CategoryPillsProps) {
  const { data: categories = [] } = useCategories()

  return (
    <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1 }}>
      <Chip
        label="ทั้งหมด"
        clickable
        onClick={() => onChange('')}
        color={value === '' ? 'primary' : 'default'}
        variant={value === '' ? 'filled' : 'outlined'}
        aria-pressed={value === ''}
      />
      {categories.map((category) => {
        const active = value === category.id
        return (
          <Chip
            key={category.id}
            label={category.name}
            clickable
            onClick={() => onChange(category.id)}
            color={active ? 'primary' : 'default'}
            variant={active ? 'filled' : 'outlined'}
            aria-pressed={active}
          />
        )
      })}
    </Stack>
  )
}
