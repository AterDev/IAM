import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { UsersService } from 'src/app/services/api/services/users.service';
import { UserDetailDto } from 'src/app/services/api/models/identity-mod/user-detail-dto.model';
import { UserEditComponent } from './user-edit';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-user-detail',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    MatCardModule,
    MatProgressSpinnerModule,
    MatChipsModule
  ],
  templateUrl: './user-detail.html',
  styleUrls: ['./user-detail.scss']
})
export class UserDetailComponent implements OnInit {
  user = signal<UserDetailDto | null>(null);
  isLoading = signal(true);
  userId?: string;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private usersService: UsersService,
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
    this.isLoading.set(true);
    this.usersService.getDetail(this.userId!).subscribe({
      next: (user) => {
        this.user.set(user);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
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
    
    this.usersService.updateStatus(this.userId!, lockoutEnd as any).subscribe({
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
        this.usersService.deleteUser(this.userId!, false).subscribe({
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
