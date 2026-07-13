import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserItemDto } from 'src/app/services/api/models/iammod/user-item-dto.model';
import { UserEntitlementDefinitionItemDto } from 'src/app/services/api/models/user-center-mod/user-entitlement-definition-item-dto.model';
import { UserEntitlementDetailDto } from 'src/app/services/api/models/user-center-mod/user-entitlement-detail-dto.model';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';

@Component({
  selector: 'app-user-entitlement-list',
  imports: [...CommonModules, ...BaseMatModules, ReactiveFormsModule, MatTableModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './list.html',
  styleUrl: './list.scss'
})
export class UserEntitlementListComponent {
  private readonly api = inject(ApiClient);
  private readonly formBuilder = inject(FormBuilder);
  readonly users = signal<UserItemDto[]>([]);
  readonly definitions = signal<UserEntitlementDefinitionItemDto[]>([]);
  readonly entitlements = signal<UserEntitlementDetailDto[]>([]);
  readonly columns = ['code', 'limit', 'used', 'expiration', 'actions'];
  readonly userForm = this.formBuilder.nonNullable.group({ keyword: [''], userId: ['', Validators.required] });
  readonly form = this.formBuilder.nonNullable.group({ entitlementDefinitionId: ['', Validators.required], valueLimit: [0, [Validators.required, Validators.min(0)]], expirationDate: [''], startDate: [new Date().toISOString().slice(0, 16), Validators.required] });

  constructor() { this.loadDefinitions(); }
  searchUsers(): void { this.api.users.getUsers(this.userForm.controls.keyword.value || null, null, null, null, null, null, 1, 20, null).subscribe(page => this.users.set(page.data)); }
  selectUser(id: string): void { this.userForm.controls.userId.setValue(id); this.load(); }
  load(): void { const id = this.userForm.controls.userId.value; if (id) this.api.userEntitlements.getPage(id, 1, 100, null).subscribe(page => this.entitlements.set(page.data)); }
  loadDefinitions(): void { this.api.userEntitlementDefinitions.getPage(null, 1, 100, null).subscribe(page => this.definitions.set(page.data)); }
  availableDefinitions(): UserEntitlementDefinitionItemDto[] { const assigned = new Set(this.entitlements().map(item => item.entitlementDefinitionId)); return this.definitions().filter(item => !assigned.has(item.id)); }
  add(): void { if (this.form.invalid || !this.userForm.controls.userId.value) return; const value = this.form.getRawValue(); this.api.userEntitlements.create(this.userForm.controls.userId.value, { entitlementDefinitionId: value.entitlementDefinitionId, valueLimit: value.valueLimit, expirationDate: value.expirationDate ? new Date(value.expirationDate) : null, startDate: new Date(value.startDate) }).subscribe(() => { this.form.reset({ entitlementDefinitionId: '', valueLimit: 0, expirationDate: '', startDate: new Date().toISOString().slice(0, 16) }); this.load(); }); }
  delete(id: string): void { this.api.userEntitlements.delete(id).subscribe(() => this.load()); }
}
