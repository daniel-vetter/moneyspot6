import {Component, inject, OnInit, OnDestroy, signal} from '@angular/core';
import {PanelModule} from "primeng/panel";
import {TableModule, TableLazyLoadEvent} from "primeng/table";
import {ProgressSpinnerModule} from "primeng/progressspinner";
import {MailIntegrationClient, ImportedEmailResponse, PagedImportedEmailsResponse} from "../../../server";
import {lastValueFrom} from "rxjs";
import {DatePipe} from "@angular/common";
import {DialogService} from "primeng/dynamicdialog";
import {EmailDetailsDialogComponent} from "./email-details-dialog/email-details-dialog.component";
import {UpdateState} from "../../../common/update-state";

@Component({
    selector: 'app-imported-emails',
    imports: [PanelModule, TableModule, ProgressSpinnerModule, DatePipe],
    templateUrl: './imported-emails.component.html',
    styleUrl: './imported-emails.component.scss',
    providers: [DialogService]
})
export class ImportedEmailsComponent implements OnInit, OnDestroy {
    mailIntegrationClient = inject(MailIntegrationClient);
    dialogService = inject(DialogService);
    private updateState = inject(UpdateState);

    emails = signal<ImportedEmailResponse[] | undefined>(undefined);
    totalRecords = signal<number>(0);
    loading = signal<boolean>(false);
    unprocessedCount = signal<number>(0);

    private statusPollingInterval?: number;

    async ngOnInit(): Promise<void> {
        await this.loadEmails({first: 0, rows: 20});
        await this.loadProcessingStatus();

        this.statusPollingInterval = window.setInterval(() => {
            if (this.updateState.updateInProgress) return;
            this.loadProcessingStatus();
        }, 5000);
    }

    ngOnDestroy(): void {
        if (this.statusPollingInterval) {
            clearInterval(this.statusPollingInterval);
        }
    }

    async loadEmails(event: TableLazyLoadEvent): Promise<void> {
        this.loading.set(true);
        try {
            const page = Math.floor((event.first ?? 0) / (event.rows ?? 20));
            const pageSize = event.rows ?? 20;

            const response = await lastValueFrom(
                this.mailIntegrationClient.getImportedEmails(page, pageSize)
            );

            this.emails.set(response.items);
            this.totalRecords.set(response.totalCount);
        } finally {
            this.loading.set(false);
        }
    }

    async loadProcessingStatus(): Promise<void> {
        const status = await lastValueFrom(this.mailIntegrationClient.getProcessingStatus());
        this.unprocessedCount.set(status.unprocessedEmailCount);
    }

    protected onEmailClicked(email: ImportedEmailResponse): void {
        this.dialogService.open(EmailDetailsDialogComponent, {
            focusOnShow: false,
            modal: true,
            data: {
                emailId: email.id
            }
        });
    }
}
