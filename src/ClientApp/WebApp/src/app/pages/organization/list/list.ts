import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { MatTreeModule } from '@angular/material/tree';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { ApiClient } from 'src/app/services/api/api-client';
import { NestedTreeControl } from '@angular/cdk/tree';
import { MatTreeNestedDataSource } from '@angular/material/tree';
import { OrganizationAddComponent } from '../add/add';
import { OrganizationEditComponent } from '../edit/edit';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { OrganizationMembersComponent } from '../members/members';
import { OrganizationTreeDto } from 'src/app/services/api/models/iammod/organization-tree-dto.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-list',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    MatTreeModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatCardModule
  ],
  templateUrl: './list.html',
  styleUrls: ['./list.scss']
})
export class OrganizationListComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  treeControl = new NestedTreeControl<OrganizationTreeDto>(node => node.children);
  dataSource = new MatTreeNestedDataSource<OrganizationTreeDto>();
  // Keep signals for template-reactive values
  selectedNode = signal<OrganizationTreeDto | null>(null);

  isLoading = signal(false);

  constructor(
    private api: ApiClient,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private translate: TranslateService
  ) { }

  ngOnInit(): void {
    this.loadTree();
  }

  loadTree(): void {
    this.isLoading.set(true);
    this.api.organizations.getTree(null).subscribe({
      next: (tree) => {
        this.dataSource.data = tree;
        this.isLoading.set(false);
        // Expand root nodes by default
        tree.forEach(node => this.treeControl.expand(node));
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(this.translate.instant(this.i18n.organization.loadFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
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
      this.snackBar.open(this.translate.instant(this.i18n.organization.deleteWithChildren), this.translate.instant(this.i18n.common.close), { duration: 3000 });
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant(this.i18n.organization.deleteConfirmTitle),
        message: this.translate.instant(this.i18n.organization.deleteConfirmMessage, { name: node.name })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.organizations.deleteOrganization(node.id, false).subscribe({
          next: () => {
            this.snackBar.open(this.translate.instant(this.i18n.organization.deletedSuccess), this.translate.instant(this.i18n.common.close), { duration: 3000 });
            this.loadTree();
            if (this.selectedNode()?.id === node.id) {
              this.selectedNode.set(null);
            }
          },
          error: () => {
            this.snackBar.open(this.translate.instant(this.i18n.organization.deleteFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
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
