import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { AuthService } from '../../services/auth.service';
import { ApiClient } from 'src/app/services/api/api-client';
import { PermissionType } from 'src/app/services/api/models/entity/permission-type.model';

@Component({
  selector: 'app-navigation',
  imports: [...BaseMatModules, ...CommonModules, MatSidenavModule, MatExpansionModule, MatListModule],
  templateUrl: './navigation.html',
  styleUrl: './navigation.scss'
})
export class NavigationComponent {
  private readonly http = inject(HttpClient);
  opened = true;
  expanded = true;
  menus = signal<Menu[]>([]);
  constructor(
    private readonly authService: AuthService,
    private readonly api: ApiClient,
  ) {
  }
  ngOnInit(): void {
    this.updateMenus();
  }

  toggle(): void {
    this.opened = !this.opened;
  }

  updateMenus(): void {
    forkJoin({
      menuConfig: this.http.get<Menu[]>('assets/menus.json'),
      permissions: this.api.permissions.getUserPermissions(),
    })
      .subscribe({
        next: ({ menuConfig, permissions }) => {
          const allowedCodes = new Set(
            permissions
              .filter((item) => item.type === PermissionType.Menu && item.ownedClientCode === this.authService.getOidcClientId())
              .map((item) => item.code),
          );
          this.menus.set(this.filterMenusByPermissions(menuConfig, allowedCodes));
        },
        error: () => {
          this.menus.set([]);
        }
      });
  }

  private filterMenusByPermissions(nodes: Menu[], allowedCodes: Set<string>): Menu[] {
    return nodes
      .map((node) => ({
        ...node,
        children: this.filterMenusByPermissions(node.children ?? [], allowedCodes),
      }))
      .filter((node) => allowedCodes.has(node.accessCode) || (node.children?.length ?? 0) > 0)
      .sort((left, right) => left.sort - right.sort);
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
