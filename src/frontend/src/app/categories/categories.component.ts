import { Component, OnInit, inject, signal } from '@angular/core';
import { CategoryPageClient } from '../server';
import { lastValueFrom } from 'rxjs';
import { DatePickerModule } from 'primeng/datepicker';
import { FormsModule } from '@angular/forms';
import { DateRange, DateRangePickerComponent } from "../common/date-range-picker/date-range-picker.component";
import { DateRangePresetsComponent } from "../common/date-range-presets/date-range-presets.component";
import { ActivatedRoute } from '@angular/router';
import { PanelModule } from 'primeng/panel';
import { EChartsOption } from 'echarts';
import { EchartComponent } from '../common/echart/echart.component';
import { formatEur } from '../common/echart/chart-format';

interface SankeyTooltipParam {
    dataType: 'node' | 'edge';
    name: string;
    value: number;
    data: { source?: string; target?: string };
}

@Component({
    selector: 'app-categories',
    imports: [DatePickerModule, FormsModule, DateRangePickerComponent, DateRangePresetsComponent, PanelModule, EchartComponent],
    templateUrl: './categories.component.html',
    styleUrl: './categories.component.scss'
})
export class CategoriesComponent implements OnInit {
    private categoryPageClient = inject(CategoryPageClient);
    private activatedRoute = inject(ActivatedRoute);
    private dateRange: DateRange | undefined;

    protected readonly options = signal<EChartsOption | undefined>(undefined);

    async ngOnInit(): Promise<void> {
        this.activatedRoute.queryParams.subscribe(async (x) => {
            this.dateRange = DateRange.parse(x['dateRange']);
            await this.update();
        });
    }

    private convertDate(date: Date, addDay = false): string {
        const converted = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 0, 0, 0, 0));
        if (addDay) {
            converted.setDate(converted.getDate() + 1);
        }
        return converted.toISOString().substring(0, 10);
    }

    private async update() {
        const dr = this.dateRange === undefined
            ? new DateRange(new Date(1900, 0, 1), new Date(2999, 0, 1))
            : this.dateRange;

        const start = this.convertDate(dr.start);
        const end = this.convertDate(dr.end, true);

        const r = await lastValueFrom(this.categoryPageClient.getSankeyData(start, end));
        const nameById = new Map(r.nodes.map(n => [n.id, n.name]));

        this.options.set({
            animation: false,
            tooltip: {
                trigger: 'item',
                formatter: (params: unknown) => CategoriesComponent.tooltipFormatter(params as SankeyTooltipParam, nameById)
            },
            series: [{
                type: 'sankey',
                nodeWidth: 20,
                nodeGap: 12,
                emphasis: { focus: 'adjacency' },
                label: {
                    fontSize: 14,
                    textBorderWidth: 0,
                    formatter: (params: { name: string }) => nameById.get(params.name) ?? params.name
                },
                lineStyle: { color: 'gradient', curveness: 0.5, opacity: 0.4 },
                data: r.nodes.map(n => ({ name: n.id, depth: n.column })),
                links: r.connections.map(c => ({ source: c.from, target: c.to, value: c.amount }))
            }]
        });
    }

    private static tooltipFormatter(p: SankeyTooltipParam, nameById: Map<string, string>): string {
        if (p.dataType === 'edge') {
            const from = nameById.get(p.data.source ?? '') ?? p.data.source;
            const to = nameById.get(p.data.target ?? '') ?? p.data.target;
            return `${from} → ${to}<br/><b>${formatEur(p.value)}</b>`;
        }
        const name = nameById.get(p.name) ?? p.name;
        return `<b>${name}</b><br/>${formatEur(p.value)}`;
    }
}
