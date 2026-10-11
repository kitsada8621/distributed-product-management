import { authHandlers } from './auth'
import { catalogHandlers } from './catalog'
import { orderHandlers } from './orders'

export const handlers = [...authHandlers, ...catalogHandlers, ...orderHandlers]
