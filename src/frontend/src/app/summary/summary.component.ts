import { Component, OnInit } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { HubConnectionBuilder } from "@microsoft/signalr";

@Component({
  selector: 'app-summary',
  standalone: true,
  imports: [ButtonModule],
  templateUrl: './summary.component.html',
  styleUrl: './summary.component.scss'
})
export class SummaryComponent implements OnInit {

  constructor() {


  }
  async ngOnInit(): Promise<void> {

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

