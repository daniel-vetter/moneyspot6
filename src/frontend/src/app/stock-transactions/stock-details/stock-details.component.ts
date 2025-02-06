import { Component, resource, signal } from '@angular/core';
import { PanelModule } from 'primeng/panel';
import { ValueComponent } from '../../common/value/value.component';
import { DecimalPipe } from '@angular/common';
import { PortfolioStockPurchaseResponse, PortfolioStockResponse, StockTransactionsPageClient } from '../../server';
import { ActivatedRoute } from '@angular/router';
import { lastValueFrom } from 'rxjs';
import { CustomDatePipe } from '../../common/custom-date.pipe';

@Component({
  selector: 'app-stock-details',
  imports: [PanelModule, ValueComponent, DecimalPipe, CustomDatePipe],
  templateUrl: './stock-details.component.html',
  styleUrl: './stock-details.component.scss'
})
export class StockDetailsComponent {
  readonly stock = signal<PortfolioStockResponse | undefined>(undefined);

  readonly currentStockId = signal(0);

  constructor(activatedRoute: ActivatedRoute, private stockTransactionsPageClient: StockTransactionsPageClient) {
    activatedRoute.paramMap.subscribe(async x => {
      const id = +x.get("id")!;
      const response = (await lastValueFrom(stockTransactionsPageClient.getPortfolio()))
      const entry = response.find(s => s.stockId == id);
      entry?.purchases.reverse();
      this.stock.set(entry);
    });
  }
}
