import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpErrorResponse,
} from "@angular/common/http";
import { inject } from "@angular/core";
import { catchError, switchMap, throwError } from "rxjs";
import { AuthService } from "../services/auth.service";
import { Router } from "@angular/router";

// JWT Interceptor — automatically attaches Bearer token
// to every outgoing HTTP request.
//
// On 401 response — attempts token refresh once.
// If refresh fails — redirects to login.
//
// This is the Angular equivalent of Axios interceptors
// or .NET DelegatingHandler.
export const jwtInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  // Skip auth header for identity endpoints
  const isAuthEndpoint =
    req.url.includes("/api/identity/login") ||
    req.url.includes("/api/identity/register") ||
    req.url.includes("/api/identity/refresh");

  if (isAuthEndpoint) {
    return next(req);
  }

  // Attach Bearer token
  const token = authService.getToken();
  const authReq = token
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // On 401 — try refresh then retry original request
      if (error.status === 401 && !isAuthEndpoint) {
        return authService.refreshToken().pipe(
          switchMap((response) => {
            const retryReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${response.accessToken}`,
              },
            });
            return next(retryReq);
          }),
          catchError((refreshError) => {
            authService.logout();
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
