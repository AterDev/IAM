import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface PermissionEntry {
  code: string;
  ownedClientCode: string | null;
  type: number;
  typeLabel: string;
}

@Injectable({
  providedIn: 'root',
})
export class SamplePermissionsService {
  private readonly http = inject(HttpClient);

  readonly permissions = signal<PermissionEntry[]>([]);
  readonly loading = signal(false);
  readonly loaded = signal(false);

  loadForCurrentUser(): void {
    if (this.loading() || this.loaded()) {
      return;
    }

    this.loading.set(true);

    this.http
      .get<Array<{ code: string; type: number; ownedClientCode?: string | null }>>(`${environment.iamApiUrl}/api/Permissions/user-permissions`)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (items) => {
          this.permissions.set(this.mapPermissions(items));
          this.loaded.set(true);
        },
        error: (error) => {
          console.error('加载当前用户权限失败', error);
          this.permissions.set([]);
          this.loaded.set(true);
        },
      });
  }

  reset(): void {
    this.permissions.set([]);
    this.loading.set(false);
    this.loaded.set(false);
  }

  private mapPermissions(items: Array<{ code: string; type: number; ownedClientCode?: string | null }>): PermissionEntry[] {
    return [...items]
      .sort((left, right) => {
        const byClient = (left.ownedClientCode ?? '').localeCompare(right.ownedClientCode ?? '');
        if (byClient !== 0) {
          return byClient;
        }

        if (left.type !== right.type) {
          return left.type - right.type;
        }

        return left.code.localeCompare(right.code);
      })
      .map((item) => ({
        code: item.code,
        ownedClientCode: item.ownedClientCode ?? null,
        type: item.type,
        typeLabel: this.resolveTypeLabel(item.type),
      }));
  }

  private resolveTypeLabel(type: number): string {
    switch (type) {
      case 1:
        return '菜单';
      case 2:
        return '按钮';
      case 3:
        return '业务权限';
      default:
        return '权限';
    }
  }
}
