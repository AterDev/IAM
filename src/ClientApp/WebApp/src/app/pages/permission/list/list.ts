import { NestedTreeControl } from '@angular/cdk/tree';
import { Component, OnInit, signal } from '@angular/core';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormsModule } from '@angular/forms';
import { ApiClient } from 'src/app/services/api/api-client';
import { ClientItemDto } from 'src/app/services/api/models/iammod/client-item-dto.model';
import { PermissionAdminService } from 'src/app/services/permission-admin.service';
import { PermissionItem, PermissionTreeNode, PermissionType } from 'src/app/services/permission-admin.models';
import { PermissionEditComponent } from '../edit/edit';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';

@Component({
  selector: 'app-permission-list',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatTreeModule,
    MatCardModule,
    MatChipsModule,
    MatCheckboxModule,
    FormsModule,
    AppLoadingComponent,
  ],
  templateUrl: './list.html',
  styleUrls: ['./list.scss'],
})
export class PermissionListComponent implements OnInit {
  readonly treeControl = new NestedTreeControl<PermissionTreeNode>((node) => node.children);
  readonly dataSource = new MatTreeNestedDataSource<PermissionTreeNode>();
  readonly isLoading = signal(false);
  readonly selectedNode = signal<PermissionTreeNode | null>(null);
  readonly selectedIds = signal<Set<string>>(new Set());
  readonly clients = signal<ClientItemDto[]>([]);
  readonly typeOptions = [
    { labelKey: 'common.all', value: null },
    { labelKey: 'permission.typeOptions.menu', value: PermissionType.Menu },
    { labelKey: 'permission.typeOptions.button', value: PermissionType.Button },
    { labelKey: 'permission.typeOptions.business', value: PermissionType.Business },
  ];
  readonly permissionTypeLabelKeys: Record<PermissionType, string> = {
    [PermissionType.Menu]: 'permission.typeOptions.menu',
    [PermissionType.Button]: 'permission.typeOptions.button',
    [PermissionType.Business]: 'permission.typeOptions.business',
  };

  keyword = '';
  clientId: string | null = null;
  type: PermissionType | null = null;

  constructor(
    private readonly permissionAdminService: PermissionAdminService,
    private readonly api: ApiClient,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly translate: TranslateService,
  ) {}

  ngOnInit(): void {
    this.loadClients();
    this.loadTree();
  }

  hasChild = (_: number, node: PermissionTreeNode) => !!node.children && node.children.length > 0;

  loadClients(): void {
    this.api.clients.getClients(null, null, null, null, 1, 200, null).subscribe({
      next: (result) => this.clients.set(result.data),
    });
  }

  loadTree(): void {
    this.isLoading.set(true);
    this.permissionAdminService.getPermissionTree({
      clientId: this.clientId,
      type: this.type,
      keyword: this.keyword || null,
      pageIndex: 1,
      pageSize: 2000,
    }).subscribe({
      next: (tree) => {
        this.dataSource.data = tree;
        this.expandAll(tree);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(this.translate.instant('permission.loadFailed'), this.translate.instant('common.close'), { duration: 3000 });
      },
    });
  }

  selectNode(node: PermissionTreeNode): void {
    this.selectedNode.set(node);
  }

  toggleBatchSelect(node: PermissionTreeNode, checked: boolean): void {
    const selected = new Set(this.selectedIds());
    this.collectNodeIds(node).forEach((id) => checked ? selected.add(id) : selected.delete(id));
    this.selectedIds.set(selected);
  }

  isBatchSelected(node: PermissionTreeNode): boolean {
    return this.selectedIds().has(node.id);
  }

  addRoot(): void {
    this.openEditDialog();
  }

  addChild(node: PermissionTreeNode, event?: Event): void {
    event?.stopPropagation();
    this.openEditDialog(null, node.id);
  }

  editNode(node: PermissionTreeNode, event?: Event): void {
    event?.stopPropagation();
    this.permissionAdminService.getPermissionDetail(node.id).subscribe({
      next: (permission) => this.openEditDialog(permission),
    });
  }

