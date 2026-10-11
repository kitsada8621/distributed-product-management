import { describe, expect, it } from 'vitest'
import { theme } from './theme'

describe('theme', () => {
  it('uses the UI-SPEC palette tokens', () => {
    expect(theme.palette.primary.main).toBe('#0C8276')
    expect(theme.palette.background.hero).toBe('#0B5F57')
    expect(theme.palette.background.surface2).toBe('#EAF3F0')
  })

  it('uses the UI-SPEC shape and typography', () => {
    expect(theme.shape.borderRadius).toBe(12)
    expect(theme.typography.button.textTransform).toBe('none')
    expect(theme.typography.fontFamily).toContain('IBM Plex Sans Thai')
  })
})
