import { Component, OnInit } from '@angular/core';
import { PanelModule } from "primeng/panel";
import { PrimeTemplate } from "primeng/api";
import { Ripple } from "primeng/ripple";
import { ValueComponent } from "../common/value/value.component";
import { StockTransactionResponse, StockTransactionsPageClient } from "../server";
import { firstValueFrom, lastValueFrom } from "rxjs";
import { CustomDateTimePipe } from "../common/custom-datetime.pipe";
import { ButtonModule } from "primeng/button";
import { DialogService } from "primeng/dynamicdialog";
import { StockTransactionEditDialogComponent } from "./stock-transaction-edit-dialog/stock-transaction-edit-dialog.component";
import { DecimalPipe } from '@angular/common';
import { CustomDatePipe } from "../common/custom-date.pipe";

@Component({
    selector: 'app-stock-transactions',
    imports: [
        PanelModule,
        PrimeTemplate,
        Ripple,
        ValueComponent,
        ButtonModule,
        DecimalPipe,
        CustomDatePipe
    ],
    providers: [DialogService],
    templateUrl: './stock-transactions.component.html',
    styleUrl: './stock-transactions.component.scss'
})
export class StockTransactionsComponent implements OnInit {
    transactions?: StockTransactionResponse[];

    constructor(private stockTransactionsPageClient: StockTransactionsPageClient, private dialogService: DialogService) {
    }

    async ngOnInit(): Promise<void> {
        await this.update();
    }

    async onNewClicked() {
        const dlg = this.dialogService.open(StockTransactionEditDialogComponent, {
            data: {
                id: undefined
            }
        });

        await firstValueFrom(dlg.onClose);
        this.update();
    }
    async update() {
        this.transactions = await lastValueFrom(this.stockTransactionsPageClient.getStockTransactions());
    }

    async onTransactionClicked(transaction: StockTransactionResponse) {
        const dlg = this.dialogService.open(StockTransactionEditDialogComponent, {
            data: {
                id: transaction.id
            }
        });

        await firstValueFrom(dlg.onClose);
        this.update();
    }
}
