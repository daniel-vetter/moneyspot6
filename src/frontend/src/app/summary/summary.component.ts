import { Component, OnInit } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { HubConnectionBuilder } from "@microsoft/signalr";
import { lastValueFrom } from 'rxjs';
import { BankAccountSummaryResponse, SummaryPageClient } from '../server';
import { RippleModule } from 'primeng/ripple';
import { CardModule } from 'primeng/card';
import { ValueComponent } from "../common/value/value.component";

@Component({
  selector: 'app-summary',
  standalone: true,
  imports: [ButtonModule, RippleModule, CardModule, ValueComponent],
  templateUrl: './summary.component.html',
  styleUrl: './summary.component.scss'
})
export class SummaryComponent implements OnInit {

  bankAccounts?: BankAccountSummaryResponse;

  constructor(private summaryPageClient: SummaryPageClient) { }

  async ngOnInit(): Promise<void> {
    this.bankAccounts = await lastValueFrom(this.summaryPageClient.getBackAccountSummary())
  }

  async onSyncButtonClicked() {
    const connection = new HubConnectionBuilder()
      .withUrl("/api/account-sync")
      .build();

    connection.on("requestTan", (message: String) => {
      return prompt("TAN: " + message);
    });

    connection.on("requestSecurityMechanism", (options) => {
      return prompt(JSON.stringify(options));
    });

    await connection.on("logMessage", (severity, message) => {
      console.log(severity, message);
    });

    await connection.start();
    await connection.invoke("start");
  }

}

