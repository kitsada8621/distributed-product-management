export interface PasswordContext {
  email?: string
  name?: string
}

const MIN_PASSWORD_LENGTH = 8
const MAX_PASSWORD_LENGTH = 64
const MIN_CONTEXT_WORD_LENGTH = 3

const TOO_SHORT_MESSAGE = 'รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร'
const TOO_LONG_MESSAGE = 'รหัสผ่านต้องไม่เกิน 64 ตัวอักษร'
const COMMON_MESSAGE = 'รหัสผ่านนี้พบได้บ่อยหรือเคยรั่วไหล กรุณาใช้รหัสผ่านอื่น'
const PATTERN_MESSAGE = 'รหัสผ่านเป็นรูปแบบซ้ำหรือเรียงต่อกันที่เดาง่าย'
const COMPOSITION_MESSAGE = 'รหัสผ่านต้องมีตัวพิมพ์ใหญ่ ตัวพิมพ์เล็ก ตัวเลข และสัญลักษณ์อย่างละ 1 ตัวขึ้นไป'
const CONTEXT_MESSAGE = 'รหัสผ่านต้องไม่มีอีเมลหรือชื่อของคุณ'

const COMMON_PASSWORDS = [
  'passw0rd!',
  'password1!',
  'password123!',
  'password1234!',
  'p@ssw0rd',
  'p@ssw0rd1',
  'p@ssword1',
  'p@ssw0rd123',
  'admin123!',
  'admin@123',
  'admin1234!',
  'administrator1!',
  'welcome1!',
  'welcome123!',
  'welcome@123',
  'qwerty123!',
  'qwerty@123',
  'qwerty1234!',
  'qazwsx123!',
  'iloveyou1!',
  'iloveyou123!',
  'letmein123!',
  'letmein1!',
  'monkey123!',
  'dragon123!',
  'football1!',
  'baseball1!',
  'sunshine1!',
  'princess1!',
  'master123!',
  'login123!',
  'abc12345!',
  'abcd1234!',
  'changeme1!',
  'changeme123!',
  'trustno1!',
  'superman1!',
  'hello123!',
  'test1234!',
  'user1234!',
]

const UPPERCASE = /\p{Lu}/u
const LOWERCASE = /\p{Ll}/u
const DIGIT = /\p{Nd}/u
const SYMBOL = /[^\p{L}\p{N}\s]/u

function isRepeatedUnit(characters: string[]): boolean {
  const total = characters.length
  for (let unitLength = 1; unitLength <= total / 2; unitLength++) {
    if (total % unitLength !== 0) continue
    let repeats = true
    for (let index = unitLength; index < total; index++) {
      if (characters[index] !== characters[index % unitLength]) {
        repeats = false
        break
      }
    }
    if (repeats) return true
  }
  return false
}

function isSequentialRun(characters: string[]): boolean {
  const codes = characters.map((character) => character.codePointAt(0) ?? 0)
  const step = codes[1] - codes[0]
  if (step !== 1 && step !== -1) return false
  for (let index = 2; index < codes.length; index++) {
    if (codes[index] - codes[index - 1] !== step) return false
  }
  return true
}

function hasAllCharacterClasses(password: string): boolean {
  return UPPERCASE.test(password) && LOWERCASE.test(password) && DIGIT.test(password) && SYMBOL.test(password)
}

function contextWords(context: PasswordContext): string[] {
  const words: string[] = []
  const localPart = (context.email ?? '').split('@')[0].toLowerCase()
  if (localPart.length >= MIN_CONTEXT_WORD_LENGTH) words.push(localPart)
  for (const word of (context.name ?? '').toLowerCase().split(/\s+/)) {
    if (word.length >= MIN_CONTEXT_WORD_LENGTH) words.push(word)
  }
  return words
}

function containsContextWord(lowercased: string, context: PasswordContext): boolean {
  for (const word of contextWords(context)) {
    if (lowercased.includes(word)) return true
  }
  return false
}

export function findPasswordIssue(password: string, context: PasswordContext = {}): string | null {
  const normalized = password.normalize('NFKC')
  const characters = [...normalized]
  const lowercased = normalized.toLowerCase()

  if (characters.length < MIN_PASSWORD_LENGTH) return TOO_SHORT_MESSAGE
  if (characters.length > MAX_PASSWORD_LENGTH) return TOO_LONG_MESSAGE
  if (COMMON_PASSWORDS.includes(lowercased)) return COMMON_MESSAGE
  if (isRepeatedUnit(characters) || isSequentialRun(characters)) return PATTERN_MESSAGE
  if (!hasAllCharacterClasses(normalized)) return COMPOSITION_MESSAGE
  if (containsContextWord(lowercased, context)) return CONTEXT_MESSAGE
  return null
}
