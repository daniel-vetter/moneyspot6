import { Component, OnInit } from '@angular/core';
import { StockChartPageClient, StockPriceInterval, StockResponse } from '../server';
import { lastValueFrom } from 'rxjs';
import { DropdownModule } from 'primeng/dropdown';
import { FormsModule } from '@angular/forms';
import { PanelModule } from 'primeng/panel';
import { HighchartsChartModule } from 'highcharts-angular';
import * as Highcharts from 'highcharts/highstock';

@Component({
  selector: 'app-stock-history',
  imports: [DropdownModule, FormsModule, PanelModule, HighchartsChartModule],
  templateUrl: './stock-history.component.html',
  styleUrl: './stock-history.component.scss'
})
export class StockHistoryComponent implements OnInit {
  possibleStocks: StockResponse[] | undefined = undefined;
  selectedStockId?: number;

  possibleIntervals = ["Täglich", "5 Minuten"]
  selectedInterval = "5 Minuten";

  Highcharts: typeof Highcharts = Highcharts;
  chart?: Highcharts.Options;

  constructor(private stockChartPageClient: StockChartPageClient) {

  }

  async ngOnInit(): Promise<void> {
    await this.update();
  }

  async update(): Promise<void> {
    this.possibleStocks = await lastValueFrom(this.stockChartPageClient.getStocks());
    if (this.possibleStocks.length > 0) {
      this.selectedStockId = this.possibleStocks[0].id;
    }

    const data = await lastValueFrom(this.stockChartPageClient.getHistory(this.selectedStockId, undefined, undefined, this.selectedInterval == "5 Minuten" ? StockPriceInterval.FiveMinutes : StockPriceInterval.Daily));
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
          type: "ohlc",
          name: "Preis",
          data: data.map(x => [+new Date(x.timestamp), x.open, x.high, x.low, x.close]),
          color: 'var(--p-red-600)',
          upColor: 'var(--p-green-600)',
          animation: {
            duration: 0
          }
        }
      ]
    }
  }

}
