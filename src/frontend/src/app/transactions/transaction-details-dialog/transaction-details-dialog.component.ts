import { Component, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { TabsModule } from 'primeng/tabs';
import { TransactionDetailsTabComponent } from './transaction-details-tab/transaction-details-tab.component';
import { TransactionRawDataTabComponent } from './transaction-raw-data-tab/transaction-raw-data-tab.component';

@Component({
    selector: 'app-transaction-details-dialog',
    imports: [ButtonModule, TabsModule, TransactionDetailsTabComponent, TransactionRawDataTabComponent],
    templateUrl: './transaction-details-dialog.component.html',
    styleUrl: './transaction-details-dialog.component.scss'
})
export class TransactionDetailsDialogComponent {
    private dynamicDialogRef = inject(DynamicDialogRef);

    id: number;

    constructor() {
        const dialogConfig = inject(DynamicDialogConfig);

        this.id = dialogConfig.data.id;
        dialogConfig.modal = true;
        dialogConfig.width = "1100px";
        dialogConfig.header = "Buchungsdetails";
    }

    onCancelClicked() {
        this.dynamicDialogRef.close();
    }

    onSaved() {
        this.dynamicDialogRef.close(true);
    }
}
