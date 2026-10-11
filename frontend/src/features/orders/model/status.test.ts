import { describe, expect, it } from 'vitest'
import { canCancel, canConfirmOrReject, isTerminal, needsPolling } from './status'
import type { OrderStatus, StockStatus } from '../types'

type Case = [OrderStatus, StockStatus | null, { terminal: boolean; cancel: boolean; poll: boolean; decide: boolean }]

const cases: Case[] = [
  ['Pending', 'Reserving', { terminal: false, cancel: true, poll: true, decide: false }],
  ['Pending', 'Reserved', { terminal: false, cancel: true, poll: false, decide: true }],
  ['Confirmed', null, { terminal: true, cancel: false, poll: false, decide: false }],
  ['Rejected', null, { terminal: true, cancel: false, poll: false, decide: false }],
  ['Cancelled', null, { terminal: true, cancel: false, poll: false, decide: false }],
]

describe.each(cases)('%s / %s', (status, stockStatus, expected) => {
  const order = { status, stockStatus }

  it('derives the allowed actions', () => {
    expect(isTerminal(order)).toBe(expected.terminal)
    expect(canCancel(order)).toBe(expected.cancel)
    expect(needsPolling(order)).toBe(expected.poll)
    expect(canConfirmOrReject(order)).toBe(expected.decide)
  })
})
