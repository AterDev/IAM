import { Component, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { BreadcrumbComponent } from '../../share/components/breadcrumb/breadcrumb';
import { AuthService } from '../../services/auth.service';

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
    private http: HttpClient,
    private authService: AuthService,
  ) {
  }
  ngOnInit(): void {
    this.updateMenus();
  }

  toggle(): void {
    this.opened = !this.opened;
  }

  updateMenus(): void {
    this.http.get<Menu[]>('/assets/menus.json?_t=' + Date.now(), { responseType: 'json' })
      .subscribe({
        next: (res) => {
          const sortedMenus = res.sort((a, b) => a.sort - b.sort);
          const userMenuCodes = this.authService.getAccessibleMenuCodes();
          const filteredMenus = userMenuCodes.length > 0
            ? this.mergeMenu(userMenuCodes, sortedMenus)
            : [];

          this.menus.set(filteredMenus);
        }
      });
  }
  mergeMenu(userMenuCodes: string[], menus: Menu[]): Menu[] {
    // 只保留有权限的菜单
    return menus.filter((item) => {
      const hasDirectAccess = userMenuCodes.includes(item.accessCode);

      if (item.children) {
        item.children = this.mergeMenu(userMenuCodes, item.children);
      }

      if (hasDirectAccess || (item.children?.length ?? 0) > 0) {
        if (item.children) {
          item.children = [...item.children];
        }
        return true;
      }
      return false;
    });
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
