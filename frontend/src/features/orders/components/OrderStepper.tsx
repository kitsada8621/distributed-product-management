import Step from '@mui/material/Step'
import StepLabel from '@mui/material/StepLabel'
import Stepper from '@mui/material/Stepper'
import type { OrderStatus, StockStatus } from '../types'

interface OrderStepperProps {
  status: OrderStatus
  stockStatus: StockStatus | null
}

const STEPS = ['สร้างคำสั่งซื้อ', 'ตรวจสต็อก', 'รอยืนยัน', 'เสร็จสิ้น']

function activeStepOf(status: OrderStatus, stockStatus: StockStatus | null): number {
  if (status === 'Confirmed') return 4
  if (status === 'Rejected' || status === 'Cancelled') return 3
  if (stockStatus === 'Reserved') return 2
  return 1
}

export function OrderStepper({ status, stockStatus }: OrderStepperProps) {
  const lastIndex = STEPS.length - 1

  return (
    <Stepper activeStep={activeStepOf(status, stockStatus)} alternativeLabel>
      {STEPS.map((label, index) => {
        if (index === lastIndex && status === 'Rejected') {
          return (
            <Step key={label}>
              <StepLabel error>ถูกปฏิเสธ</StepLabel>
            </Step>
          )
        }
        if (index === lastIndex && status === 'Cancelled') {
          return (
            <Step key={label}>
              <StepLabel sx={{ color: 'text.disabled' }}>ยกเลิกแล้ว</StepLabel>
            </Step>
          )
        }
        return (
          <Step key={label}>
            <StepLabel>{label}</StepLabel>
          </Step>
        )
      })}
    </Stepper>
  )
}
