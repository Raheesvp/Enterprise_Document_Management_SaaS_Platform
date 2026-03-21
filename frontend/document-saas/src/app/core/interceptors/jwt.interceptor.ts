import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpErrorResponse,
} from "@angular/common/http";
import { inject } from "@angular/core";
import { catchError, switchMap, throwError, BehaviorSubject, filter, take } from "rxjs";
import { AuthService } from "../services/auth.service";

let isRefreshing = false;
let refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const jwtInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const authService = inject(AuthService);

  const isAuthEndpoint =
    req.url.includes("/api/identity/login") ||
    req.url.includes("/api/identity/register") ||
    req.url.includes("/api/identity/refresh");

  if (isAuthEndpoint) {
    return next(req);
  }

  const token = localStorage.getItem("accessToken");

  const authReq = token
    ? req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
      })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      if (isRefreshing) {
        return refreshTokenSubject.pipe(
          filter((t) => t !== null),
          take(1),
          switchMap((newToken) => {
            return next(req.clone({
              setHeaders: { Authorization: `Bearer ${newToken}` },
            }));
          })
        );
      }

      isRefreshing = true;
      refreshTokenSubject.next(null);

      return authService.refreshToken().pipe(
        switchMap((response) => {
          isRefreshing = false;
          refreshTokenSubject.next(response.accessToken);
          return next(req.clone({
            setHeaders: {
              Authorization: `Bearer ${response.accessToken}`,
            },
          }));
        }),
        catchError((refreshError) => {
          isRefreshing = false;
          authService.logout();
          return throwError(() => refreshError);
        })
      );
    })
  );
};
