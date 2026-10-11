import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import { Controller, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form'
import { FormField } from '@/shared/ui'
import { PAYMENT_METHOD_OPTIONS } from '../model/paymentMethod'
import type { CheckoutFormInput } from '../schemas/checkoutSchema'

interface CheckoutFieldsProps {
  register: UseFormRegister<CheckoutFormInput>
  control: Control<CheckoutFormInput>
  errors: FieldErrors<CheckoutFormInput>
}

export function CheckoutFields({ register, control, errors }: CheckoutFieldsProps) {
  const addressErrors = errors.shippingAddress

  return (
    <Stack spacing={1}>
      <FormField
        label="ชื่อผู้รับ"
        autoComplete="name"
        error={addressErrors?.recipientName?.message}
        {...register('shippingAddress.recipientName')}
      />
      <FormField
        label="เบอร์โทรศัพท์"
        type="tel"
        autoComplete="tel"
        error={addressErrors?.phone?.message}
        {...register('shippingAddress.phone')}
      />
      <FormField
        label="ที่อยู่ (บ้านเลขที่ ซอย ถนน แขวง/ตำบล เขต/อำเภอ)"
        autoComplete="street-address"
        multiline
        minRows={2}
        error={addressErrors?.addressLine?.message}
        {...register('shippingAddress.addressLine')}
      />
      <FormField
        label="จังหวัด"
        autoComplete="address-level1"
        error={addressErrors?.province?.message}
        {...register('shippingAddress.province')}
      />
      <FormField
        label="รหัสไปรษณีย์"
        autoComplete="postal-code"
        error={addressErrors?.postalCode?.message}
        {...register('shippingAddress.postalCode')}
      />
      <Controller
        name="paymentMethod"
        control={control}
        defaultValue={'' as CheckoutFormInput['paymentMethod']}
        render={({ field }) => (
          <FormField select label="วิธีชำระเงิน" error={errors.paymentMethod?.message} {...field}>
            {PAYMENT_METHOD_OPTIONS.map((option) => (
              <MenuItem key={option.value} value={option.value}>
                {option.label}
              </MenuItem>
            ))}
          </FormField>
        )}
      />
    </Stack>
  )
}
