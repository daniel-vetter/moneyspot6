import { Component, input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DropdownModule } from 'primeng/dropdown';

@Component({
    selector: 'app-grouping-bar',
    imports: [DropdownModule, FormsModule],
    templateUrl: './grouping-bar.component.html',
    styleUrl: './grouping-bar.component.scss'
})
export class GroupingBarComponent implements OnInit {

    showDataTypeSelection = input<Boolean>();

    constructor(
        private activatedRoute: ActivatedRoute,
        private router: Router,
    ) { }

    ngOnInit(): void {
        this.activatedRoute.queryParams.subscribe((x) => {
            this.selectedViewGrouping = x['grouping'] ?? 'Monthly';
            this.selectedData = x['view'] ?? 'AccountAndStocks';
        });
    }

    viewGroupOptions: ViewGroupingSelectItem[] = [
        { name: 'Keine', key: 'None' },
        { name: 'Monatlich', key: 'Monthly' },
        { name: 'Jährlich', key: 'Yearly' },
    ];
    selectedViewGrouping: ViewGrouping = 'Monthly';

    dataOptions: DataSelectItem[] = [
        { name: 'Konto und Aktien', key: 'AccountAndStocks' },
        { name: 'Konto', key: 'Account' },
        { name: 'Aktien', key: 'Stocks' },
    ];
    selectedData: ViewData = 'AccountAndStocks';

    async onGroupingChanged(event: any) {
        this.router.navigate([], {
            relativeTo: this.activatedRoute,
            queryParams: {
                grouping: this.selectedViewGrouping == 'Monthly' ? undefined : this.selectedViewGrouping,
                view: this.selectedData == 'AccountAndStocks' ? undefined : this.selectedData,
            },
            queryParamsHandling: 'merge',
        });
    }
}

interface ViewGroupingSelectItem {
    name: string;
    key: ViewGrouping;
}

interface DataSelectItem {
    name: string;
    key: ViewData;
}

export type ViewGrouping = 'None' | 'Monthly' | 'Yearly';
export type ViewData = 'Account' | 'Stocks' | 'AccountAndStocks';
