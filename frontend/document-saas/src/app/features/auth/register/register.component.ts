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
import { MatStepperModule } from "@angular/material/stepper";
import { AuthService } from "../../../core/services/auth.service";

@Component({
  selector: "app-register",
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
    MatStepperModule,
  ],
  template: `
    <div class="register-container">
      <mat-card class="register-card">
        <mat-card-header>
          <div class="logo-section">
            <mat-icon class="logo-icon">description</mat-icon>
            <h1>Document SaaS</h1>
            <p>Register your company</p>
          </div>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">

            <h3 class="section-title">Company Details</h3>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Company Name</mat-label>
              <input matInput formControlName="tenantName"
                placeholder="Acme Corporation" />
              <mat-icon matSuffix>business</mat-icon>
              @if (registerForm.get("tenantName")?.hasError("required")
                && registerForm.get("tenantName")?.touched) {
                <mat-error>Company name is required</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Subdomain</mat-label>
              <input matInput formControlName="subdomain"
                placeholder="acme-corp" />
              <mat-icon matSuffix>domain</mat-icon>
              <mat-hint>Lowercase letters, numbers, hyphens only</mat-hint>
              @if (registerForm.get("subdomain")?.hasError("pattern")) {
                <mat-error>
                  Only lowercase letters, numbers, hyphens
                </mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Company Email</mat-label>
              <input matInput type="email" formControlName="contactEmail"
                placeholder="contact@acme.com" />
              <mat-icon matSuffix>email</mat-icon>
            </mat-form-field>

            <h3 class="section-title">Admin Account</h3>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Admin Full Name</mat-label>
              <input matInput formControlName="adminFullName"
                placeholder="John Smith" />
              <mat-icon matSuffix>person</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Admin Email</mat-label>
              <input matInput type="email" formControlName="adminEmail"
                placeholder="admin@acme.com" />
              <mat-icon matSuffix>email</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Admin Password</mat-label>
              <input matInput
                [type]="hidePassword() ? 'password' : 'text'"
                formControlName="adminPassword" />
              <button mat-icon-button matSuffix type="button"
                (click)="hidePassword.set(!hidePassword())">
                <mat-icon>
                  {{ hidePassword() ? "visibility_off" : "visibility" }}
                </mat-icon>
              </button>
              <mat-hint>Min 8 chars, uppercase, number, symbol</mat-hint>
            </mat-form-field>

            @if (errorMessage()) {
              <div class="error-banner">
                <mat-icon>error_outline</mat-icon>
                {{ errorMessage() }}
              </div>
            }

            <button
              mat-raised-button color="primary"
              type="submit"
              class="full-width submit-btn"
              [disabled]="isLoading() || registerForm.invalid">
              @if (isLoading()) {
                <mat-spinner diameter="20"></mat-spinner>
                Registering...
              } @else {
                Create Account
              }
            </button>

          </form>
        </mat-card-content>

        <mat-card-actions>
          <p class="login-link">
            Already have an account?
            <a routerLink="/auth/login">Sign in</a>
          </p>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .register-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #1E40AF 0%, #2563EB 100%);
      padding: 16px;
    }
    .register-card {
      width: 100%;
      max-width: 480px;
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
    }
    .logo-section p {
      margin: 0;
      color: #64748B;
      font-size: 14px;
    }
    .section-title {
      font-size: 14px;
      font-weight: 600;
      color: #2563EB;
      margin: 16px 0 8px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }
    .full-width {
      width: 100%;
      margin-bottom: 8px;
    }
    .submit-btn {
      height: 48px;
      font-size: 16px;
      margin-top: 12px;
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
    .login-link {
      text-align: center;
      width: 100%;
      color: #64748B;
      font-size: 14px;
    }
    .login-link a {
      color: #2563EB;
      font-weight: 600;
      text-decoration: none;
    }
    mat-card-actions { justify-content: center; }
  `],
})
export class RegisterComponent {
  registerForm: FormGroup;
  isLoading    = signal(false);
  hidePassword = signal(true);
  errorMessage = signal("");

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.registerForm = this.fb.group({
      tenantName:    ["", Validators.required],
      subdomain:     ["", [
        Validators.required,
        Validators.pattern(/^[a-z0-9-]+$/)
      ]],
      contactEmail:  ["", [Validators.required, Validators.email]],
      adminFullName: ["", Validators.required],
      adminEmail:    ["", [Validators.required, Validators.email]],
      adminPassword: ["", [
        Validators.required,
        Validators.minLength(8)
      ]],
    });
  }

  onSubmit(): void {
    if (this.registerForm.invalid) return;

    this.isLoading.set(true);
    this.errorMessage.set("");

    this.authService.register(this.registerForm.value).subscribe({
      next: () => {
        this.snackBar.open(
          "Account created! Please sign in.", "Close",
          { duration: 4000 }
        );
        this.router.navigate(["/auth/login"]);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(
          err.error?.message ?? "Registration failed. Try again."
        );
      },
    });
  }
}
