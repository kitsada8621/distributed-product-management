export type Role = 'user' | 'admin'

export interface User {
  id: string
  email: string
  name: string
  role: Role
}
