import { Routes } from "@angular/router";
import { authGuard } from "./core/guards/auth.guard";

export const routes: Routes = [
  // Default redirect
  {
    path: "",
    redirectTo: "dashboard",
    pathMatch: "full",
  },

  // Auth routes — public (no guard)
  {
    path: "auth",
    loadChildren: () =>
      import("./features/auth/auth.routes").then(
        (m) => m.AUTH_ROUTES
      ),
  },

  // Protected routes — require authentication
  {
    path: "dashboard",
    canActivate: [authGuard],
    loadComponent: () =>
      import("./features/dashboard/dashboard.component").then(
        (m) => m.DashboardComponent
      ),
  },
  {
    path: "documents",
    canActivate: [authGuard],
    loadChildren: () =>
      import("./features/documents/documents.routes").then(
        (m) => m.DOCUMENT_ROUTES
      ),
  },
  {
    path: "workflow",
    canActivate: [authGuard],
    loadChildren: () =>
      import("./features/workflow/workflow.routes").then(
        (m) => m.WORKFLOW_ROUTES
      ),
  },
  {
    path: "notifications",
    canActivate: [authGuard],
    loadComponent: () =>
      import("./features/notifications/notifications.component").then(
        (m) => m.NotificationsComponent
      ),
  },

  // Wildcard redirect
  {
    path: "**",
    redirectTo: "dashboard",
  },
];
