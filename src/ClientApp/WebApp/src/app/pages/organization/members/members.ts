import { Component, OnInit, Inject, signal } from '@angular/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatTableModule } from '@angular/material/table';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserItemDto } from 'src/app/services/api/models/iammod/user-item-dto.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-members',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatDialogModule,
    MatTableModule,
    MatProgressSpinnerModule,
    FormsModule
  ],
  templateUrl: './members.html',
  styleUrls: ['./members.scss']
})
export class OrganizationMembersComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  displayedColumns: string[] = ['userName', 'email', 'actions'];
  
  // Keep signals for template-reactive values
  members = signal<UserItemDto[]>([]);
  allUsers = signal<UserItemDto[]>([]);
  
  selectedUserId = '';
  isLoading = false;
  isAdding = false;

  constructor(
    private api: ApiClient,
    private dialogRef: MatDialogRef<OrganizationMembersComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) public data: { organizationId: string, organizationName: string }
  ) {}

  ngOnInit(): void {
    // this.loadMembers(); // No getUsers endpoint available
    this.loadAllUsers();
  }

  loadAllUsers(): void {
    this.api.users.getUsers(null, null, null, null, null, null, 1, 100, null).subscribe({
      next: (result) => {
        this.allUsers.set(result.data);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.snackBar.open(this.translate.instant(this.i18n.organization.memberLoadFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
      }
    });
  }

  addMember(): void {
    if (!this.selectedUserId) {
      this.snackBar.open(this.translate.instant(this.i18n.organization.selectMember), this.translate.instant(this.i18n.common.close), { duration: 3000 });
      return;
    }

    this.isAdding = true;
    this.api.organizations.addUsers(this.data.organizationId, [this.selectedUserId]).subscribe({
      next: () => {
        this.snackBar.open(this.translate.instant(this.i18n.organization.memberAdded), this.translate.instant(this.i18n.common.close), { duration: 3000 });
        this.selectedUserId = '';
        this.isAdding = false;
        // Update members list by marking the user as added
        const addedUsers = this.members();
        const userToAdd = this.allUsers().find(u => u.id === this.selectedUserId);
        if (userToAdd) {
          this.members.set([...addedUsers, userToAdd]);
        }
      },
      error: () => {
        this.isAdding = false;
        this.snackBar.open(this.translate.instant(this.i18n.organization.memberAddFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
      }
    });
  }

  removeMember(userId: string): void {
    this.api.organizations.removeUsers(this.data.organizationId, [userId]).subscribe({
      next: () => {
        this.snackBar.open(this.translate.instant(this.i18n.organization.memberRemoved), this.translate.instant(this.i18n.common.close), { duration: 3000 });
        // Update members list
        this.members.set(this.members().filter(m => m.id !== userId));
      },
      error: () => {
        this.snackBar.open(this.translate.instant(this.i18n.organization.memberRemoveFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
      }
    });
  }

  getAvailableUsers(): UserItemDto[] {
    const memberIds = new Set(this.members().map(m => m.id));
    return this.allUsers().filter(u => !memberIds.has(u.id));
  }

  onClose(): void {
    this.dialogRef.close();
  }
}
