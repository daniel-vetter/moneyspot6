import { Component, OnInit } from '@angular/core';
import { TransactionEntryResponse, TransactionPageClient, TransactionResponse } from '../server';
import { lastValueFrom } from 'rxjs';
import { ValueComponent } from '../common/value/value.component';
import { CustomDatePipe } from '../common/custom-date.pipe';
import { PanelModule } from 'primeng/panel';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { RippleModule } from 'primeng/ripple';
import { SearchBarComponent } from '../common/search-bar/search-bar.component';
import { Grouping, GroupingBarComponent } from '../common/grouping-bar/grouping-bar.component';

@Component({
    selector: 'app-transactions',
    standalone: true,
    imports: [ValueComponent, PanelModule, CustomDatePipe, ButtonModule, ProgressSpinnerModule, RippleModule, SearchBarComponent, GroupingBarComponent],
    templateUrl: './transactions.component.html',
    styleUrl: './transactions.component.scss',
})
export class TransactionsComponent implements OnInit {
    transactions: TransactionResponse[] = [];
    blocksShown: Block[] = [];
    blocksHidden: Block[] = [];
    searchText: string = '';
    isLoading = false;
    selectedGrouping: Grouping = 'Monthly';

    constructor(
        private transactionPageClient: TransactionPageClient,
        private router: Router,
        private activatedRoute: ActivatedRoute,
    ) { }

    async ngOnInit(): Promise<void> {
        this.activatedRoute.queryParams.subscribe(async (x) => {
            this.searchText = x['search'] ?? '';
            this.selectedGrouping = x['grouping'] ?? 'Monthly';
            await this.update();
        });
    }

    async update() {
        this.blocksShown = [];
        this.isLoading = true;
        const response = await lastValueFrom(this.transactionPageClient.getTransactions(this.searchText === '' ? undefined : this.searchText));
        this.isLoading = false;
        const blocks: Block[] = [];
        let currentBlock: Block | undefined;
        for (const transaction of response.entries!) {
            const groupId = this.getGroupId(transaction.date!);
            if (currentBlock === undefined || currentBlock.id != groupId) {
                currentBlock = {
                    id: groupId,
                    total: 0,
                    expense: 0,
                    income: 0,
                    transactions: [],
                    title: this.getTitle(transaction.date!),
                };
                blocks.push(currentBlock);
            }

            currentBlock.total += transaction.value!;
            currentBlock.income += transaction.value! > 0 ? transaction.value! : 0;
            currentBlock.expense += transaction.value! < 0 ? -transaction.value! : 0;
            currentBlock.transactions.push(transaction);
        }

        this.blocksHidden = blocks;
        this.blocksShown = [];
        this.showMore();
    }

    showMore() {
        let totalEntriesShown = 0;
        while (this.blocksHidden.length > 0) {
            const toMove = this.blocksHidden.shift()!;
            totalEntriesShown += toMove.transactions.length;
            this.blocksShown.push(toMove);

            if (totalEntriesShown > 1000) {
                break;
            }
        }
    }

    getGroupId(date: Date): string {
        if (this.selectedGrouping === 'None') return '0';
        if (this.selectedGrouping === 'Monthly') return date.getFullYear().toString() + date.getMonth().toString();
        if (this.selectedGrouping === 'Yearly') return date.getFullYear().toString();
        throw Error('Invalid group: ' + this.selectedGrouping);
    }

    getTitle(date: Date): string {
        if (this.selectedGrouping === 'None') return 'Ergebnis';
        if (this.selectedGrouping === 'Monthly')
            return (
                ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni', 'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'][date.getMonth()] +
                ' ' +
                date.getFullYear().toString()
            );
        if (this.selectedGrouping === 'Yearly') return date.getFullYear().toString();
        throw Error('Invalid group: ' + this.selectedGrouping);
    }
}

interface Block {
    id: string;
    title: string;
    transactions: TransactionEntryResponse[];
    total: number;
    income: number;
    expense: number;
}
