import { Component, OnInit } from '@angular/core';

import { HighchartsChartModule } from 'highcharts-angular';
import * as HighchartsStock from 'highcharts/highstock';
import 'highcharts/modules/sankey'; // <-- Import the module
import { CategoryPageClient } from '../server';
import { lastValueFrom } from 'rxjs';
import { DatePickerModule } from 'primeng/datepicker';
import { FormsModule } from '@angular/forms';
import { DaterangePresetSelectorComponent } from '../history/daterange-preset-selector/daterange-preset-selector.component';

@Component({
  selector: 'app-categories',
  imports: [HighchartsChartModule, DatePickerModule, FormsModule, DaterangePresetSelectorComponent],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss'
})
export class CategoriesComponent implements OnInit {


  Highcharts: typeof HighchartsStock = HighchartsStock;
  charts: (Highcharts.Options & { index: number })[] = [];
  dateRange: [Date, Date];

  constructor(private categoryPageClient: CategoryPageClient) {
    const start = new Date();
    start.setDate(start.getDate() + 1);
    start.setMonth(start.getMonth() - 12);
    start.setHours(0, 0, 0, 0);

    const end = new Date();
    end.setHours(0, 0, 0, 0);

    this.dateRange = [start, end];
  }

  async ngOnInit(): Promise<void> {
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
    if (this.dateRange[0] === null || this.dateRange[0] === undefined) return;
    if (this.dateRange[1] === null || this.dateRange[1] === undefined) return;

    const start = this.convertDate(this.dateRange[0]);
    const end = this.convertDate(this.dateRange[1], true);

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
