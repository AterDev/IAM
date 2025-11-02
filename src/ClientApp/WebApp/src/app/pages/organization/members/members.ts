import { Component, OnInit, Inject, signal } from '@angular/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatTableModule } from '@angular/material/table';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserItemDto } from 'src/app/services/api/models/identity-mod/user-item-dto.model';

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
        this.snackBar.open('Failed to load users', 'Close', { duration: 3000 });
      }
    });
  }

  addMember(): void {
    if (!this.selectedUserId) {
      this.snackBar.open('Please select a user', 'Close', { duration: 3000 });
      return;
    }

    this.isAdding = true;
    this.api.organizations.addUsers(this.data.organizationId, [this.selectedUserId]).subscribe({
      next: () => {
        this.snackBar.open('Member added successfully', 'Close', { duration: 3000 });
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
        this.snackBar.open('Failed to add member', 'Close', { duration: 3000 });
      }
    });
  }

  removeMember(userId: string): void {
    this.api.organizations.removeUsers(this.data.organizationId, [userId]).subscribe({
      next: () => {
        this.snackBar.open('Member removed successfully', 'Close', { duration: 3000 });
        // Update members list
        this.members.set(this.members().filter(m => m.id !== userId));
      },
      error: () => {
        this.snackBar.open('Failed to remove member', 'Close', { duration: 3000 });
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
