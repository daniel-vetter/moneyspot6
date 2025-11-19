import { Component, inject, OnInit, signal } from '@angular/core';
import { DynamicDialogConfig, DynamicDialogRef } from "primeng/dynamicdialog";
import { ImportedEmailDetailsResponse, MailIntegrationClient } from "../../../../server";
import { lastValueFrom } from "rxjs";
import { ProgressSpinnerModule } from "primeng/progressspinner";
import { DatePipe, DecimalPipe } from "@angular/common";
import { ButtonModule } from "primeng/button";

@Component({
    selector: 'app-email-details-dialog',
    imports: [ProgressSpinnerModule, DatePipe, DecimalPipe, ButtonModule],
    templateUrl: './email-details-dialog.component.html',
    styleUrl: './email-details-dialog.component.scss'
})
export class EmailDetailsDialogComponent implements OnInit {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    private mailIntegrationClient = inject(MailIntegrationClient);

    emailId: number;
    emailDetails = signal<ImportedEmailDetailsResponse | undefined>(undefined);
    extractedData = signal<any | undefined>(undefined);

    constructor() {
        this.emailId = this.dialogConfig.data?.emailId;
        this.dialogConfig.width = "900px";
        this.dialogConfig.header = "E-Mail Details";
    }

    async ngOnInit(): Promise<void> {
        try {
            const details = await lastValueFrom(
                this.mailIntegrationClient.getImportedEmailDetails(this.emailId)
            );
            this.emailDetails.set(details);

            if (details.processedData) {
                try {
                    this.extractedData.set(JSON.parse(details.processedData));
                } catch (e) {
                    console.error('Failed to parse processed data', e);
                }
            }
        } catch (error) {
            console.error('Failed to load email details', error);
        }
    }

    protected onCloseClicked() {
        this.dialogRef.close();
    }
}
