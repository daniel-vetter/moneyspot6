import { Component, input, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { SelectModule } from 'primeng/select';

@Component({
    selector: 'app-grouping-bar',
    imports: [SelectModule, FormsModule],
    templateUrl: './grouping-bar.component.html',
    styleUrl: './grouping-bar.component.scss'
})
export class GroupingBarComponent implements OnInit {
    private activatedRoute = inject(ActivatedRoute);
    private router = inject(Router);


    showDataTypeSelection = input<Boolean>();

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
