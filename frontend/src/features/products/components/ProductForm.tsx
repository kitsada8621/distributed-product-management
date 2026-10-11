import { zodResolver } from '@hookform/resolvers/zod'
import Button from '@mui/material/Button'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import CardHeader from '@mui/material/CardHeader'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import type { ReactNode } from 'react'
import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useCategories } from '@/features/categories'
import { StockEditor } from '@/features/stock'
import { FormField } from '@/shared/ui'
import { productSchema, type ProductFormValues } from '../schemas/productSchema'

interface ProductFormProps {
  defaultValues: ProductFormValues
  submitting: boolean
  fieldErrors: Record<string, string>
  results?: ReactNode
  onSubmit: (values: ProductFormValues) => void
  onCancel: () => void
}

export function ProductForm({ defaultValues, submitting, fieldErrors, results, onSubmit, onCancel }: ProductFormProps) {
  const { data: categories = [] } = useCategories()
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ProductFormValues>({ resolver: zodResolver(productSchema), defaultValues })

  useEffect(() => {
    for (const [field, message] of Object.entries(fieldErrors)) {
      if (field in defaultValues) setError(field as keyof ProductFormValues, { message })
    }
  }, [fieldErrors, defaultValues, setError])

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <Stack spacing={3}>
        {results}
        <Card>
          <CardHeader title="ข้อมูลสินค้า" />
          <CardContent>
            <FormField label="ชื่อสินค้า" error={errors.name?.message} {...register('name')} />
            <FormField
              label="รายละเอียด"
              multiline
              minRows={3}
              error={errors.description?.message}
              {...register('description')}
            />
            <FormField
              label="ราคา"
              type="number"
              slotProps={{ htmlInput: { min: 0, step: '0.01' } }}
              error={errors.price?.message}
              {...register('price')}
            />
            <Controller
              name="categoryId"
              control={control}
              render={({ field }) => (
                <FormField select label="หมวดหมู่" error={errors.categoryId?.message} {...field}>
                  {categories.map((category) => (
                    <MenuItem key={category.id} value={category.id}>
                      {category.name}
                    </MenuItem>
                  ))}
                </FormField>
              )}
            />
          </CardContent>
        </Card>
        <Card>
          <CardHeader title="สต็อก" />
          <CardContent>
            <Controller
              name="stock"
              control={control}
              render={({ field }) => (
                <StockEditor value={field.value} onChange={field.onChange} error={errors.stock?.message} />
              )}
            />
          </CardContent>
        </Card>
        <Stack direction="row" spacing={2}>
          <Button type="submit" variant="contained" disabled={submitting}>
            บันทึก
          </Button>
          <Button onClick={onCancel}>ยกเลิก</Button>
        </Stack>
      </Stack>
    </form>
  )
}
