export interface Page<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export interface PageMeta {
  page: number
  pageSize: number
  total: number
  totalPages: number
}

export interface ApiEnvelope<T> {
  status: number
  success: boolean
  code: string
  message: string
  data: T
  meta: PageMeta | null
  errors: FieldErrorBody[] | null
  correlationId?: string | null
  timestamp?: string
}

export interface FieldErrorBody {
  field: string
  code: string
  message: string
}

export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly fieldErrors: Record<string, string>
  readonly details: Record<string, unknown> | null
  readonly correlationId: string | null

  constructor(init: {
    status: number
    code: string
    message: string
    fieldErrors?: Record<string, string>
    details?: Record<string, unknown> | null
    correlationId?: string | null
  }) {
    super(init.message)
    this.name = 'ApiError'
    this.status = init.status
    this.code = init.code
    this.fieldErrors = init.fieldErrors ?? {}
    this.details = init.details ?? null
    this.correlationId = init.correlationId ?? null
  }
}
