import { Component, inject, OnInit, signal } from '@angular/core';
import { PanelModule } from "primeng/panel";
import { TableModule, TableLazyLoadEvent } from "primeng/table";
import { ProgressSpinnerModule } from "primeng/progressspinner";
import { MailIntegrationClient, ImportedEmailResponse, PagedImportedEmailsResponse } from "../../../server";
import { lastValueFrom } from "rxjs";
import { DatePipe } from "@angular/common";

@Component({
    selector: 'app-imported-emails',
    imports: [PanelModule, TableModule, ProgressSpinnerModule, DatePipe],
    templateUrl: './imported-emails.component.html',
    styleUrl: './imported-emails.component.scss'
})
export class ImportedEmailsComponent implements OnInit {
    mailIntegrationClient = inject(MailIntegrationClient);

    emails = signal<ImportedEmailResponse[] | undefined>(undefined);
    totalRecords = signal<number>(0);
    loading = signal<boolean>(false);

    async ngOnInit(): Promise<void> {
        await this.loadEmails({ first: 0, rows: 20 });
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
}
