import { Component, OnInit, Inject, signal } from '@angular/core';
import { CommonModules, CommonFormModules, BaseMatModules } from 'src/app/share/shared-modules';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { ApiClient } from 'src/app/services/api/api-client';
import { RoleItemDto } from 'src/app/services/api/models/identity-mod/role-item-dto.model';
import { PermissionClaim } from 'src/app/services/api/models/identity-mod/permission-claim.model';
import { RoleGrantPermissionDto } from 'src/app/services/api/models/identity-mod/role-grant-permission-dto.model';

interface PermissionGroup {
  category: string;
  permissions: PermissionClaim[];
  allSelected: boolean;
  someSelected: boolean;
}

@Component({
  selector: 'app-permissions',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    ...BaseMatModules,
    MatDialogModule,
    MatCheckboxModule,
    MatExpansionModule,
    MatChipsModule,
    FormsModule
  ],
  templateUrl: './permissions.html',
  styleUrls: ['./permissions.scss']
})
export class RolePermissionsComponent implements OnInit {
  isLoading = true;
  isSaving = false;
  searchText = '';
  
  currentPermissions = signal<PermissionClaim[]>([]);
  selectedPermissions = signal<Set<string>>(new Set());
  permissionGroups = signal<PermissionGroup[]>([]);

  // Common permission categories with their permissions
  private readonly commonPermissions: { [key: string]: string[] } = {
    'users': ['read', 'create', 'update', 'delete', 'manage'],
    'roles': ['read', 'create', 'update', 'delete', 'assign'],
    'organizations': ['read', 'create', 'update', 'delete', 'manage-members'],
    'clients': ['read', 'create', 'update', 'delete', 'manage-secrets'],
    'scopes': ['read', 'create', 'update', 'delete'],
    'audit': ['read', 'export'],
    'system': ['read', 'configure', 'manage']
  };

  constructor(
    private api: ApiClient,
    private dialogRef: MatDialogRef<RolePermissionsComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) public data: RoleItemDto
  ) {}

  ngOnInit(): void {
    this.loadPermissions();
  }

  loadPermissions(): void {
    this.isLoading = true;
    
    this.api.roles.getPermissions(this.data.id).subscribe({
      next: (permissions) => {
        this.currentPermissions.set(permissions);
        
        // Initialize selected permissions
        const selected = new Set<string>();
        permissions.forEach(p => {
          selected.add(`${p.claimType}:${p.claimValue}`);
        });
        this.selectedPermissions.set(selected);
        
        // Build permission groups
        this.buildPermissionGroups();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load permissions:', error);
        this.snackBar.open(
          this.translate.instant('error.loadPermissionsFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.isLoading = false;
      }
    });
  }

  buildPermissionGroups(): void {
    const groups: PermissionGroup[] = [];
    const selected = this.selectedPermissions();
    
    Object.keys(this.commonPermissions).forEach(category => {
      const permissions: PermissionClaim[] = this.commonPermissions[category].map(value => ({
        claimType: 'permissions',
        claimValue: `${category}.${value}`
      }));
      
      const selectedCount = permissions.filter(p => 
        selected.has(`${p.claimType}:${p.claimValue}`)
      ).length;
      
      groups.push({
        category,
        permissions,
        allSelected: selectedCount === permissions.length,
        someSelected: selectedCount > 0 && selectedCount < permissions.length
      });
    });
    
    this.permissionGroups.set(groups);
  }

  togglePermission(permission: PermissionClaim): void {
    const key = `${permission.claimType}:${permission.claimValue}`;
    const selected = new Set(this.selectedPermissions());
    
    if (selected.has(key)) {
      selected.delete(key);
    } else {
      selected.add(key);
    }
    
    this.selectedPermissions.set(selected);
    this.buildPermissionGroups();
  }

  isPermissionSelected(permission: PermissionClaim): boolean {
    const key = `${permission.claimType}:${permission.claimValue}`;
    return this.selectedPermissions().has(key);
  }

  toggleGroupPermissions(group: PermissionGroup): void {
    const selected = new Set(this.selectedPermissions());
    
    if (group.allSelected) {
      // Deselect all in group
      group.permissions.forEach(p => {
        const key = `${p.claimType}:${p.claimValue}`;
        selected.delete(key);
      });
    } else {
      // Select all in group
      group.permissions.forEach(p => {
        const key = `${p.claimType}:${p.claimValue}`;
        selected.add(key);
      });
    }
    
    this.selectedPermissions.set(selected);
    this.buildPermissionGroups();
  }

  getFilteredGroups(): PermissionGroup[] {
    const groups = this.permissionGroups();
    if (!this.searchText) {
      return groups;
    }
    
    const searchLower = this.searchText.toLowerCase();
    return groups.filter(group => 
      group.category.toLowerCase().includes(searchLower) ||
      group.permissions.some(p => p.claimValue.toLowerCase().includes(searchLower))
    );
  }

  getPermissionLabel(value: string): string {
    // Extract the action from permission value (e.g., "users.read" -> "read")
    const parts = value.split('.');
    return parts[parts.length - 1];
  }

  getCategoryLabel(category: string): string {
    return this.translate.instant(`permission.category.${category}`) || category;
  }

  onSave(): void {
    if (this.isSaving) {
      return;
    }
    
    this.isSaving = true;
    
    const permissions: PermissionClaim[] = [];
    this.selectedPermissions().forEach(key => {
      const [claimType, claimValue] = key.split(':');
      permissions.push({ claimType, claimValue });
    });
    
    const data: RoleGrantPermissionDto = {
      permissions
    };
    
    this.api.roles.grantPermissions(this.data.id, data).subscribe({
      next: () => {
        this.snackBar.open(
          this.translate.instant('success.permissionsSaved'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.dialogRef.close(true);
      },
      error: (error) => {
        console.error('Failed to save permissions:', error);
        this.snackBar.open(
          this.translate.instant('error.savePermissionsFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.isSaving = false;
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
