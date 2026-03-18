import { Component, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Router, RouterLink } from "@angular/router";
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from "@angular/forms";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";
import { AuthService } from "../../../core/services/auth.service";

@Component({
  selector: "app-login",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  template: `
    <div class="login-container">
      <mat-card class="login-card">
        <mat-card-header>
          <div class="logo-section">
            <mat-icon class="logo-icon">description</mat-icon>
            <h1>Document SaaS</h1>
            <p>Sign in to your account</p>
          </div>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Email</mat-label>
              <input
                matInput
                type="email"
                formControlName="email"
                placeholder="admin@company.com"
              />
              <mat-icon matSuffix>email</mat-icon>
              @if (loginForm.get("email")?.hasError("required")
                && loginForm.get("email")?.touched) {
                <mat-error>Email is required</mat-error>
              }
              @if (loginForm.get("email")?.hasError("email")) {
                <mat-error>Invalid email format</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Password</mat-label>
              <input
                matInput
                [type]="hidePassword() ? 'password' : 'text'"
                formControlName="password"
              />
              <button
                mat-icon-button
                matSuffix
                type="button"
                (click)="hidePassword.set(!hidePassword())"
              >
                <mat-icon>
                  {{ hidePassword() ? "visibility_off" : "visibility" }}
                </mat-icon>
              </button>
              @if (loginForm.get("password")?.hasError("required")
                && loginForm.get("password")?.touched) {
                <mat-error>Password is required</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Company Subdomain</mat-label>
              <input
                matInput
                formControlName="subdomain"
                placeholder="your-company"
              />
              <mat-icon matSuffix>domain</mat-icon>
              @if (loginForm.get("subdomain")?.hasError("required")
                && loginForm.get("subdomain")?.touched) {
                <mat-error>Subdomain is required</mat-error>
              }
            </mat-form-field>

            @if (errorMessage()) {
              <div class="error-banner">
                <mat-icon>error_outline</mat-icon>
                {{ errorMessage() }}
              </div>
            }

            <button
              mat-raised-button
              color="primary"
              type="submit"
              class="full-width submit-btn"
              [disabled]="isLoading() || loginForm.invalid"
            >
              @if (isLoading()) {
                <mat-spinner diameter="20"></mat-spinner>
                Signing in...
              } @else {
                Sign In
              }
            </button>

          </form>
        </mat-card-content>

        <mat-card-actions>
          <p class="register-link">
            New company?
            <a routerLink="/auth/register">Register here</a>
          </p>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .login-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #1E40AF 0%, #2563EB 100%);
      padding: 16px;
    }
    .login-card {
      width: 100%;
      max-width: 420px;
      border-radius: 16px;
      padding: 8px;
    }
    .logo-section {
      text-align: center;
      width: 100%;
      padding: 16px 0;
    }
    .logo-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: #2563EB;
    }
    .logo-section h1 {
      margin: 8px 0 4px;
      font-size: 24px;
      font-weight: 700;
      color: #1E293B;
    }
    .logo-section p {
      margin: 0;
      color: #64748B;
      font-size: 14px;
    }
    .full-width {
      width: 100%;
      margin-bottom: 8px;
    }
    .submit-btn {
      height: 48px;
      font-size: 16px;
      margin-top: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
    }
    .error-banner {
      display: flex;
      align-items: center;
      gap: 8px;
      background: #FEF2F2;
      color: #DC2626;
      padding: 10px 14px;
      border-radius: 8px;
      font-size: 14px;
      margin-bottom: 12px;
    }
    .register-link {
      text-align: center;
      width: 100%;
      color: #64748B;
      font-size: 14px;
    }
    .register-link a {
      color: #2563EB;
      font-weight: 600;
      text-decoration: none;
    }
    mat-card-actions {
      justify-content: center;
    }
  `],
})
export class LoginComponent {
  loginForm: FormGroup;
  isLoading  = signal(false);
  hidePassword = signal(true);
  errorMessage = signal("");

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.loginForm = this.fb.group({
      email:     ["", [Validators.required, Validators.email]],
      password:  ["", [Validators.required]],
      subdomain: ["", [Validators.required]],
    });

    // Redirect if already logged in
    if (this.authService.isLoggedIn()) {
      this.router.navigate(["/dashboard"]);
    }
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;

    this.isLoading.set(true);
    this.errorMessage.set("");

    this.authService.login(this.loginForm.value).subscribe({
      next: () => {
        this.snackBar.open("Welcome back!", "Close", {
          duration: 3000,
        });
        this.router.navigate(["/dashboard"]);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(
          err.status === 401
            ? "Invalid email, password or subdomain"
            : "Login failed. Please try again."
        );
      },
    });
  }
}
