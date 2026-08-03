import { NestedTreeControl } from '@angular/cdk/tree';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { ApiClient } from 'src/app/services/api/api-client';
import { PermissionType } from 'src/app/services/api/models/entity/permission-type.model';
import { ClientItemDto } from 'src/app/services/api/models/iammod/client-item-dto.model';
import { PermissionTreeNodeDto } from 'src/app/services/api/models/iammod/permission-tree-node-dto.model';
import { RoleDetailDto } from 'src/app/services/api/models/iammod/role-detail-dto.model';
import { CommonModules, CommonFormModules, BaseMatModules } from 'src/app/share/shared-modules';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

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
  readonly i18n = I18N_KEYS;
  readonly treeControl = new NestedTreeControl<PermissionTreeNodeDto>((node) => node.children);
  readonly dataSource = new MatTreeNestedDataSource<PermissionTreeNodeDto>();
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly role = signal<RoleDetailDto | null>(null);
  readonly clients = signal<ClientItemDto[]>([]);
  readonly clientTree = signal<PermissionTreeNodeDto[]>([]);
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
    private readonly api: ApiClient,
    private readonly snackBar: MatSnackBar,
    private readonly translate: TranslateService,
  ) {}

  ngOnInit(): void {
    this.roleId = this.route.snapshot.paramMap.get('id');
    if (!this.roleId) {
      this.router.navigate(['/role/list']);
      return;
    }

    this.loadPage();
  }

  hasChild = (_: number, node: PermissionTreeNodeDto) => !!node.children && node.children.length > 0;

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
    this.api.roles.getPermissionTree(this.roleId, this.clientId, null, null, null, null, null, 1, 2000, null).subscribe({
      next: (tree) => {
        this.clientTree.set(tree);
        this.currentClientCodes.set(new Set(this.collectCodes(tree)));
        this.selectedCodes.set(new Set(this.collectSelectedCodes(tree)));
        this.applyTreeView();
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(this.translate.instant(this.i18n.error.loadPermissionsFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
      },
    });
  }

  isSelected(node: PermissionTreeNodeDto): boolean {
    return this.selectedCodes().has(node.code);
  }

  isIndeterminate(node: PermissionTreeNodeDto): boolean {
    if (node.children.length === 0) {
      return false;
    }

    const descendants = this.collectCodes(node.children);
    const selectedCount = descendants.filter((code) => this.selectedCodes().has(code)).length;
    return selectedCount > 0 && selectedCount < descendants.length;
  }

  toggleNode(node: PermissionTreeNodeDto, checked: boolean): void {
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
    this.api.roles.grantPermissions(this.roleId, { permissionCodes: Array.from(mergedCodes).sort() }).subscribe({
      next: () => {
        this.baselineRoleCodes.set(mergedCodes);
        this.isSaving.set(false);
        this.snackBar.open(this.translate.instant(this.i18n.success.permissionsSaved), this.translate.instant(this.i18n.common.close), { duration: 3000 });
        this.loadTree();
      },
      error: () => {
        this.isSaving.set(false);
        this.snackBar.open(this.translate.instant(this.i18n.error.savePermissionsFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
      },
    });
  }

  goBack(): void {
    if (!this.roleId) {
      this.router.navigate(['/role/list']);
      return;
    }

    this.router.navigate(['/role/detail', this.roleId]);
  }

  clearFilters(): void {
    this.keyword = '';
    this.applyTreeView();
  }

  getNodeLabel(node: Pick<PermissionTreeNodeDto, 'name'>): string {
    return this.translateLabel(node.name);
  }

  private collectSelectedCodes(nodes: PermissionTreeNodeDto[]): string[] {
    return nodes.flatMap((node) => [
      ...(node.selected ? [node.code] : []),
      ...this.collectSelectedCodes(node.children),
    ]);
  }

  private collectCodes(nodes: PermissionTreeNodeDto[]): string[] {
    return nodes.flatMap((node) => [node.code, ...this.collectCodes(node.children)]);
  }

  private expandAll(nodes: PermissionTreeNodeDto[]): void {
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
      roleCodes: this.api.roles.getPermissions(this.roleId),
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
        this.snackBar.open(this.translate.instant(this.i18n.error.loadPermissionsFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
      },
    });
  }

  applyTreeView(): void {
    const filteredTree = this.filterTree(this.clientTree(), this.type, this.keyword.trim().toLocaleLowerCase());
    this.dataSource.data = filteredTree;
    this.treeControl.dataNodes = filteredTree;
    this.expandAll(filteredTree);
  }

  private filterTree(nodes: PermissionTreeNodeDto[], type: PermissionType, keyword: string): PermissionTreeNodeDto[] {
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

  private matchesKeyword(node: PermissionTreeNodeDto, keyword: string): boolean {
    const label = this.translateLabel(node.name).toLocaleLowerCase();
    return label.includes(keyword)
      || node.code.toLocaleLowerCase().includes(keyword)
      || (node.description?.toLocaleLowerCase().includes(keyword) ?? false);
  }

  private translateLabel(label: string): string {
    const translated = this.translate.instant(label);
    return typeof translated === 'string' && translated.trim().length > 0 ? translated : label;
  }
}
