import {Component, inject, OnInit, OnDestroy, signal} from '@angular/core';
import {PanelModule} from "primeng/panel";
import {TableModule, TableLazyLoadEvent} from "primeng/table";
import {ProgressSpinnerModule} from "primeng/progressspinner";
import {ButtonModule} from "primeng/button";
import {TooltipModule} from "primeng/tooltip";
import {MailIntegrationClient, ImportedEmailResponse} from "../../../server";
import {lastValueFrom} from "rxjs";
import {DatePipe} from "@angular/common";
import {EmailDetailsDialogComponent} from "./email-details-dialog/email-details-dialog.component";
import {UpdateState} from "../../../common/update-state";
import {ModalDialogService} from "../../../common/modal-dialog.service";

@Component({
    selector: 'app-imported-emails',
    imports: [PanelModule, TableModule, ProgressSpinnerModule, ButtonModule, TooltipModule, DatePipe],
    templateUrl: './imported-emails.component.html',
    styleUrl: './imported-emails.component.scss'
})
export class ImportedEmailsComponent implements OnInit, OnDestroy {
    mailIntegrationClient = inject(MailIntegrationClient);
    modalDialogService = inject(ModalDialogService);
    private updateState = inject(UpdateState);

    emails = signal<ImportedEmailResponse[] | undefined>(undefined);
    totalRecords = signal<number>(0);
    loading = signal<boolean>(false);
    unprocessedCount = signal<number>(0);
    failedCount = signal<number>(0);
    retrying = signal<boolean>(false);

    private statusPollingInterval?: number;
    private lastTableEvent: TableLazyLoadEvent = {first: 0, rows: 20};

    async ngOnInit(): Promise<void> {
        await this.loadEmails(this.lastTableEvent);
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
        this.lastTableEvent = event;
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
        this.failedCount.set(status.failedEmailCount);
    }

    protected async onRetryFailedClicked(): Promise<void> {
        this.retrying.set(true);
        try {
            await lastValueFrom(this.mailIntegrationClient.retryFailedEmails());
            await this.loadProcessingStatus();
            await this.loadEmails(this.lastTableEvent);
        } finally {
            this.retrying.set(false);
        }
    }

    protected onEmailClicked(email: ImportedEmailResponse): void {
        this.modalDialogService.open(EmailDetailsDialogComponent, {
            focusOnShow: false,
            data: {
                emailId: email.id
            }
        });
    }
}
