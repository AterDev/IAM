import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiClient } from 'src/app/services/api/api-client';
import { ResourceDetailDto } from 'src/app/services/api/models/access-mod/resource-detail-dto.model';
import { ResourceEditComponent } from './resource-edit';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-resource-detail',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    MatCardModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './resource-detail.html',
  styleUrls: ['./resource-detail.scss']
})
export class ResourceDetailComponent implements OnInit {
  resource = signal<ResourceDetailDto | null>(null);
  isLoading = false;
  resourceId?: string;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiClient,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.resourceId = this.route.snapshot.paramMap.get('id') || '';
    if (this.resourceId) {
      this.loadResource();
    }
  }

  loadResource(): void {
    this.isLoading = true;
    this.api.resources.getDetail(this.resourceId!).subscribe({
      next: (resource) => {
        this.resource.set(resource);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.snackBar.open(
          this.translate.instant('error.loadResourceFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.router.navigate(['/resource']);
      }
    });
  }

  openEditDialog(): void {
    const dialogRef = this.dialog.open(ResourceEditComponent, {
      width: '600px',
      data: { resourceId: this.resourceId }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadResource();
      }
    });
  }

  deleteResource(): void {
    const resource = this.resource();
    if (!resource) {
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant('resource.deleteConfirmTitle'),
        message: this.translate.instant('resource.deleteConfirmDetailMessage', { name: resource.displayName })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.resources.deleteResource(this.resourceId!).subscribe({
          next: () => {
            this.snackBar.open(
              this.translate.instant('resource.deleteSuccess'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
            this.router.navigate(['/resource']);
          },
          error: () => {
            this.snackBar.open(
              this.translate.instant('error.deleteResourceFailed'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
          }
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/resource']);
  }
}
