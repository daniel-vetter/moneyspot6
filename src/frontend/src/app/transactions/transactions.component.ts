import { Component, OnInit, inject } from '@angular/core';
import { TransactionEntryResponse, TransactionPageClient, TransactionResponse, TransactionType } from '../server';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { ValueComponent } from '../common/value/value.component';
import { CustomDatePipe } from '../common/custom-date.pipe';
import { PanelModule } from 'primeng/panel';
import { ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { RippleModule } from 'primeng/ripple';
import { SearchBarComponent } from '../common/search-bar/search-bar.component';
import { ViewGrouping, GroupingBarComponent } from '../common/grouping-bar/grouping-bar.component';
import { DialogService } from 'primeng/dynamicdialog';
import { TransactionDetailsDialogComponent } from './transaction-details-dialog/transaction-details-dialog.component';
import { InflationAdjustmentDialogComponent } from './inflation-adjustment-dialog/inflation-adjustment-dialog.component';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { AppEvents } from '../app-events';
import { DatePickerModule } from 'primeng/datepicker';
import { FormsModule } from '@angular/forms';
import { DateRangePickerComponent, DateRange } from "../common/date-range-picker/date-range-picker.component";

@Component({
    selector: 'app-transactions',
    imports: [ValueComponent, PanelModule, CustomDatePipe, ButtonModule, ProgressSpinnerModule, RippleModule, SearchBarComponent, GroupingBarComponent, TagModule, TooltipModule, DatePickerModule, FormsModule, DateRangePickerComponent],
    providers: [DialogService],
    templateUrl: './transactions.component.html',
    styleUrl: './transactions.component.scss'
})
export class TransactionsComponent implements OnInit {
    private transactionPageClient = inject(TransactionPageClient);
    private dialogService = inject(DialogService);
    private activatedRoute = inject(ActivatedRoute);
    private appEvents = inject(AppEvents);

    transactions: TransactionResponse[] = [];
    blocksShown: Block[] = [];
    blocksHidden: Block[] = [];
    searchText: string = '';
    isLoading = false;
    selectedGrouping: ViewGrouping = 'Monthly';
    isFirstUpdate = true;
    dateRange: DateRange | undefined;
    inflationAdjustmentDate: Date | undefined;

    async ngOnInit(): Promise<void> {
        this.activatedRoute.queryParams.subscribe(async (x) => {
            this.searchText = x['search'] ?? '';
            this.selectedGrouping = x['grouping'] ?? 'Monthly';
            this.dateRange = DateRange.parse(x['dateRange']);
            console.log(this.dateRange)
            await this.update();
        });
    }

    convertDate(date: Date, addDay = false): string {
        const converted = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 0, 0, 0, 0));
        if (addDay) {
            converted.setDate(converted.getDate() + 1);
        }
        return converted.toISOString().substring(0, 10);
    }

    async update(keepState: boolean = false) {

        const curShownBlockCount = this.blocksShown.length
        if (keepState == false) {
            this.blocksShown = [];
            this.isLoading = true;
        }
        const response = await lastValueFrom(this.transactionPageClient.getTransactions(
            this.searchText === '' ? undefined : this.searchText,
            this.dateRange === undefined ? undefined : this.convertDate(this.dateRange!.start),
            this.dateRange === undefined ? undefined : this.convertDate(this.dateRange!.end, true),
            this.inflationAdjustmentDate === undefined ? undefined : this.convertDate(this.inflationAdjustmentDate)));
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

            currentBlock.total += transaction.amount!;
            currentBlock.income += transaction.amount! > 0 ? transaction.amount! : 0;
            currentBlock.expense += transaction.amount! < 0 ? -transaction.amount! : 0;
            currentBlock.transactions.push(transaction);
        }

        this.blocksHidden = blocks;
        this.blocksShown = [];
        this.showMore(keepState ? curShownBlockCount : undefined);

        if (this.isFirstUpdate) {
            this.isFirstUpdate = false;
            const hasChangedSomething = await lastValueFrom(this.transactionPageClient.markAllSeen());
            if (hasChangedSomething) {
                this.appEvents.emit({ type: 'NewTransactionsSeenEvent' })
            }
        }
    }

    showMore(blocksToShow: number | undefined = undefined) {
        let totalEntriesShown = 0;
        while (this.blocksHidden.length > 0) {
            const toMove = this.blocksHidden.shift()!;
            totalEntriesShown += toMove.transactions.length;
            this.blocksShown.push(toMove);

            if ((blocksToShow == undefined && totalEntriesShown > 500) || (blocksToShow !== undefined && this.blocksShown.length >= blocksToShow)) {
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

    async openTransaction(transaction: TransactionEntryResponse) {
        console.log("Clicked");
        const dlg = this.dialogService.open(TransactionDetailsDialogComponent, {
            data: {
                id: transaction.id,
            },
            focusOnShow: false
        });

        const result = await firstValueFrom(dlg.onClose)
        if (result === true) {
            await this.update(true);
        }
    }

    getTypeIcon(type: TransactionType | undefined, amount: number | undefined): string {
        switch (type) {
            case TransactionType.Transfer: return 'pi pi-arrows-h';
            case TransactionType.Investment: return 'pi pi-chart-line';
            case TransactionType.Refund: return 'pi pi-replay';
            default: return (amount ?? 0) >= 0 ? 'pi pi-arrow-up-right' : 'pi pi-arrow-down-right';
        }
    }

    getTypeTooltip(type: TransactionType | undefined, amount: number | undefined): string {
        switch (type) {
            case TransactionType.Transfer: return 'Umbuchung';
            case TransactionType.Investment: return 'Investment';
            case TransactionType.Refund: return 'Erstattung';
            default: return (amount ?? 0) >= 0 ? 'Einnahme' : 'Ausgabe';
        }
    }

    async toggleInflationAdjustment() {
        if (this.inflationAdjustmentDate !== undefined) {
            // Deaktivieren
            this.inflationAdjustmentDate = undefined;
            await this.update();
        } else {
            // Dialog öffnen
            const dlg = this.dialogService.open(InflationAdjustmentDialogComponent, {
                focusOnShow: false
            });

            const result = await firstValueFrom(dlg.onClose);
            if (result !== undefined) {
                this.inflationAdjustmentDate = result;
                await this.update();
            }
        }
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
