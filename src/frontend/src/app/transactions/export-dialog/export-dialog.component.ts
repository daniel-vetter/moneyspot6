import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { lastValueFrom } from 'rxjs';
import { TransactionPageClient } from '../../server';

@Component({
    selector: 'app-export-dialog',
    imports: [FormsModule, ButtonModule, CheckboxModule],
    templateUrl: './export-dialog.component.html',
    styleUrl: './export-dialog.component.scss',
    standalone: true
})
export class ExportDialogComponent {
    private dynamicDialogRef = inject(DynamicDialogRef);
    private transactionPageClient = inject(TransactionPageClient);

    includeFinal = true;
    includeRaw = false;
    isExporting = false;

    constructor() {
        const dialogConfig = inject(DynamicDialogConfig);

        dialogConfig.modal = true;
        dialogConfig.width = '500px';
        dialogConfig.header = 'Daten exportieren';
    }

    isValid(): boolean {
        return this.includeFinal || this.includeRaw;
    }

    onCancelClicked() {
        this.dynamicDialogRef.close();
    }

    async onExportClicked() {
        this.isExporting = true;
        try {
            const response = await lastValueFrom(this.transactionPageClient.export(this.includeRaw, this.includeFinal));
            const url = URL.createObjectURL(response.data);
            const anchor = document.createElement('a');
            anchor.href = url;
            anchor.download = response.fileName ?? 'MoneySpot-Transaktionen.csv';
            anchor.click();
            URL.revokeObjectURL(url);
            this.dynamicDialogRef.close();
        } finally {
            this.isExporting = false;
        }
    }
}
