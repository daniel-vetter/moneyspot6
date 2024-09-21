import { Component, OnInit } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { HubConnectionBuilder } from "@microsoft/signalr";
import { lastValueFrom } from 'rxjs';
import { BankAccountSummaryResponse, SummaryPageClient } from '../server';
import { RippleModule } from 'primeng/ripple';
import { CardModule } from 'primeng/card';
import { ValueComponent } from "../common/value/value.component";
import { PanelModule } from 'primeng/panel';
import { AccountSyncComponent } from "../account-sync/account-sync.component";

@Component({
  selector: 'app-summary',
  standalone: true,
  imports: [ButtonModule, RippleModule, CardModule, ValueComponent, PanelModule, AccountSyncComponent],
  templateUrl: './summary.component.html',
  styleUrl: './summary.component.scss'
})
export class SummaryComponent implements OnInit {

  bankAccounts?: BankAccountSummaryResponse;
  total = 0;

  constructor(private summaryPageClient: SummaryPageClient) { }

  async ngOnInit(): Promise<void> {
    this.bankAccounts = await lastValueFrom(this.summaryPageClient.getBackAccountSummary())
    this.total = this.bankAccounts.entries?.reduce((a, b) => a + b.total!, 0)!;
  }
}

