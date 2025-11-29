import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { StockClient, CreateStockRequest, StockSearchResponse } from '../../../server';
import { lastValueFrom } from 'rxjs';
import { ListboxModule } from 'primeng/listbox';

@Component({
    selector: 'app-stock-dialog',
    imports: [ButtonModule, FormsModule, CommonModule, InputTextModule, ListboxModule],
    templateUrl: './stock-dialog.component.html',
    styleUrl: './stock-dialog.component.scss',
    standalone: true
})
export class StockDialogComponent {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    private stockClient = inject(StockClient);

    searchQuery: string = '';
    searchResults: StockSearchResponse[] | undefined;
    selectedStock: StockSearchResponse | null = null;
    isSearching: boolean = false;
    hasSearched: boolean = false;

    constructor() {
        this.dialogConfig.header = 'Neue Aktie';
        this.dialogConfig.width = '600px';
        this.dialogConfig.height = '600px';
        this.dialogConfig.modal = true;
    }

    onCancelClicked() {
        this.dialogRef.close();
    }

    async onSearch() {
        if (!this.searchQuery) {
            this.searchResults = [];
            return;
        }

        this.isSearching = true;
        this.hasSearched = false;
        this.selectedStock = null;

        try {
            this.searchResults = await lastValueFrom(this.stockClient.search(this.searchQuery));
            this.hasSearched = true;
        } finally {
            this.isSearching = false;
        }
    }

    async onSubmit() {
        if (!this.selectedStock) {
            return;
        }

        await lastValueFrom(this.stockClient.create(new CreateStockRequest({
            name: this.selectedStock.name,
            symbol: this.selectedStock.symbol
        })));

        this.dialogRef.close(true);
    }
}
