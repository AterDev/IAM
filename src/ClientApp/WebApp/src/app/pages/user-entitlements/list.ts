import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatDialog } from '@angular/material/dialog';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserItemDto } from 'src/app/services/api/models/iammod/user-item-dto.model';
import { UserEntitlementDefinitionItemDto } from 'src/app/services/api/models/user-center-mod/user-entitlement-definition-item-dto.model';
import { UserEntitlementDetailDto } from 'src/app/services/api/models/user-center-mod/user-entitlement-detail-dto.model';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';
import { debounceTime, distinctUntilChanged, startWith, switchMap, tap } from 'rxjs';
import { UserEntitlementAddComponent } from './add/add';

@Component({
  selector: 'app-user-entitlement-list',
  imports: [...CommonModules, ...BaseMatModules, ReactiveFormsModule, MatAutocompleteModule, MatTableModule, MatFormFieldModule, MatInputModule],
  templateUrl: './list.html',
  styleUrl: './list.scss'
})
export class UserEntitlementListComponent implements OnInit {
  private readonly api = inject(ApiClient);
  private readonly dialog = inject(MatDialog);
  readonly users = signal<UserItemDto[]>([]);
  readonly definitions = signal<UserEntitlementDefinitionItemDto[]>([]);
  readonly entitlements = signal<UserEntitlementDetailDto[]>([]);
  readonly selectedUser = signal<UserItemDto | null>(null);
  readonly columns = ['code', 'limit', 'used', 'expiration', 'actions'];
  readonly userControl = new FormControl<string | UserItemDto>('');

  ngOnInit(): void {
    this.loadDefinitions();
    this.userControl.valueChanges.pipe(
      startWith(''),
      tap(value => {
        if (typeof value === 'string') {
          this.selectedUser.set(null);
          this.entitlements.set([]);
        }
      }),
      debounceTime(250),
      distinctUntilChanged(),
      switchMap(value => this.api.users.getUsers(typeof value === 'string' && value.trim() ? value.trim() : null, null, null, null, null, null, 1, 20, null)),
    ).subscribe(page => this.users.set(page.data));
  }

  displayUser(user: UserItemDto | string | null): string {
    return typeof user === 'object' && user ? `${user.userName} · ${user.email ?? ''}` : user ?? '';
  }

  selectUser(user: UserItemDto): void {
    this.selectedUser.set(user);
    this.load();
  }

  load(): void {
    const user = this.selectedUser();
    if (user) {
      this.api.userEntitlements.getPage(user.id, 1, 100, null).subscribe(page => this.entitlements.set(page.data));
    }
  }

  loadDefinitions(): void { this.api.userEntitlementDefinitions.getPage(null, 1, 100, null).subscribe(page => this.definitions.set(page.data)); }
  availableDefinitions(): UserEntitlementDefinitionItemDto[] { const assigned = new Set(this.entitlements().map(item => item.entitlementDefinitionId)); return this.definitions().filter(item => !assigned.has(item.id)); }

  add(): void {
    const user = this.selectedUser();
    if (!user) return;

    this.dialog.open(UserEntitlementAddComponent, {
      width: '600px',
      data: { userId: user.id, definitions: this.availableDefinitions() },
    }).afterClosed().subscribe(saved => {
      if (saved) this.load();
    });
  }

  delete(id: string): void { this.api.userEntitlements.delete(id).subscribe(() => this.load()); }
}
