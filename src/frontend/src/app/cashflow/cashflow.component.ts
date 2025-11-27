import { Component, OnInit, inject } from '@angular/core';
import { CashflowClient, IncomeExpenseEntryResponse, IncomeExpenseGrouping } from '../server';
import { lastValueFrom } from 'rxjs';
import { ValueComponent } from '../common/value/value.component';
import { PanelModule } from 'primeng/panel';
import { RippleModule } from 'primeng/ripple';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { SearchBarComponent } from '../common/search-bar/search-bar.component';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ViewGrouping, GroupingBarComponent, ViewData } from '../common/grouping-bar/grouping-bar.component';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
    selector: 'app-income-expense-report',
    imports: [ValueComponent, PanelModule, RippleModule, FormsModule, InputTextModule, SearchBarComponent, GroupingBarComponent, RouterLink, ProgressSpinnerModule],
    templateUrl: './cashflow.component.html',
    styleUrl: './cashflow.component.scss'
})
export class CashflowComponent implements OnInit {
    private cashflowClient = inject(CashflowClient);
    private activatedRoute = inject(ActivatedRoute);

    dataType: ViewData = 'AccountAndStocks';
    onSearchSubmit() { }
    lines: Line[] = [];
    blocks: Block[] = [];
    isLoading = false;
    searchText?: string;
    grouping: ViewGrouping = 'Monthly';

    async ngOnInit(): Promise<void> {
        this.activatedRoute.queryParams.subscribe(async (x) => {
            this.searchText = x['search'];
            this.grouping = x['grouping'] ?? 'Monthly';
            this.dataType = x['view'] ?? 'AccountAndStocks';
            await this.update();
        });
    }

    private async update(): Promise<void> {
        this.blocks = [];
        this.isLoading = true;
        const response = (
            await lastValueFrom(
                this.cashflowClient.get(
                    this.searchText,
                    this.grouping === 'Monthly'
                        ? IncomeExpenseGrouping.Month
                        : this.grouping === 'None'
                            ? IncomeExpenseGrouping.None
                            : IncomeExpenseGrouping.Year,
                ),
            )
        ).reverse();
        const blocks: Block[] = [];

        let currentBlock: Block | undefined;
        for (const entry of response) {
            const groupId = this.getGroupId(entry);
            if (currentBlock === undefined || currentBlock.id != groupId) {
                currentBlock = {
                    id: groupId,
                    name: this.getGroupId(entry).toString(),
                    lines: [],
                    income: 0,
                    expense: 0,
                    accountBalance: 0,
                    stockBalance: 0,
                    total: 0
                };
                blocks.push(currentBlock);
            }

            currentBlock.lines.push({
                id: entry.month,
                name: this.getLineName(entry.month),
                expense: entry.expense,
                income: entry.income,
                accountBalance: entry.income - entry.expense,
                stockBalance: entry.stockBalance,
                total: entry.income - entry.expense + entry.stockBalance,
                bar: <any>{}!,
                dateRange: this.getDateRange(entry.month)
            });
        }

        for (const block of blocks) {
            this.calcBars(block.lines);
            this.calcTotals(block);
        }
        this.blocks = blocks;
        this.isLoading = false;
    }
    calcTotals(block: Block) {
        for (const line of block.lines) {
            block.income += line.income;
            block.expense += line.expense;
            block.accountBalance += line.accountBalance;
            block.stockBalance += line.stockBalance;
            block.total += line.total;
        }
    }

    calcBars(lines: Line[]) {

        let max = 0;
        for (const line of lines) {
            max = Math.max(max, Math.abs(this.dataType == "AccountAndStocks" ? line.total : this.dataType == "Account" ? line.accountBalance : line.stockBalance));
        }
        max *= 1.2;

        for (const line of lines) {
            const total = this.dataType == "AccountAndStocks" ? line.total : this.dataType == "Account" ? line.accountBalance : line.stockBalance;
            if (total > 0) {
                const percent = ((total / max) * 100) / 2;
                line.bar = {
                    color: 'var(--p-green-600)',
                    left: 50,
                    width: percent,
                    radius: '0 0.5rem 0.5rem 0',
                };
            } else {
                const percent = ((-total / max) * 100) / 2;
                line.bar = {
                    color: 'var(--p-red-600)',
                    left: 50 - percent,
                    width: percent,
                    radius: '0.5rem 0 0 0.5rem',
                };
            }
        }
    }

    getGroupId(entry: IncomeExpenseEntryResponse) {
        if (this.grouping === "None") return "Gesamt";
        if (this.grouping === "Yearly") return "Gesamt"
        if (this.grouping === "Monthly") return Math.floor(entry.month / 13).toString();
        throw Error("Invalid grouping");
    }

    private getLineName(month: number) {
        if (month === 0) return 'Gesamt';
        const y = Math.floor(month / 13);
        const m = month % 13;
        if (m === 0) {
            return y.toString();
        }
        return ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni', 'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'][m - 1] + ' ' + y;
    }

    private getDateRange(month: number): string | undefined {
        if (month === 0) return undefined;
        const y = Math.floor(month / 13);
        const m = month % 13;
        if (m === 0) {
            return `${y}0101${y}1231`;
        }
        return `${y}${this.toTwoDigits(m)}01${y}${this.toTwoDigits(m)}${this.daysInMonth(m, y)}`;
    }

    toTwoDigits(m: number) {
        return m < 10 ? `0${m}` : `${m}`;
    }
    daysInMonth(m: number, y: number) {
        return new Date(y, m, 0).getDate();
    }
}

interface Line {
    id: number;
    name: string;
    income: number;
    expense: number;
    accountBalance: number;
    stockBalance: number;
    total: number;
    bar: Bar;
    dateRange: string | undefined;
}

interface Bar {
    left: number;
    width: number;
    color: string;
    radius: string;
}

interface Block {
    id: string;
    name: string;
    lines: Line[];
    income: number;
    expense: number;
    accountBalance: number;
    stockBalance: number;
    total: number;
}
