import { Component, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TransactionEntryResponse, TransactionPageClient, TransactionResponse } from '../server';
import { lastValueFrom } from 'rxjs';
import { ValueComponent } from '../common/value/value.component';
import { CustomDatePipe } from '../common/custom-date.pipe';
import { PanelModule } from 'primeng/panel';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [TableModule, ValueComponent, PanelModule, CustomDatePipe],
  templateUrl: './transactions.component.html',
  styleUrl: './transactions.component.scss'
})
export class TransactionsComponent implements OnInit {
  transactions: TransactionResponse[] = [];
  blocks: Block[] = [];

  constructor(private transactionPageClient: TransactionPageClient) { }

  async ngOnInit(): Promise<void> {

    let date = DateOnly.today().setDay(1);
    for (let i = 0; i < 12; i++) {
      const response = await lastValueFrom(this.transactionPageClient.getTransactions(date.toTransport(), date.addMonths(1).toTransport()));
      this.blocks.push({
        title: `${["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember"][date.month - 1]} ${date.year}`,
        month: date.month,
        year: date.year,
        transactions: response.entries!,
        total: response.total!,
        income: response.income!,
        expense: response.expense!
      })

      date = date.addMonths(-1);
    }
  }
}

class DateOnly {

  constructor(
    public readonly year: number,
    public readonly month: number,
    public readonly day: number) {
  }

  public static today() {
    const d = new Date();
    return new DateOnly(d.getFullYear(), d.getMonth() + 1, d.getDate());
  }

  toISOString() {
    return `${this.year}-${this.month}-${this.day}`;
  }

  toTransport(): Date {
    return <any>this;
  }

  setDay(day: number): DateOnly {
    return new DateOnly(this.year, this.month, day);
  }

  addMonths(count: number): DateOnly {
    const d = new Date(this.year, this.month - 1, this.day);
    d.setMonth(d.getMonth() + count);
    return new DateOnly(d.getFullYear(), d.getMonth() + 1, d.getDate())
  }
}

interface Block {
  title: string;
  year: number;
  month: number;
  transactions: TransactionEntryResponse[];
  total: number;
  income: number;
  expense: number;
}