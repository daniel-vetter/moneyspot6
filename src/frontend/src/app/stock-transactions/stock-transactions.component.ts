import { Component, OnInit, signal, inject } from '@angular/core';
import { PanelModule } from "primeng/panel";
import { PrimeTemplate } from "primeng/api";
import { Ripple } from "primeng/ripple";
import { ValueComponent } from "../common/value/value.component";
import { PortfolioStockResponse, StockTransactionResponse, StockTransactionsPageClient } from "../server";
import { firstValueFrom, lastValueFrom } from "rxjs";
import { ButtonModule } from "primeng/button";
import { StockTransactionEditDialogComponent } from "./stock-transaction-edit-dialog/stock-transaction-edit-dialog.component";
import { CommonModule, DecimalPipe } from '@angular/common';
import { CustomDatePipe } from "../common/custom-date.pipe";
import { TabsModule } from 'primeng/tabs';
import { RouterLink, RouterModule } from '@angular/router';
import { ModalDialogService } from "../common/modal-dialog.service";

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
    templateUrl: './stock-transactions.component.html',
    styleUrl: './stock-transactions.component.scss'
})
export class StockTransactionsComponent implements OnInit {
    private stockTransactionsPageClient = inject(StockTransactionsPageClient);
    private modalDialogService = inject(ModalDialogService);

    transactions = signal<StockTransactionResponse[] | undefined>(undefined);
    portfolio = signal<PortfolioStockResponse[] | undefined>(undefined);

    async ngOnInit(): Promise<void> {
        await this.update();
    }

    async onNewClicked() {
        const dlg = this.modalDialogService.open(StockTransactionEditDialogComponent, {
            data: {
                id: undefined
            }
        });

        await firstValueFrom(dlg.onClose);
        this.update();
    }
    async update() {
        this.portfolio.set((await lastValueFrom(this.stockTransactionsPageClient.getPortfolio())).reverse());
        this.transactions.set((await lastValueFrom(this.stockTransactionsPageClient.getStockTransactions())).reverse());
    }

    async onTransactionClicked(transaction: StockTransactionResponse) {
        const dlg = this.modalDialogService.open(StockTransactionEditDialogComponent, {
            data: {
                id: transaction.id
            }
        });

        await firstValueFrom(dlg.onClose);
        this.update();
    }
}
