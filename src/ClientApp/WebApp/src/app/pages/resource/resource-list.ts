import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { ApiClient } from 'src/app/services/api/api-client';
import { ResourceItemDto } from 'src/app/services/api/models/access-mod/resource-item-dto.model';
import { PageList } from 'src/app/services/api/models/ater/page-list.model';
import { ResourceAddComponent } from './resource-add';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-resource-list',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatTableModule,
    MatPaginatorModule,
    MatMenuModule,
    FormsModule
  ],
  templateUrl: './resource-list.html',
  styleUrls: ['./resource-list.scss']
})
export class ResourceListComponent implements OnInit {
  displayedColumns: string[] = ['name', 'displayName', 'description', 'actions'];
  
  dataSource = signal<ResourceItemDto[]>([]);
  total = signal(0);
  
  pageSize = 10;
  pageIndex = 0;
  isLoading = false;
  searchText = '';

  constructor(
    private api: ApiClient,
    private router: Router,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    
    this.api.resources.getResources(
      this.searchText || null,
      this.searchText || null,
      this.pageIndex + 1,
      this.pageSize,
      null
    ).subscribe({
      next: (res: PageList<ResourceItemDto>) => {
        this.dataSource.set(res.data);
        this.total.set(res.count);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load resources:', error);
        this.snackBar.open(
          this.translate.instant('error.loadResourcesFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
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

  clearFilters(): void {
    this.searchText = '';
    this.pageIndex = 0;
    this.loadData();
  }

  viewDetail(id: string): void {
    this.router.navigate(['/resource', id]);
  }

  openAddDialog(): void {
    const dialogRef = this.dialog.open(ResourceAddComponent, {
      width: '600px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadData();
      }
    });
  }

  deleteResource(id: string): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant('resource.deleteConfirmTitle'),
        message: this.translate.instant('resource.deleteConfirmMessage')
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.resources.deleteResource(id).subscribe({
          next: () => {
            this.snackBar.open(
              this.translate.instant('resource.deleteSuccess'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
            this.loadData();
          },
          error: (error) => {
            console.error('Failed to delete resource:', error);
            this.snackBar.open(
              this.translate.instant('error.deleteResourceFailed'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
          }
        });
      }
    });
  }
}
