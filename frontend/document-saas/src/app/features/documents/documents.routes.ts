import { Routes } from "@angular/router";

export const DOCUMENT_ROUTES: Routes = [
  {
    path: "",
    loadComponent: () =>
      import("./document-list/document-list.component").then(
        (m) => m.DocumentListComponent
      ),
  },
  {
    path: "upload",
    loadComponent: () =>
      import("./upload/upload.component").then(
        (m) => m.UploadComponent
      ),
  },
  {
    path: ":id",
    loadComponent: () =>
      import("./document-detail/document-detail.component").then(
        (m) => m.DocumentDetailComponent
      ),
  },
];
