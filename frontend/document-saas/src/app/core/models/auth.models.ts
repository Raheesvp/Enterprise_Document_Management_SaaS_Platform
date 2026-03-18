// Models matching the Identity Service API responses exactly

export interface LoginRequest {
  email: string;
  password: string;
  subdomain: string;
}

export interface RegisterRequest {
  tenantName: string;
  subdomain: string;
  contactEmail: string;
  adminFullName: string;
  adminEmail: string;
  adminPassword: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;
  user: UserInfo;
}

export interface UserInfo {
  userId: string;
  tenantId: string;
  email: string;
  fullName: string;
  role: string;
}

export interface RegisterResponse {
  tenantId: string;
  adminUserId: string;
  message: string;
}

export interface DecodedToken {
  sub: string;
  email: string;
  tenant_id: string;
  tenant_name: string;
  tenant_subdomain: string;
  role: string;
  full_name: string;
  exp: number;
  iss: string;
  aud: string;
}
