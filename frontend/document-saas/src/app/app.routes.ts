import { Routes } from "@angular/router";
import { authGuard } from "./core/guards/auth.guard";

export const routes: Routes = [
  {
    path: "",
    redirectTo: "dashboard",
    pathMatch: "full",
  },

  // Public auth routes — NO shell
  {
    path: "auth",
    loadChildren: () =>
      import("./features/auth/auth.routes").then((m) => m.AUTH_ROUTES),
  },

  // All protected routes — wrapped in ShellComponent
  {
    path: "",
    canActivate: [authGuard],
    loadComponent: () =>
      import("./shared/components/layout/shell/shell.component").then(
        (m) => m.ShellComponent
      ),
    children: [
      {
        path: "dashboard",
        loadComponent: () =>
          import("./features/dashboard/dashboard.component").then(
            (m) => m.DashboardComponent
          ),
      },
      {
        path: "documents",
        loadChildren: () =>
          import("./features/documents/documents.routes").then(
            (m) => m.DOCUMENT_ROUTES
          ),
      },
      {
        path: "workflow",
        loadChildren: () =>
          import("./features/workflow/workflow.routes").then(
            (m) => m.WORKFLOW_ROUTES
          ),
      },
      {
        path: "notifications",
        loadComponent: () =>
          import("./features/notifications/notifications.component").then(
            (m) => m.NotificationsComponent
          ),
      },
    ],
  },

  { path: "**", redirectTo: "dashboard" },
];
