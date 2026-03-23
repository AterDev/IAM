import { NestedTreeControl } from '@angular/cdk/tree';
import { Component, OnInit, computed, signal } from '@angular/core';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatTabsModule } from '@angular/material/tabs';
import { CommonModules, CommonFormModules, BaseMatModules } from 'src/app/share/shared-modules';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiClient } from 'src/app/services/api/api-client';
import { ClientItemDto } from 'src/app/services/api/models/iammod/client-item-dto.model';
import { RoleDetailDto } from 'src/app/services/api/models/iammod/role-detail-dto.model';
import { PermissionAdminService } from 'src/app/services/permission-admin.service';
import { PermissionTreeNode, PermissionType } from 'src/app/services/permission-admin.models';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';

@Component({
  selector: 'app-permissions',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    ...BaseMatModules,
    MatCheckboxModule,
    MatTreeModule,
    MatCardModule,
    MatListModule,
    MatTabsModule,
    FormsModule,
    AppLoadingComponent,
  ],
  templateUrl: './permissions.html',
  styleUrls: ['./permissions.scss'],
})
export class RolePermissionsComponent implements OnInit {
  readonly treeControl = new NestedTreeControl<PermissionTreeNode>((node) => node.children);
  readonly dataSource = new MatTreeNestedDataSource<PermissionTreeNode>();
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly role = signal<RoleDetailDto | null>(null);
  readonly clients = signal<ClientItemDto[]>([]);
  readonly clientTree = signal<PermissionTreeNode[]>([]);
  readonly currentClientCodes = signal<Set<string>>(new Set());
  readonly baselineRoleCodes = signal<Set<string>>(new Set());
  readonly selectedClient = computed(() => this.clients().find((client) => client.id === this.clientId) ?? null);
  readonly selectedCodes = signal<Set<string>>(new Set());
  readonly typeTabs = [
    { labelKey: 'permission.typeOptions.menu', value: PermissionType.Menu },
    { labelKey: 'permission.typeOptions.button', value: PermissionType.Button },
    { labelKey: 'permission.typeOptions.business', value: PermissionType.Business },
  ];

  clientId: string | null = null;
  type = PermissionType.Menu;
  keyword = '';
  activeTabIndex = 0;
  private roleId: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly permissionAdminService: PermissionAdminService,
    private readonly api: ApiClient,
    private readonly snackBar: MatSnackBar,
    private readonly translate: TranslateService,
  ) {}

  ngOnInit(): void {
    this.roleId = this.route.snapshot.paramMap.get('id');
    if (!this.roleId) {
      this.router.navigate(['/role']);
      return;
    }

    this.loadPage();
  }

  hasChild = (_: number, node: PermissionTreeNode) => !!node.children && node.children.length > 0;

  loadTree(): void {
    if (!this.clientId || !this.roleId) {
      this.clientTree.set([]);
      this.currentClientCodes.set(new Set());
      this.selectedCodes.set(new Set());
      this.dataSource.data = [];
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.permissionAdminService.getRolePermissionTree(this.roleId, {
      clientId: this.clientId,
      pageIndex: 1,
      pageSize: 2000,
    }).subscribe({
      next: (tree) => {
        this.clientTree.set(tree);
        this.currentClientCodes.set(new Set(this.collectCodes(tree)));
        this.selectedCodes.set(new Set(this.collectSelectedCodes(tree)));
        this.applyTreeView();
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

  selectClient(client: ClientItemDto): void {
    if (this.clientId === client.id) {
      return;
    }

    this.clientId = client.id;
    this.keyword = '';
    this.activeTabIndex = 0;
    this.type = PermissionType.Menu;
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
    this.applyTreeView();
  }

  trackByClientId(_: number, client: ClientItemDto): string {
    return client.id;
  }

  save(): void {
    if (this.isSaving() || !this.roleId) {
      return;
    }

    const mergedCodes = new Set(this.baselineRoleCodes());
    this.currentClientCodes().forEach((code) => mergedCodes.delete(code));
    this.selectedCodes().forEach((code) => mergedCodes.add(code));

    this.isSaving.set(true);
    this.permissionAdminService.grantRolePermissions(this.roleId, Array.from(mergedCodes).sort()).subscribe({
      next: () => {
        this.baselineRoleCodes.set(mergedCodes);
        this.isSaving.set(false);
        this.snackBar.open(this.translate.instant('success.permissionsSaved'), this.translate.instant('common.close'), { duration: 3000 });
        this.loadTree();
      },
      error: () => {
        this.isSaving.set(false);
        this.snackBar.open(this.translate.instant('error.savePermissionsFailed'), this.translate.instant('common.close'), { duration: 3000 });
      },
    });
  }

  goBack(): void {
    if (!this.roleId) {
      this.router.navigate(['/role']);
      return;
    }

    this.router.navigate(['/role', this.roleId]);
  }

  clearFilters(): void {
    this.keyword = '';
    this.applyTreeView();
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

  private loadPage(): void {
    if (!this.roleId) {
      return;
    }

    this.isLoading.set(true);
    forkJoin({
      role: this.api.roles.getDetail(this.roleId),
      clients: this.api.clients.getClients(null, null, null, null, 1, 200, null),
      roleCodes: this.permissionAdminService.getRolePermissionCodes(this.roleId),
    }).subscribe({
      next: ({ role, clients, roleCodes }) => {
        this.role.set(role);
        this.clients.set(clients.data);
        this.baselineRoleCodes.set(new Set(roleCodes));

        if (!this.clientId && clients.data.length > 0) {
          this.clientId = clients.data[0].id;
        }

        this.loadTree();
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(this.translate.instant('error.loadPermissionsFailed'), this.translate.instant('common.close'), { duration: 3000 });
      },
    });
  }

  applyTreeView(): void {
    const filteredTree = this.filterTree(this.clientTree(), this.type, this.keyword.trim().toLocaleLowerCase());
    this.dataSource.data = filteredTree;
    this.treeControl.dataNodes = filteredTree;
    this.expandAll(filteredTree);
  }

  private filterTree(nodes: PermissionTreeNode[], type: PermissionType, keyword: string): PermissionTreeNode[] {
    return nodes.flatMap((node) => {
      const children = this.filterTree(node.children, type, keyword);
      const matchesType = node.type === type;
      const matchesKeyword = !keyword || this.matchesKeyword(node, keyword);

      if ((matchesType && matchesKeyword) || children.length > 0) {
        return [{ ...node, children }];
      }

      return [];
    });
  }

  private matchesKeyword(node: PermissionTreeNode, keyword: string): boolean {
    const label = this.translateLabel(node.displayName || node.name).toLocaleLowerCase();
    return label.includes(keyword)
      || node.code.toLocaleLowerCase().includes(keyword)
      || (node.description?.toLocaleLowerCase().includes(keyword) ?? false);
  }

  private translateLabel(label: string): string {
    const translated = this.translate.instant(label);
    return typeof translated === 'string' && translated.trim().length > 0 ? translated : label;
  }
}
