import { Injectable, signal, computed } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { Observable, tap, catchError, throwError } from "rxjs";
import { jwtDecode } from "jwt-decode";
import { environment } from "../../../environments/environment";
import {
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  RegisterResponse,
  DecodedToken,
  UserInfo,
} from "../models/auth.models";

@Injectable({ providedIn: "root" })
export class AuthService {
  // Angular signals for reactive state management
  private _currentUser = signal<UserInfo | null>(null);
  private _token = signal<string | null>(null);

  // Computed signals � auto-update when _currentUser changes
  currentUser = this._currentUser.asReadonly();
  isLoggedIn  = computed(() => this._currentUser() !== null);
  isAdmin     = computed(() => this._currentUser()?.role === "Admin");
  isManager   = computed(() => this._currentUser()?.role === "Manager"
                            || this._currentUser()?.role === "Admin");

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // Restore session from localStorage on app start
    this.restoreSession();
  }

  // -- Login --------------------------------------------------
  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(
        `${environment.identityUrl}/api/identity/login`,
        request
      )
      .pipe(
        tap((response) => this.handleAuthResponse(response)),
        catchError((error) => {
          console.error("Login failed:", error);
          console.error(error.message)
          return throwError(() => error);
        })
      );
  }

  // -- Register Tenant ----------------------------------------
  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(
      `${environment.identityUrl}/api/identity/register`,
      request
    );
  }

  // -- Refresh Token ------------------------------------------
  refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem("refreshToken");
    const user = this._currentUser();

    if (!refreshToken || !user) {
      this.logout();
      return throwError(() => new Error("No refresh token"));
    }

    return this.http
      .post<AuthResponse>(
        `${environment.identityUrl}/api/identity/refresh`,
        {
          refreshToken,
          userId: user.userId,
        }
      )
      .pipe(
        tap((response) => this.handleAuthResponse(response)),
        catchError((error) => {
          this.logout();
          return throwError(() => error);
        })
      );
  }

  // -- Logout -------------------------------------------------
  logout(): void {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("currentUser");
    this._currentUser.set(null);
    this._token.set(null);
    this.router.navigate(["/auth/login"]);
  }

  // -- Get Current Token --------------------------------------
  getToken(): string | null {
    return this._token() ?? localStorage.getItem("accessToken");
  }

  // -- Check Token Expiry -------------------------------------
  isTokenExpired(): boolean {
    const token = this.getToken();
    if (!token) return true;

    try {
      const decoded = jwtDecode<DecodedToken>(token);
      const expiry  = decoded.exp * 1000;
      return Date.now() > expiry - 60000; // 60s buffer
    } catch {
      return true;
    }
  }

  // -- Private Helpers ----------------------------------------
  private handleAuthResponse(response: AuthResponse): void {
    localStorage.setItem("accessToken",  response.accessToken);
    localStorage.setItem("refreshToken", response.refreshToken);
    localStorage.setItem("currentUser",
      JSON.stringify(response.user));

    this._token.set(response.accessToken);
    this._currentUser.set(response.user);
  }

  private restoreSession(): void {
    const token = localStorage.getItem("accessToken");
    const user  = localStorage.getItem("currentUser");

    if (token && user) {
      try {
        const decoded = jwtDecode<DecodedToken>(token);
        const isExpired = Date.now() > decoded.exp * 1000;

        if (!isExpired) {
          this._token.set(token);
          this._currentUser.set(JSON.parse(user));
        } else {
          this.logout();
        }
      } catch {
        this.logout();
      }
    }
  }
}