  deleteNode(node: PermissionTreeNode, event?: Event): void {
    event?.stopPropagation();
    this.confirmDelete([node.id], 'permission.deleteConfirm').subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.permissionAdminService.deletePermission(node.id).subscribe({
        next: () => {
          this.snackBar.open(this.translate.instant('permission.deleteSuccess'), this.translate.instant('common.close'), { duration: 3000 });
          this.loadTree();
        },
        error: () => {
          this.snackBar.open(this.translate.instant('permission.deleteFailed'), this.translate.instant('common.close'), { duration: 3000 });
        },
      });
    });
  }

  deleteSelected(): void {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0) {
      return;
    }

    this.confirmDelete(ids, 'permission.deleteSelectedConfirm').subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      let completed = 0;
      let failed = 0;
      ids.forEach((id) => {
        this.permissionAdminService.deletePermission(id).subscribe({
          next: () => {
            completed++;
            this.finishBatchDelete(ids.length, completed, failed);
          },
          error: () => {
            failed++;
            this.finishBatchDelete(ids.length, completed, failed);
          },
        });
      });
    });
  }

  clearFilters(): void {
    this.keyword = '';
    this.clientId = null;
    this.type = null;
    this.loadTree();
  }

  protected readonly permissionType = PermissionType;

  getPermissionTypeLabelKey(type: PermissionType): string {
    return this.permissionTypeLabelKeys[type];
  }

  getNodeLabel(node: Pick<PermissionTreeNode, 'displayName' | 'name'>): string {
    return this.translateLabel(node.displayName || node.name);
  }

  private openEditDialog(permission?: PermissionItem | null, defaultParentId?: string | null): void {
    const dialogRef = this.dialog.open(PermissionEditComponent, {
      width: '900px',
      data: {
        permission,
        defaultParentId,
        clients: this.clients(),
        parentOptions: this.flattenPermissions(this.dataSource.data),
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.loadTree();
      }
    });
  }

  private flattenPermissions(nodes: PermissionTreeNode[], depth = 0): PermissionItem[] {
    return nodes.flatMap((node) => {
      const labelPrefix = '—'.repeat(depth);
      const item: PermissionItem = {
        id: node.id,
        code: node.code,
        name: `${labelPrefix}${node.name}`,
        displayName: node.displayName,
        description: node.description,
        type: node.type,
        parentId: node.parentId,
        namespace: node.namespace,
        resource: node.resource,
        action: node.action,
        path: node.path,
        icon: node.icon,
        sort: node.sort,
        ownedClientId: node.ownedClientId,
        ownedClientCode: node.ownedClientCode,
        createdTime: '',
        updatedTime: '',
      };

      return [item, ...this.flattenPermissions(node.children, depth + 1)];
    });
  }

  private collectNodeIds(node: PermissionTreeNode): string[] {
    return [node.id, ...node.children.flatMap((child) => this.collectNodeIds(child))];
  }

  private expandAll(nodes: PermissionTreeNode[]): void {
    nodes.forEach((node) => {
      this.treeControl.expand(node);
      if (node.children.length > 0) {
        this.expandAll(node.children);
      }
    });
  }

  private confirmDelete(ids: string[], messageKey: string) {
    return this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: this.translate.instant('common.delete'),
        message: this.translate.instant(messageKey, { count: ids.length }),
      },
    }).afterClosed();
  }

  private finishBatchDelete(total: number, success: number, failed: number): void {
    if (success + failed !== total) {
      return;
    }

    this.selectedIds.set(new Set());
    this.loadTree();
    this.snackBar.open(
      this.translate.instant(failed === 0 ? 'permission.deleteSelectedSuccess' : 'permission.deleteSelectedPartial', { success, failed }),
      this.translate.instant('common.close'),
      { duration: 4000 },
    );
  }

  private translateLabel(label: string): string {
    const translated = this.translate.instant(label);
    return typeof translated === 'string' && translated.trim().length > 0 ? translated : label;
  }
}