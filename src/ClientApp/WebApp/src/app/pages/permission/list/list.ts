import { NestedTreeControl } from '@angular/cdk/tree';
import { Component, OnInit, computed, signal } from '@angular/core';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatTabsModule } from '@angular/material/tabs';
import { FormsModule } from '@angular/forms';
import { MatMenuModule } from '@angular/material/menu';
import { ApiClient } from 'src/app/services/api/api-client';
import { ClientItemDto } from 'src/app/services/api/models/iammod/client-item-dto.model';
import { PermissionAdminService } from 'src/app/services/permission-admin.service';
import { PermissionItem, PermissionTreeNode, PermissionType } from 'src/app/services/permission-admin.models';
import { PermissionEditComponent } from '../edit/edit';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-permission-list',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatTreeModule,
    MatCardModule,
    MatListModule,
    MatTabsModule,
    FormsModule,
    MatMenuModule,
    AppLoadingComponent,
  ],
  templateUrl: './list.html',
  styleUrls: ['./list.scss'],
})
export class PermissionListComponent implements OnInit {
  readonly treeControl = new NestedTreeControl<PermissionTreeNode>((node) => node.children);
  readonly dataSource = new MatTreeNestedDataSource<PermissionTreeNode>();
  readonly isLoading = signal(false);
  readonly clients = signal<ClientItemDto[]>([]);
  readonly selectedClient = computed(() => this.clients().find((client) => client.id === this.clientId) ?? null);
  readonly typeTabs = [
    { labelKey: 'permission.typeOptions.menu', value: PermissionType.Menu },
    { labelKey: 'permission.typeOptions.button', value: PermissionType.Button },
    { labelKey: 'permission.typeOptions.business', value: PermissionType.Business },
  ];

  readonly i18n = I18N_KEYS;

  keyword = '';
  clientId: string | null = null;
  type = PermissionType.Menu;
  activeTabIndex = 0;

  constructor(
    private readonly permissionAdminService: PermissionAdminService,
    private readonly api: ApiClient,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly translate: TranslateService,
  ) {}

  ngOnInit(): void {
    this.loadClients();
  }

  hasChild = (_: number, node: PermissionTreeNode) => !!node.children && node.children.length > 0;

  loadClients(): void {
    this.api.clients.getClients(null, null, null, null, 1, 200, null).subscribe({
      next: (result) => {
        const clients = result.data;
        this.clients.set(clients);

        if (!this.clientId && clients.length > 0) {
          this.clientId = clients[0].id;
        }

        this.loadTree();
      },
      error: () => {
        this.snackBar.open(this.translate.instant(this.i18n.error.loadClientsFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
      },
    });
  }

  loadTree(): void {
    if (!this.clientId) {
      this.dataSource.data = [];
      this.isLoading.set(false);
      return;
    }

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
        this.snackBar.open(this.translate.instant(this.i18n.permission.loadFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
      },
    });
  }

  selectClient(client: ClientItemDto): void {
    if (this.clientId === client.id) {
      return;
    }

    this.clientId = client.id;
    this.loadTree();
  }

  onTabChange(index: number): void {
    const nextType = this.typeTabs[index]?.value;
    if (!nextType || this.type === nextType) {
      this.activeTabIndex = index;
      return;
    }

    this.activeTabIndex = index;
    this.type = nextType;
    this.loadTree();
  }

  trackByClientId(_: number, client: ClientItemDto): string {
    return client.id;
  }

  addRoot(): void {
    this.openEditDialog();
  }

  addChild(node: PermissionTreeNode): void {
    this.openEditDialog(null, node.id);
  }

  editNode(node: PermissionTreeNode): void {
    this.permissionAdminService.getPermissionDetail(node.id).subscribe({
      next: (permission) => this.openEditDialog(permission),
    });
  }

  deleteNode(node: PermissionTreeNode): void {
    this.confirmDelete([node.id], 'permission.deleteConfirm').subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.permissionAdminService.deletePermission(node.id).subscribe({
        next: () => {
          this.snackBar.open(this.translate.instant(this.i18n.permission.deleteSuccess), this.translate.instant(this.i18n.common.close), { duration: 3000 });
          this.loadTree();
        },
        error: () => {
          this.snackBar.open(this.translate.instant(this.i18n.permission.deleteFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
        },
      });
    });
  }

  clearFilters(): void {
    this.keyword = '';
    this.loadTree();
  }

  getNodeLabel(node: Pick<PermissionTreeNode, 'displayName' | 'name'>): string {
    return this.translateLabel(node.displayName || node.name);
  }

  private openEditDialog(permission?: PermissionItem | null, defaultParentId?: string | null): void {
    const currentClient = this.selectedClient();
    const dialogRef = this.dialog.open(PermissionEditComponent, {
      width: '720px',
      data: {
        permission,
        defaultParentId,
        clients: this.clients(),
        parentOptions: this.flattenPermissions(this.dataSource.data),
        currentClientId: permission?.ownedClientId ?? this.clientId,
        currentClientLabel: currentClient ? `${currentClient.displayName || currentClient.clientId} · ${currentClient.clientId}` : null,
        lockClient: true,
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
      const label = this.translateLabel(node.displayName || node.name);
      const item: PermissionItem = {
        id: node.id,
        code: node.code,
        name: `${labelPrefix}${label}`,
        displayName: `${labelPrefix}${label}`,
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
        title: this.translate.instant(this.i18n.common.delete),
        message: this.translate.instant(messageKey, { count: ids.length }),
      },
    }).afterClosed();
  }

  private translateLabel(label: string): string {
    const translated = this.translate.instant(label);
    return typeof translated === 'string' && translated.trim().length > 0 ? translated : label;
  }
}