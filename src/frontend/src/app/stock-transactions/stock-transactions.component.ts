import { Component, OnInit, signal, inject } from '@angular/core';
import { PanelModule } from "primeng/panel";
import { PrimeTemplate } from "primeng/api";
import { Ripple } from "primeng/ripple";
import { ValueComponent } from "../common/value/value.component";
import { PortfolioStockResponse, StockTransactionResponse, StockTransactionsPageClient } from "../server";
import { firstValueFrom, lastValueFrom } from "rxjs";
import { ButtonModule } from "primeng/button";
import { DialogService } from "primeng/dynamicdialog";
import { StockTransactionEditDialogComponent } from "./stock-transaction-edit-dialog/stock-transaction-edit-dialog.component";
import { CommonModule, DecimalPipe } from '@angular/common';
import { CustomDatePipe } from "../common/custom-date.pipe";
import { TabsModule } from 'primeng/tabs';
import { RouterLink, RouterModule } from '@angular/router';

@Component({
    selector: 'app-stock-transactions',
    imports: [
        PanelModule,
        PrimeTemplate,
        Ripple,
        ValueComponent,
        ButtonModule,
        DecimalPipe,
        CustomDatePipe,
        TabsModule,
        RouterModule
    ],
    providers: [DialogService],
    templateUrl: './stock-transactions.component.html',
    styleUrl: './stock-transactions.component.scss'
})
export class StockTransactionsComponent implements OnInit {
    private stockTransactionsPageClient = inject(StockTransactionsPageClient);
    private dialogService = inject(DialogService);

    transactions = signal<StockTransactionResponse[] | undefined>(undefined);
    portfolio = signal<PortfolioStockResponse[] | undefined>(undefined);

    async ngOnInit(): Promise<void> {
        await this.update();
    }

    async onNewClicked() {
        const dlg = this.dialogService.open(StockTransactionEditDialogComponent, {
            data: {
                id: undefined
            },
            modal: true
        });

        await firstValueFrom(dlg.onClose);
        this.update();
    }
    async update() {
        this.portfolio.set((await lastValueFrom(this.stockTransactionsPageClient.getPortfolio())).reverse());
        this.transactions.set((await lastValueFrom(this.stockTransactionsPageClient.getStockTransactions())).reverse());
    }

    async onTransactionClicked(transaction: StockTransactionResponse) {
        const dlg = this.dialogService.open(StockTransactionEditDialogComponent, {
            data: {
                id: transaction.id
            },
            modal: true
        });

        await firstValueFrom(dlg.onClose);
        this.update();
    }
}
