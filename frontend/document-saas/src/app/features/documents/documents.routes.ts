import { Routes } from "@angular/router";

export const DOCUMENT_ROUTES: Routes = [
  {
    path: "",
    loadComponent: () =>
      import("./document-list/document-list.component").then(
        (m) => m.DocumentListComponent
      ),
  },
];
