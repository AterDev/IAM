import { Component, signal } from '@angular/core';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { TranslateService } from '@ngx-translate/core';
import { BreadcrumbComponent } from '../../share/components/breadcrumb/breadcrumb';
import { AuthService } from '../../services/auth.service';
import { PermissionAdminService } from '../../services/permission-admin.service';
import { PermissionTreeNode, PermissionType } from '../../services/permission-admin.models';

@Component({
  selector: 'app-navigation',
  imports: [...BaseMatModules, ...CommonModules, MatSidenavModule, MatExpansionModule, MatListModule],
  templateUrl: './navigation.html',
  styleUrl: './navigation.scss'
})
export class NavigationComponent {
  events: string[] = [];
  opened = true;
  expanded = true;
  menus = signal<Menu[]>([]);
  constructor(
    private readonly authService: AuthService,
    private readonly permissionAdminService: PermissionAdminService,
    private readonly translate: TranslateService,
  ) {
  }
  ngOnInit(): void {
    this.updateMenus();
  }

  toggle(): void {
    this.opened = !this.opened;
  }

  updateMenus(): void {
    this.permissionAdminService.getMyMenuTree(this.authService.getOidcClientId())
      .subscribe({
        next: (res) => {
          this.menus.set(this.mapMenus(res));
        },
        error: () => {
          this.menus.set([]);
        }
      });
  }

  private mapMenus(nodes: PermissionTreeNode[]): Menu[] {
    return nodes
      .filter((node) => node.type === PermissionType.Menu)
      .sort((left, right) => left.sort - right.sort)
      .map((node) => ({
        name: this.translateMenuLabel(node.displayName || node.name),
        path: node.path || null,
        accessCode: node.code,
        icon: node.icon || 'menu',
        sort: node.sort,
        menuType: 0,
        children: this.mapMenus(node.children),
      }));
  }

  private translateMenuLabel(label: string): string {
    const translated = this.translate.instant(label);
    return typeof translated === 'string' && translated.trim().length > 0 ? translated : label;
  }
}
export interface Menu {
  name: string,
  path: string | null,
  accessCode: string,
  icon: string,
  sort: number,
  menuType: 0 | 1,
  children?: Menu[] | null,
}
