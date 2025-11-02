import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserItemDto } from 'src/app/services/api/models/identity-mod/user-item-dto.model';
import { PageList } from 'src/app/services/api/models/ater/page-list.model';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { UserEditComponent } from '../edit/edit';
import { UserAddComponent } from '../add/add';

@Component({
  selector: 'app-list',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatTableModule,
    MatPaginatorModule,
    MatCheckboxModule,
    MatChipsModule,
    MatDialogModule,
    MatMenuModule,
    FormsModule
  ],
  templateUrl: './list.html',
  styleUrls: ['./list.scss']
})
export class UserListComponent implements OnInit {
  displayedColumns: string[] = ['select', 'userName', 'email', 'phoneNumber', 'lockoutEnabled', 'createdTime', 'actions'];

  // Use signals only for reactive template values
  dataSource = signal<UserItemDto[]>([]);
  total = signal(0);
  selectedIds = signal<Set<string>>(new Set());

  // Regular properties for non-reactive values
  pageSize = 10;
  pageIndex = 0;
  isLoading = signal(false);
  searchText = '';
  lockoutEnabled: boolean | null = null;

  // Computed
  allSelected = computed(() => {
    const data = this.dataSource();
    const selected = this.selectedIds();
    return data.length > 0 && data.every(item => selected.has(item.id));
  });

  someSelected = computed(() => {
    const data = this.dataSource();
    const selected = this.selectedIds();
    return selected.size > 0 && !this.allSelected();
  });

  constructor(
    private api: ApiClient,
    private router: Router,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
  this.isLoading.set(true);

    this.api.users.getUsers(
      this.searchText || null,
      this.searchText || null,
      this.searchText || null,
      this.lockoutEnabled,
      null,
      null,
      this.pageIndex + 1,
      this.pageSize,
      null
    ).subscribe({
      next: (result: PageList<UserItemDto>) => {
        this.dataSource.set(result.data);
        this.total.set(result.count);
  this.isLoading.set(false);
      },
      error: () => {
  this.isLoading.set(false);
        this.snackBar.open('Failed to load users', 'Close', { duration: 3000 });
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadData();
  }

  onSearch(): void {
    this.pageIndex = 0;
    this.loadData();
  }

  toggleSelectAll(): void {
    if (this.allSelected()) {
      this.selectedIds.set(new Set());
    } else {
      this.selectedIds.set(new Set(this.dataSource().map(item => item.id)));
    }
  }

  toggleSelect(id: string): void {
    const selected = new Set(this.selectedIds());
    if (selected.has(id)) {
      selected.delete(id);
    } else {
      selected.add(id);
    }
    this.selectedIds.set(selected);
  }

  openAddDialog(): void {
    const dialogRef = this.dialog.open(UserAddComponent, {
      width: '600px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadData();
      }
    });
  }

  openEditDialog(user: UserItemDto): void {
    const dialogRef = this.dialog.open(UserEditComponent, {
      width: '600px',
      data: { userId: user.id }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadData();
      }
    });
  }

  viewDetail(user: UserItemDto): void {
    this.router.navigate(['/user', user.id]);
  }

  toggleUserStatus(user: UserItemDto): void {
    const lockoutEnd = user.lockoutEnabled ? null : new Date(Date.now() + 365 * 24 * 60 * 60 * 1000);

    this.api.users.updateStatus(user.id, lockoutEnd as any).subscribe({
      next: () => {
        this.snackBar.open(
          user.lockoutEnabled ? 'User unlocked successfully' : 'User locked successfully',
          'Close',
          { duration: 3000 }
        );
        this.loadData();
      },
      error: () => {
        this.snackBar.open('Failed to update user status', 'Close', { duration: 3000 });
      }
    });
  }

  deleteUser(user: UserItemDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete User',
        message: `Are you sure you want to delete user "${user.userName}"?`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.users.deleteUser(user.id, false).subscribe({
          next: () => {
            this.snackBar.open('User deleted successfully', 'Close', { duration: 3000 });
            this.loadData();
          },
          error: () => {
            this.snackBar.open('Failed to delete user', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }

  batchLock(): void {
    const selectedIds = Array.from(this.selectedIds());
    if (selectedIds.length === 0) {
      this.snackBar.open('Please select users to lock', 'Close', { duration: 3000 });
      return;
    }

    const lockoutEnd = new Date(Date.now() + 365 * 24 * 60 * 60 * 1000);
    let completed = 0;

    selectedIds.forEach(id => {
      this.api.users.updateStatus(id, lockoutEnd as any).subscribe({
        next: () => {
          completed++;
          if (completed === selectedIds.length) {
            this.snackBar.open(`${selectedIds.length} users locked successfully`, 'Close', { duration: 3000 });
            this.selectedIds.set(new Set());
            this.loadData();
          }
        },
        error: () => {
          completed++;
          if (completed === selectedIds.length) {
            this.loadData();
          }
        }
      });
    });
  }

  batchUnlock(): void {
    const selectedIds = Array.from(this.selectedIds());
    if (selectedIds.length === 0) {
      this.snackBar.open('Please select users to unlock', 'Close', { duration: 3000 });
      return;
    }

    let completed = 0;

    selectedIds.forEach(id => {
      this.api.users.updateStatus(id, null as any).subscribe({
        next: () => {
          completed++;
          if (completed === selectedIds.length) {
            this.snackBar.open(`${selectedIds.length} users unlocked successfully`, 'Close', { duration: 3000 });
            this.selectedIds.set(new Set());
            this.loadData();
          }
        },
        error: () => {
          completed++;
          if (completed === selectedIds.length) {
            this.loadData();
          }
        }
      });
    });
  }
}
