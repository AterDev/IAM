import { NestedTreeControl } from '@angular/cdk/tree';
import { Component, Inject, OnInit, signal } from '@angular/core';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { CommonModules, CommonFormModules, BaseMatModules } from 'src/app/share/shared-modules';
import { ApiClient } from 'src/app/services/api/api-client';
import { RoleItemDto } from 'src/app/services/api/models/iammod/role-item-dto.model';
import { ClientItemDto } from 'src/app/services/api/models/iammod/client-item-dto.model';
import { PermissionAdminService } from 'src/app/services/permission-admin.service';
import { PermissionTreeNode, PermissionType } from 'src/app/services/permission-admin.models';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';

@Component({
  selector: 'app-permissions',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    ...BaseMatModules,
    MatDialogModule,
    MatCheckboxModule,
    MatChipsModule,
    MatTreeModule,
    MatCardModule,
    FormsModule,
    AppLoadingComponent,
  ],
  templateUrl: './permissions.html',
  styleUrls: ['./permissions.scss'],
})
export class RolePermissionsComponent implements OnInit {
  readonly treeControl = new NestedTreeControl<PermissionTreeNode>((node) => node.children);
  readonly dataSource = new MatTreeNestedDataSource<PermissionTreeNode>();
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly clients = signal<ClientItemDto[]>([]);
  readonly selectedCodes = signal<Set<string>>(new Set());
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

  clientId: string | null = null;
  type: PermissionType | null = null;
  keyword = '';

  constructor(
    private readonly permissionAdminService: PermissionAdminService,
    private readonly api: ApiClient,
    private readonly dialogRef: MatDialogRef<RolePermissionsComponent>,
    private readonly snackBar: MatSnackBar,
    private readonly translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) readonly data: RoleItemDto,
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
    this.permissionAdminService.getRolePermissionTree(this.data.id, {
      clientId: this.clientId,
      type: this.type,
      keyword: this.keyword || null,
      pageIndex: 1,
      pageSize: 2000,
    }).subscribe({
      next: (tree) => {
        this.dataSource.data = tree;
        this.selectedCodes.set(new Set(this.collectSelectedCodes(tree)));
        this.expandAll(tree);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(this.translate.instant('error.loadPermissionsFailed'), this.translate.instant('common.close'), { duration: 3000 });
      },
    });
  }

  isSelected(node: PermissionTreeNode): boolean {
    return this.selectedCodes().has(node.code);
  }

  isIndeterminate(node: PermissionTreeNode): boolean {
    if (node.children.length === 0) {
      return false;
    }

    const descendants = this.collectCodes(node.children);
    const selectedCount = descendants.filter((code) => this.selectedCodes().has(code)).length;
    return selectedCount > 0 && selectedCount < descendants.length;
  }

  toggleNode(node: PermissionTreeNode, checked: boolean): void {
    const selected = new Set(this.selectedCodes());
    this.collectCodes([node]).forEach((code) => checked ? selected.add(code) : selected.delete(code));
    this.selectedCodes.set(selected);
  }

  save(): void {
    if (this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.permissionAdminService.grantRolePermissions(this.data.id, Array.from(this.selectedCodes()).sort()).subscribe({
      next: () => {
        this.snackBar.open(this.translate.instant('success.permissionsSaved'), this.translate.instant('common.close'), { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: () => {
        this.isSaving.set(false);
        this.snackBar.open(this.translate.instant('error.savePermissionsFailed'), this.translate.instant('common.close'), { duration: 3000 });
      },
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }

  clearFilters(): void {
    this.clientId = null;
    this.type = null;
    this.keyword = '';
    this.loadTree();
  }

  protected readonly permissionType = PermissionType;

  getPermissionTypeLabelKey(type: PermissionType): string {
    return this.permissionTypeLabelKeys[type];
  }

  getNodeLabel(node: Pick<PermissionTreeNode, 'displayName' | 'name'>): string {
    return this.translateLabel(node.displayName || node.name);
  }

  private collectSelectedCodes(nodes: PermissionTreeNode[]): string[] {
    return nodes.flatMap((node) => [
      ...(node.selected ? [node.code] : []),
      ...this.collectSelectedCodes(node.children),
    ]);
  }

  private collectCodes(nodes: PermissionTreeNode[]): string[] {
    return nodes.flatMap((node) => [node.code, ...this.collectCodes(node.children)]);
  }

  private expandAll(nodes: PermissionTreeNode[]): void {
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
