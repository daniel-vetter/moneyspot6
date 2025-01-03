import {Component, OnInit} from '@angular/core';
import {PanelModule} from "primeng/panel";
import {PrimeTemplate} from "primeng/api";
import {Ripple} from "primeng/ripple";
import {ValueComponent} from "../common/value/value.component";
import {StockTransactionResponse, StockTransactionsPageClient} from "../server";
import {lastValueFrom} from "rxjs";
import {CustomDateTimePipe} from "../common/custom-datetime.pipe";
import {ButtonModule} from "primeng/button";
import {DialogService} from "primeng/dynamicdialog";
import {StockTransactionEditDialogComponent} from "./stock-transaction-edit-dialog/stock-transaction-edit-dialog.component";

@Component({
    selector: 'app-stock-transactions',
    imports: [
        CustomDateTimePipe,
        PanelModule,
        PrimeTemplate,
        Ripple,
        ValueComponent,
        ButtonModule
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
        this.transactions = await lastValueFrom(this.stockTransactionsPageClient.getStockTransactions());
    }

    onNewClicked() {
        this.dialogService.open(StockTransactionEditDialogComponent, {
            header: "Neue Transaktion",
            width: "500px",
            height: "620px",
            data: {
                transactionId: undefined
            }
        });
    }
}
