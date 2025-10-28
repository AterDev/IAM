import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiClient } from 'src/app/services/api/api-client';
import { ScopeItemDto } from 'src/app/services/api/models/access-mod/scope-item-dto.model';
import { PageList } from 'src/app/services/api/models/ater/page-list.model';

@Component({
  selector: 'app-scope-list',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatTableModule,
    MatPaginatorModule,
    MatChipsModule,
    FormsModule
  ],
  templateUrl: './scope-list.html',
  styleUrls: ['./scope-list.scss']
})
export class ScopeListComponent implements OnInit {
  displayedColumns: string[] = ['name', 'displayName', 'required', 'emphasize', 'description'];
  
  dataSource = signal<ScopeItemDto[]>([]);
  total = signal(0);
  
  pageSize = 10;
  pageIndex = 0;
  isLoading = false;
  searchText = '';
  requiredFilter: boolean | null = null;

  constructor(
    private api: ApiClient,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    
    this.api.scopes.getScopes(
      this.searchText || null,
      this.searchText || null,
      this.requiredFilter,
      this.pageIndex + 1,
      this.pageSize,
      null
    ).subscribe({
      next: (res: PageList<ScopeItemDto>) => {
        this.dataSource.set(res.data);
        this.total.set(res.count);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load scopes:', error);
        this.snackBar.open('加载作用域列表失败', '关闭', { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  onSearch(): void {
    this.pageIndex = 0;
    this.loadData();
  }

  onFilterChange(): void {
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
    this.requiredFilter = null;
    this.pageIndex = 0;
    this.loadData();
  }
}
