import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { TranslateService } from '@ngx-translate/core';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserEntitlementDefinitionItemDto } from 'src/app/services/api/models/user-center-mod/user-entitlement-definition-item-dto.model';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { BaseMatModules, CommonFormModules, CommonModules } from 'src/app/share/shared-modules';
import { EntitlementDefinitionAddComponent } from './add/add';
import { EntitlementDefinitionEditComponent } from './edit/edit';

@Component({
  selector: 'app-entitlement-definition-list',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
  ],
  templateUrl: './list.html',
})
export class EntitlementDefinitionListComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  private readonly api = inject(ApiClient);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);

  readonly data = signal<UserEntitlementDefinitionItemDto[]>([]);
  readonly total = signal(0);
  readonly columns = ['entitlementCode', 'displayName', 'unit', 'actions'];
  keyword = '';
  pageIndex = 0;
  pageSize = 10;
  loading = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.userEntitlementDefinitions
      .getPage(this.keyword || null, this.pageIndex + 1, this.pageSize, null)
      .subscribe({
        next: (page) => {
          this.data.set(page.data);
          this.total.set(page.count);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  search(): void {
    this.pageIndex = 0;
    this.load();
  }

  page(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.load();
  }

  add(): void {
    this.dialog
      .open(EntitlementDefinitionAddComponent, { width: '600px' })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) this.load();
      });
  }

  edit(item: UserEntitlementDefinitionItemDto): void {
    this.dialog
      .open(EntitlementDefinitionEditComponent, { width: '600px', data: item })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) this.load();
      });
  }

  remove(item: UserEntitlementDefinitionItemDto): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        width: '400px',
        data: {
          title: this.translate.instant(this.i18n.dialog.confirmDelete.title),
          message: `${this.translate.instant(this.i18n.dialog.confirmDelete.message)} ${item.displayName}`,
        },
      })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) return;

        this.api.userEntitlementDefinitions.delete(item.id).subscribe({
          next: () => {
            this.snackBar.open(
              this.translate.instant(this.i18n.common.delete),
              this.translate.instant(this.i18n.common.close),
              { duration: 3000 },
            );
            this.load();
          },
        });
      });
  }
}
