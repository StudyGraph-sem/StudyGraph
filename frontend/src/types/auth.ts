export interface User {
  id: string
  email: string
  displayName: string
  role: 'User' | 'Admin'
  createdAt: string
}
export interface AuthResponse {
  accessToken: string
  expiresAt: string
  user: User
}
export interface LoginRequest {
  email: string
  password: string
}
export interface RegisterRequest extends LoginRequest {
  displayName: string
}
