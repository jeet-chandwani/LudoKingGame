export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  displayName: string;
}

export interface LoginRequest {
  identifier: string;
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
  username: string;
  displayName: string;
  role: string;
}
