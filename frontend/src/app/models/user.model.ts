export interface User {
  id: string;
  username: string;
  email: string;
  createdAt: string;
}

export interface AuthTokenResponse {
  token: string;
  expiresAt: string;
  userId: string;
  username: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}
