import { Component, OnInit } from '@angular/core';
import { StockChartPageClient, StockResponse } from '../server';
import { lastValueFrom } from 'rxjs';
import { DropdownModule } from 'primeng/dropdown';
import { FormsModule } from '@angular/forms';
import { PanelModule } from 'primeng/panel';
import { HighchartsChartModule } from 'highcharts-angular';
import * as Highcharts from 'highcharts/highstock';

@Component({
  selector: 'app-stock-history',
  standalone: true,
  imports: [DropdownModule, FormsModule, PanelModule, HighchartsChartModule],
  templateUrl: './stock-history.component.html',
  styleUrl: './stock-history.component.scss'
})
export class StockHistoryComponent implements OnInit {
  stocks: StockResponse[] | undefined;
  selectedStockId?: number;
  Highcharts: typeof Highcharts = Highcharts;
  chart?: Highcharts.Options;

  constructor(private stockChartPageClient: StockChartPageClient) {

  }

  async ngOnInit(): Promise<void> {
    this.stocks = await lastValueFrom(this.stockChartPageClient.getStocks());
    if (this.stocks.length > 0) {
      this.selectedStockId = this.stocks[0].id;
    }

    const data = await lastValueFrom(this.stockChartPageClient.getHistory(this.selectedStockId, undefined, undefined));
    this.chart = {
      chart: {
        animation: {
          duration: 0
        },
        height: "50%",
        zooming: {
          type: "x"
        }
      },
      xAxis: {
        type: "datetime"
      },
      rangeSelector: {
        selected: 3,
        buttons: [{
          type: 'day',
          count: 1,
          text: '1d',
          title: 'View 1 day'
        }, {
          type: 'day',
          count: 7,
          text: '7d',
          title: 'View 7 day'
        }, {
          type: 'month',
          count: 1,
          text: '1m',
          title: 'View 1 months'
        }, {
          type: 'month',
          count: 3,
          text: '3m',
          title: 'View 3 months'
        }, {
          type: 'month',
          count: 6,
          text: '6m',
          title: 'View 6 months'
        }, {
          type: 'ytd',
          text: 'YTD',
          title: 'View year to date'
        }, {
          type: 'year',
          count: 1,
          text: '1y',
          title: 'View 1 year'
        }, {
          type: 'all',
          text: 'All',
          title: 'View all'
        }]
      },
      series: [
        {
          type: "line",
          data: data.flatMap(x => [
            [new Date(x.date!).setHours(7), x.open],
            [new Date(x.date!).setHours(16), x.close]
          ]),
          animation: {
            duration: 0
          }
        }
      ]
    }
  }

}
