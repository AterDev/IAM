import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { ApiClient } from 'src/app/services/api/api-client';
import { RoleItemDto } from 'src/app/services/api/models/identity-mod/role-item-dto.model';
import { PageList } from 'src/app/services/api/models/ater/page-list.model';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { RoleEditComponent } from './role-edit';
import { RoleAddComponent } from './role-add';
import { RolePermissionsComponent } from './role-permissions';

@Component({
  selector: 'app-role-list',
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
    MatDividerModule,
    FormsModule
  ],
  templateUrl: './role-list.html',
  styleUrls: ['./role-list.scss']
})
export class RoleListComponent implements OnInit {
  displayedColumns: string[] = ['select', 'name', 'description', 'createdTime', 'actions'];
  
  // Use signals only for reactive template values
  dataSource = signal<RoleItemDto[]>([]);
  total = signal(0);
  selectedIds = signal<Set<string>>(new Set());
  
  // Regular properties for non-reactive values
  pageSize = 10;
  pageIndex = 0;
  isLoading = false;
  searchText = '';

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
    this.isLoading = true;
    
    this.api.roles.getRoles(
      this.searchText || null,
      null,
      null,
      this.pageIndex + 1,
      this.pageSize,
      null
    ).subscribe({
      next: (res: PageList<RoleItemDto>) => {
        this.dataSource.set(res.data);
        this.total.set(res.count);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load roles:', error);
        this.snackBar.open('加载角色列表失败', '关闭', { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  onSearch(): void {
    this.pageIndex = 0;
    this.loadData();
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.pageIndex = event.pageIndex;
    this.loadData();
  }

  toggleAll(): void {
    const data = this.dataSource();
    const selected = this.selectedIds();
    
    if (this.allSelected()) {
      this.selectedIds.set(new Set());
    } else {
      this.selectedIds.set(new Set(data.map(item => item.id)));
    }
  }

  toggleSelection(id: string): void {
    const selected = new Set(this.selectedIds());
    
    if (selected.has(id)) {
      selected.delete(id);
    } else {
      selected.add(id);
    }
    
    this.selectedIds.set(selected);
  }

  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  openAddDialog(): void {
    const dialogRef = this.dialog.open(RoleAddComponent, {
      width: '500px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadData();
      }
    });
  }

  openEditDialog(role: RoleItemDto): void {
    const dialogRef = this.dialog.open(RoleEditComponent, {
      width: '500px',
      data: role
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadData();
      }
    });
  }

  openPermissionsDialog(role: RoleItemDto): void {
    const dialogRef = this.dialog.open(RolePermissionsComponent, {
      width: '700px',
      maxHeight: '80vh',
      data: role
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadData();
      }
    });
  }

  viewDetail(id: string): void {
    this.router.navigate(['/system-role', id]);
  }

  deleteRole(id: string): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: '确认删除',
        message: '确定要删除该角色吗？此操作不可撤销。'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.roles.deleteRole(id, false).subscribe({
          next: () => {
            this.snackBar.open('角色已删除', '关闭', { duration: 3000 });
            this.loadData();
          },
          error: (error) => {
            console.error('Failed to delete role:', error);
            this.snackBar.open('删除角色失败', '关闭', { duration: 3000 });
          }
        });
      }
    });
  }

  deleteBatch(): void {
    const selected = this.selectedIds();
    if (selected.size === 0) {
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: '确认批量删除',
        message: `确定要删除选中的 ${selected.size} 个角色吗？此操作不可撤销。`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        let deletedCount = 0;
        const totalCount = selected.size;
        
        selected.forEach(id => {
          this.api.roles.deleteRole(id, false).subscribe({
            next: () => {
              deletedCount++;
              if (deletedCount === totalCount) {
                this.snackBar.open(`成功删除 ${deletedCount} 个角色`, '关闭', { duration: 3000 });
                this.selectedIds.set(new Set());
                this.loadData();
              }
            },
            error: (error) => {
              console.error('Failed to delete role:', error);
              deletedCount++;
              if (deletedCount === totalCount) {
                this.snackBar.open(`部分角色删除失败`, '关闭', { duration: 3000 });
                this.selectedIds.set(new Set());
                this.loadData();
              }
            }
          });
        });
      }
    });
  }
}
