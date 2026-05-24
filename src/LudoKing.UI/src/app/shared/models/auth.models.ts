export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface AuthResult {
  accessToken: string;
  expiresAt: string;
  userId: string;
  displayName: string;
  role: string;
}
