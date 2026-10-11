// @vitest-environment node
import { ESLint } from 'eslint'
import { expect, it } from 'vitest'

const eslint = new ESLint()

async function lintMessages(filePath: string, code: string) {
  const [result] = await eslint.lintText(code, { filePath })
  return result.messages.map((m) => m.message).join(' | ')
}

it('blocks shared importing features', async () => {
  const messages = await lintMessages(
    'src/shared/lib/x.ts',
    "import { a } from '@/features/auth'\nexport const b = a\n",
  )
  expect(messages).toContain('shared must not import features, pages or app.')
})

it('blocks a feature deep-importing another feature', async () => {
  const messages = await lintMessages(
    'src/features/orders/x.ts',
    "import { a } from '@/features/products/api/productApi'\nexport const b = a\n",
  )
  expect(messages).toContain('Import another feature only through its index.ts.')
})

it('blocks a feature importing pages', async () => {
  const messages = await lintMessages(
    'src/features/orders/x.ts',
    "import { a } from '@/pages/Home'\nexport const b = a\n",
  )
  expect(messages).toContain('features must not import pages or app.')
})

it('blocks pages calling an api module directly', async () => {
  const messages = await lintMessages(
    'src/pages/X.tsx',
    "import { a } from '@/features/products/api/productApi'\nexport const b = a\n",
  )
  expect(messages).toContain('pages must not call APIs directly; use feature hooks.')
})

it('allows a feature index import from a page', async () => {
  const messages = await lintMessages(
    'src/pages/X.tsx',
    "import { a } from '@/features/products'\nexport const b = a\n",
  )
  expect(messages).toBe('')
})
