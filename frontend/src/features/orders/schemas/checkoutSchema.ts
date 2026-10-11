import { z } from 'zod'

const required = 'กรุณากรอกข้อมูล'

function text(maxLength: number) {
  return z.string().trim().min(1, required).max(maxLength, `ยาวเกิน ${maxLength} ตัวอักษร`)
}

function pattern(regex: RegExp, message: string) {
  return z.string().trim().min(1, required).pipe(z.string().regex(regex, message))
}

export const checkoutSchema = z.object({
  shippingAddress: z.object({
    recipientName: text(100),
    phone: pattern(/^0\d{8,9}$/, 'เบอร์โทรต้องขึ้นต้นด้วย 0 และมี 9-10 หลัก'),
    addressLine: text(255),
    province: text(100),
    postalCode: pattern(/^\d{5}$/, 'รหัสไปรษณีย์ต้องเป็นตัวเลข 5 หลัก'),
  }),
  paymentMethod: z.enum(['CASH_ON_DELIVERY', 'PAY_AT_STORE'], { error: 'เลือกวิธีชำระเงิน' }),
})

export type CheckoutFormInput = z.infer<typeof checkoutSchema>
