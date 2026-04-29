import { Component, OnInit, inject } from '@angular/core';
import { StockClient, StockListResponse } from '../../server';
import { firstValueFrom } from 'rxjs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { StockDialogComponent } from './stock-dialog/stock-dialog.component';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { CommonModule } from '@angular/common';
import { PanelModule } from 'primeng/panel';
import { ModalDialogService } from '../../common/modal-dialog.service';

@Component({
    selector: 'app-stocks',
    imports: [TableModule, ButtonModule, ConfirmDialogModule, ToastModule, CommonModule, PanelModule],
    providers: [ConfirmationService, MessageService],
    templateUrl: './stocks.component.html',
    styleUrl: './stocks.component.scss',
    standalone: true
})
export class StocksComponent implements OnInit {
    private stockClient = inject(StockClient);
    private modalDialogService = inject(ModalDialogService);
    private confirmationService = inject(ConfirmationService);
    private messageService = inject(MessageService);

    stocks: StockListResponse[] = [];

    async ngOnInit() {
        await this.loadStocks();
    }

    async loadStocks() {
        const response = await firstValueFrom(this.stockClient.getAll());
        this.stocks = response;
    }

    async createStock() {
        const dlg = this.modalDialogService.open(StockDialogComponent, {
            focusOnShow: false
        });

        const result = await firstValueFrom(dlg.onClose);
        if (result === true) {
            this.messageService.add({
                severity: 'success',
                summary: 'Aktie erstellt',
                detail: 'Die Aktie wurde erfolgreich erstellt.'
            });
            await this.loadStocks();
        }
    }

    async deleteStock(stock: StockListResponse) {
        this.confirmationService.confirm({
            message: `Möchten Sie die Aktie "${stock.name}" wirklich löschen? Alle zugehörigen Kursdaten und Transaktionen werden ebenfalls gelöscht.`,
            header: 'Aktie löschen',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Ja',
            rejectLabel: 'Nein',
            accept: async () => {
                await firstValueFrom(this.stockClient.delete(stock.id));
                this.messageService.add({
                    severity: 'success',
                    summary: 'Aktie gelöscht',
                    detail: 'Die Aktie wurde erfolgreich gelöscht.'
                });
                await this.loadStocks();
            }
        });
    }
}
