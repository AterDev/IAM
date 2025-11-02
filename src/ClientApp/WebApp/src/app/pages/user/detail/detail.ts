import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserDetailDto } from 'src/app/services/api/models/identity-mod/user-detail-dto.model';
import { UserEditComponent } from '../edit/edit';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-detail',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    MatCardModule,
    MatProgressSpinnerModule,
    MatChipsModule
  ],
  templateUrl: './detail.html',
  styleUrls: ['./detail.scss']
})
export class UserDetailComponent implements OnInit {
  // Keep signals only for template-reactive values
  user = signal<UserDetailDto | null>(null);
  
  isLoading = false;
  userId?: string;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiClient,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id') || '';
    if (this.userId) {
      this.loadUser();
    }
  }

  loadUser(): void {
    this.isLoading = true;
    this.api.users.getDetail(this.userId!).subscribe({
      next: (user) => {
        this.user.set(user);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.snackBar.open('Failed to load user', 'Close', { duration: 3000 });
        this.router.navigate(['/system-user']);
      }
    });
  }

  openEditDialog(): void {
    const dialogRef = this.dialog.open(UserEditComponent, {
      width: '600px',
      data: { userId: this.userId }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadUser();
      }
    });
  }

  toggleUserStatus(): void {
    const user = this.user();
    if (!user) {
      return;
    }

    const lockoutEnd = user.lockoutEnabled ? null : new Date(Date.now() + 365 * 24 * 60 * 60 * 1000);
    
    this.api.users.updateStatus(this.userId!, lockoutEnd as any).subscribe({
      next: () => {
        this.snackBar.open(
          user.lockoutEnabled ? 'User unlocked successfully' : 'User locked successfully',
          'Close',
          { duration: 3000 }
        );
        this.loadUser();
      },
      error: () => {
        this.snackBar.open('Failed to update user status', 'Close', { duration: 3000 });
      }
    });
  }

  deleteUser(): void {
    const user = this.user();
    if (!user) {
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete User',
        message: `Are you sure you want to delete user "${user.userName}"?`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.users.deleteUser(this.userId!, false).subscribe({
          next: () => {
            this.snackBar.open('User deleted successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/system-user']);
          },
          error: () => {
            this.snackBar.open('Failed to delete user', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/system-user']);
  }
}
