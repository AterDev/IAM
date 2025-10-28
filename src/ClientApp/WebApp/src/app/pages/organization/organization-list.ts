import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { MatTreeModule } from '@angular/material/tree';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { OrganizationsService } from 'src/app/services/api/services/organizations.service';
import { OrganizationTreeDto } from 'src/app/services/api/models/identity-mod/organization-tree-dto.model';
import { NestedTreeControl } from '@angular/cdk/tree';
import { MatTreeNestedDataSource } from '@angular/material/tree';
import { OrganizationAddComponent } from './organization-add';
import { OrganizationEditComponent } from './organization-edit';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { OrganizationMembersComponent } from './organization-members';

@Component({
  selector: 'app-organization-list',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    MatTreeModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatCardModule
  ],
  templateUrl: './organization-list.html',
  styleUrls: ['./organization-list.scss']
})
export class OrganizationListComponent implements OnInit {
  treeControl = new NestedTreeControl<OrganizationTreeDto>(node => node.children);
  dataSource = new MatTreeNestedDataSource<OrganizationTreeDto>();
  isLoading = signal(false);
  selectedNode = signal<OrganizationTreeDto | null>(null);

  constructor(
    private organizationsService: OrganizationsService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadTree();
  }

  loadTree(): void {
    this.isLoading.set(true);
    this.organizationsService.getTree(null).subscribe({
      next: (tree) => {
        this.dataSource.data = tree;
        this.isLoading.set(false);
        // Expand root nodes by default
        tree.forEach(node => this.treeControl.expand(node));
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load organization tree', 'Close', { duration: 3000 });
      }
    });
  }

  hasChild = (_: number, node: OrganizationTreeDto) => !!node.children && node.children.length > 0;

  selectNode(node: OrganizationTreeDto): void {
    this.selectedNode.set(node);
  }

  openAddDialog(parentNode?: OrganizationTreeDto): void {
    const dialogRef = this.dialog.open(OrganizationAddComponent, {
      width: '600px',
      data: { parentId: parentNode?.id || null }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadTree();
        this.selectedNode.set(null);
      }
    });
  }

  openEditDialog(node: OrganizationTreeDto, event: Event): void {
    event.stopPropagation();
    const dialogRef = this.dialog.open(OrganizationEditComponent, {
      width: '600px',
      data: { organizationId: node.id }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadTree();
      }
    });
  }

  deleteNode(node: OrganizationTreeDto, event: Event): void {
    event.stopPropagation();
    
    if (node.children && node.children.length > 0) {
      this.snackBar.open('Cannot delete organization with children', 'Close', { duration: 3000 });
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Organization',
        message: `Are you sure you want to delete organization "${node.name}"?`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.organizationsService.deleteOrganization(node.id, false).subscribe({
          next: () => {
            this.snackBar.open('Organization deleted successfully', 'Close', { duration: 3000 });
            this.loadTree();
            if (this.selectedNode()?.id === node.id) {
              this.selectedNode.set(null);
            }
          },
          error: () => {
            this.snackBar.open('Failed to delete organization', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }

  openMembersDialog(node: OrganizationTreeDto, event: Event): void {
    event.stopPropagation();
    const dialogRef = this.dialog.open(OrganizationMembersComponent, {
      width: '800px',
      data: { organizationId: node.id, organizationName: node.name }
    });

    dialogRef.afterClosed().subscribe(() => {
      // No need to reload tree when members change
    });
  }
}
