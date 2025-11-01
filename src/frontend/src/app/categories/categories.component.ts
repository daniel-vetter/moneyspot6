import { Component, OnInit, inject } from '@angular/core';

import { HighchartsChartModule } from 'highcharts-angular';
import * as HighchartsStock from 'highcharts/highstock';
import 'highcharts/modules/sankey'; // <-- Import the module
import { CategoryPageClient } from '../server';
import { lastValueFrom } from 'rxjs';
import { DatePickerModule } from 'primeng/datepicker';
import { FormsModule } from '@angular/forms';
import { DateRange, DateRangePickerComponent } from "../common/date-range-picker/date-range-picker.component";
import { DateRangePresetsComponent } from "../common/date-range-presets/date-range-presets.component";
import { ActivatedRoute } from '@angular/router';

@Component({
    selector: 'app-categories',
    imports: [HighchartsChartModule, DatePickerModule, FormsModule, DateRangePickerComponent, DateRangePresetsComponent],
    templateUrl: './categories.component.html',
    styleUrl: './categories.component.scss'
})
export class CategoriesComponent implements OnInit {
    private categoryPageClient = inject(CategoryPageClient);
    Highcharts: typeof HighchartsStock = HighchartsStock;
    charts: (Highcharts.Options & { index: number })[] = [];
    private activatedRoute = inject(ActivatedRoute);
    private dateRange: DateRange | undefined;

    async ngOnInit(): Promise<void> {
        this.activatedRoute.queryParams.subscribe(async (x) => {
            this.dateRange = DateRange.parse(x['dateRange']);
            await this.update();
        });
        await this.update();
    }

    convertDate(date: Date, addDay = false): string {
        const converted = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 0, 0, 0, 0));
        if (addDay) {
            converted.setDate(converted.getDate() + 1);
        }
        return converted.toISOString().substring(0, 10);
    }

    async update() {
        const dr = this.dateRange === undefined
            ? new DateRange(new Date(1900, 0, 1), new Date(2999, 0, 1))
            : this.dateRange;

        const start = this.convertDate(dr.start);
        const end = this.convertDate(dr.end, true);

        const r = await lastValueFrom(this.categoryPageClient.getSankeyData(start, end));
        this.charts = [];
        this.charts.push({
            index: this.charts.length,
            title: { text: '' },
            series: [{
                type: 'sankey',
                keys: ['from', 'to', 'weight'],
                nodes: r.nodes,
                data: r.connections?.map(x => [x.from, x.to, x.amount]),
            }]
        });
    }
}
