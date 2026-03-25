import { NestedTreeControl } from '@angular/cdk/tree';
import { Component, OnInit, input, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';
import { TranslateService } from '@ngx-translate/core';
import { DotnetSwaggerClient } from 'src/app/services/dotnet-swagger/dotnet-swagger-client';
import { PermissionType } from 'src/app/services/dotnet-swagger/models/entity/permission-type.model';
import { PermissionSyncNodeDto } from 'src/app/services/dotnet-swagger/models/iammod/permission-sync-node-dto.model';
import { PermissionTreeNodeDto } from 'src/app/services/dotnet-swagger/models/iammod/permission-tree-node-dto.model';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { ClientPermissionNodeDialogComponent } from './node-dialog';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

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
  readonly i18n = I18N_KEYS;
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
    private readonly api: DotnetSwaggerClient,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly translate: TranslateService,
  ) {}

  ngOnInit(): void {
    this.loadTree();
  }

  hasChild = (_: number, node: PermissionSyncNodeDto) => !!node.children && node.children.length > 0;

  getNodeLabel(node: Pick<PermissionSyncNodeDto, 'name'>): string {
    return this.translateLabel(node.name);
  }

  loadTree(): void {
    this.isLoading.set(true);
    this.api.clients.getPermissionTree(this.clientId(), null, null, null, null, null, true, 1, 2000, null).subscribe({
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
    this.api.clients.syncMenuPermissions(this.clientId(), {
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
      width: '720px',
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

      this.dataSource.data = [...this.dataSource.data, result].sort((left, right) => left.code.localeCompare(right.code));
    });
  }

  private toSyncNodes(nodes: PermissionTreeNodeDto[]): PermissionSyncNodeDto[] {
    return nodes
      .filter((node) => node.type === PermissionType.Menu || node.type === PermissionType.Button)
      .map((node) => ({
        code: node.code,
        name: node.name,
        description: node.description,
        type: node.type,
        path: node.path,
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
          children: [...node.children, child].sort((left, right) => left.code.localeCompare(right.code)),
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