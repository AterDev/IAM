import { NestedTreeControl } from '@angular/cdk/tree';
import { Component, OnInit, input, signal } from '@angular/core';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { MatCardModule } from '@angular/material/card';
import { PermissionAdminService } from 'src/app/services/permission-admin.service';
import { PermissionSyncNodeDto, PermissionTreeNode, PermissionType } from 'src/app/services/permission-admin.models';
import { ClientPermissionNodeDialogComponent } from './node-dialog';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';

@Component({
  selector: 'app-client-permissions',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    MatTreeModule,
    MatCardModule,
    AppLoadingComponent,
  ],
  templateUrl: './permissions.html',
  styleUrls: ['./permissions.scss'],
})
export class ClientPermissionsComponent implements OnInit {
  readonly clientId = input.required<string>();
  readonly treeControl = new NestedTreeControl<PermissionSyncNodeDto>((node) => node.children);
  readonly dataSource = new MatTreeNestedDataSource<PermissionSyncNodeDto>();
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly selectedNode = signal<PermissionSyncNodeDto | null>(null);
  readonly permissionTypeLabelKeys: Record<PermissionType, string> = {
    [PermissionType.Menu]: 'permission.typeOptions.menu',
    [PermissionType.Button]: 'permission.typeOptions.button',
    [PermissionType.Business]: 'permission.typeOptions.business',
  };

  constructor(
    private readonly permissionAdminService: PermissionAdminService,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly translate: TranslateService,
  ) {}

  ngOnInit(): void {
    this.loadTree();
  }

  hasChild = (_: number, node: PermissionSyncNodeDto) => !!node.children && node.children.length > 0;

  getNodeLabel(node: Pick<PermissionSyncNodeDto, 'displayName' | 'name'>): string {
    return this.translateLabel(node.displayName || node.name);
  }

  loadTree(): void {
    this.isLoading.set(true);
    this.permissionAdminService.getClientPermissionTree(this.clientId(), {
      onlyNonBusiness: true,
      pageIndex: 1,
      pageSize: 2000,
    }).subscribe({
      next: (tree) => {
        this.dataSource.data = this.toSyncNodes(tree);
        this.expandAll(this.dataSource.data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(this.translate.instant('client.loadPermissionsFailed'), this.translate.instant('common.close'), { duration: 3000 });
      },
    });
  }

  selectNode(node: PermissionSyncNodeDto): void {
    this.selectedNode.set(node);
  }

  addRoot(): void {
    this.openNodeDialog();
  }

  addChild(node: PermissionSyncNodeDto, event?: Event): void {
    event?.stopPropagation();
    this.openNodeDialog(undefined, node);
  }

  editNode(node: PermissionSyncNodeDto, event?: Event): void {
    event?.stopPropagation();
    this.openNodeDialog(node);
  }

  deleteNode(node: PermissionSyncNodeDto, event?: Event): void {
    event?.stopPropagation();
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: this.translate.instant('common.delete'),
        message: this.translate.instant('client.deletePermissionNodeConfirm'),
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.dataSource.data = this.removeNode(this.dataSource.data, node.code);
      this.selectedNode.set(null);
    });
  }

  save(): void {
    if (this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.permissionAdminService.syncClientMenuPermissions(this.clientId(), {
      permissions: this.dataSource.data,
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.snackBar.open(this.translate.instant('client.syncPermissionsSuccess'), this.translate.instant('common.close'), { duration: 3000 });
        this.loadTree();
      },
      error: () => {
        this.isSaving.set(false);
        this.snackBar.open(this.translate.instant('client.syncPermissionsFailed'), this.translate.instant('common.close'), { duration: 3000 });
      },
    });
  }

  private openNodeDialog(node?: PermissionSyncNodeDto, parent?: PermissionSyncNodeDto): void {
    const dialogRef = this.dialog.open(ClientPermissionNodeDialogComponent, {
      width: '820px',
      data: { node },
    });

    dialogRef.afterClosed().subscribe((result?: PermissionSyncNodeDto) => {
      if (!result) {
        return;
      }

      if (node) {
        this.dataSource.data = this.updateNode(this.dataSource.data, node.code, result);
        this.selectedNode.set(result);
        return;
      }

      if (parent) {
        this.dataSource.data = this.insertChild(this.dataSource.data, parent.code, result);
        this.treeControl.expand(parent);
        return;
      }

      this.dataSource.data = [...this.dataSource.data, result].sort((left, right) => left.sort - right.sort);
    });
  }

  private toSyncNodes(nodes: PermissionTreeNode[]): PermissionSyncNodeDto[] {
    return nodes
      .filter((node) => node.type === PermissionType.Menu || node.type === PermissionType.Button)
      .map((node) => ({
        code: node.code,
        name: node.name,
        displayName: node.displayName,
        description: node.description,
        type: node.type,
        namespace: node.namespace,
        resource: node.resource,
        action: node.action,
        path: node.path,
        icon: node.icon,
        sort: node.sort,
        children: this.toSyncNodes(node.children),
      }));
  }

  private removeNode(nodes: PermissionSyncNodeDto[], code: string): PermissionSyncNodeDto[] {
    return nodes
      .filter((node) => node.code !== code)
      .map((node) => ({ ...node, children: this.removeNode(node.children, code) }));
  }

  private updateNode(nodes: PermissionSyncNodeDto[], code: string, updated: PermissionSyncNodeDto): PermissionSyncNodeDto[] {
    return nodes.map((node) => {
      if (node.code === code) {
        return { ...updated, children: updated.children ?? node.children };
      }

      return { ...node, children: this.updateNode(node.children, code, updated) };
    });
  }

  private insertChild(nodes: PermissionSyncNodeDto[], parentCode: string, child: PermissionSyncNodeDto): PermissionSyncNodeDto[] {
    return nodes.map((node) => {
      if (node.code === parentCode) {
        return {
          ...node,
          children: [...node.children, child].sort((left, right) => left.sort - right.sort),
        };
      }

      return { ...node, children: this.insertChild(node.children, parentCode, child) };
    });
  }

  private expandAll(nodes: PermissionSyncNodeDto[]): void {
    nodes.forEach((node) => {
      this.treeControl.expand(node);
      this.expandAll(node.children);
    });
  }

  private translateLabel(label: string): string {
    const translated = this.translate.instant(label);
    return typeof translated === 'string' && translated.trim().length > 0 ? translated : label;
  }
}