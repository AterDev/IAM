import { NestedTreeControl } from '@angular/cdk/tree';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { TranslateService } from '@ngx-translate/core';
import { DotnetSwaggerClient } from 'src/app/services/dotnet-swagger/dotnet-swagger-client';
import { PermissionType } from 'src/app/services/dotnet-swagger/models/entity/permission-type.model';
import { ClientItemDto } from 'src/app/services/dotnet-swagger/models/iammod/client-item-dto.model';
import { PermissionDetailDto } from 'src/app/services/dotnet-swagger/models/iammod/permission-detail-dto.model';
import { PermissionTreeNodeDto } from 'src/app/services/dotnet-swagger/models/iammod/permission-tree-node-dto.model';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { PermissionEditComponent, PermissionParentOption } from '../edit/edit';

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
    AppLoadingComponent,
  ],
  templateUrl: './list.html',
  styleUrls: ['./list.scss'],
})
export class PermissionListComponent implements OnInit {
  readonly treeControl = new NestedTreeControl<PermissionTreeNodeDto>((node) => node.children);
  readonly dataSource = new MatTreeNestedDataSource<PermissionTreeNodeDto>();
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
    private readonly api: DotnetSwaggerClient,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly translate: TranslateService,
  ) {}

  ngOnInit(): void {
    this.loadClients();
  }

  hasChild = (_: number, node: PermissionTreeNodeDto) => !!node.children && node.children.length > 0;

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
    this.api.permissions.getPermissionTree(this.clientId, null, this.type, null, this.keyword || null, null, 1, 2000, null).subscribe({
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

  addChild(node: PermissionTreeNodeDto): void {
    this.openEditDialog(null, node.id);
  }

  editNode(node: PermissionTreeNodeDto): void {
    this.api.permissions.getDetail(node.id).subscribe({
      next: (permission) => this.openEditDialog(permission),
    });
  }

  deleteNode(node: PermissionTreeNodeDto): void {
    this.confirmDelete([node.id], 'permission.deleteConfirm').subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.api.permissions.delete(node.id).subscribe({
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

  getNodeLabel(node: Pick<PermissionTreeNodeDto, 'name'>): string {
    return this.translateLabel(node.name);
  }

  private openEditDialog(permission?: PermissionDetailDto | null, defaultParentId?: string | null): void {
    const currentClient = this.selectedClient();
    const dialogRef = this.dialog.open(PermissionEditComponent, {
      width: '720px',
      data: {
        permission,
        defaultParentId,
        parentOptions: this.flattenPermissions(this.dataSource.data),
        currentClientId: permission?.ownedClientId ?? this.clientId,
        currentClientLabel: currentClient ? `${currentClient.displayName || currentClient.clientId} · ${currentClient.clientId}` : null,
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.loadTree();
      }
    });
  }

  private flattenPermissions(nodes: PermissionTreeNodeDto[], depth = 0): PermissionParentOption[] {
    return nodes.flatMap((node) => {
      const labelPrefix = '—'.repeat(depth);
      const label = this.translateLabel(node.name);
      const item: PermissionParentOption = {
        id: node.id,
        name: `${labelPrefix}${label}`,
      };

      return [item, ...this.flattenPermissions(node.children, depth + 1)];
    });
  }

  private expandAll(nodes: PermissionTreeNodeDto[]): void {
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