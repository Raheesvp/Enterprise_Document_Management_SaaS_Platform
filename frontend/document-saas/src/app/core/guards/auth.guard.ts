import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/auth.service";

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  // Check signal — true if _currentUser is not null
  if (authService.isLoggedIn()) {
    return true;
  }

  // Also check localStorage directly as fallback
  const token = localStorage.getItem("accessToken");
  const user  = localStorage.getItem("currentUser");

  if (token && user) {
    return true; // session will be restored by AuthService constructor
  }

  router.navigate(["/auth/login"]);
  return false;
};

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  if (authService.isAdmin()) return true;

  router.navigate(["/dashboard"]);
  return false;
};
