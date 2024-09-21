import { AfterViewInit, Component, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TanDialogComponent } from './tan-dialog/tan-dialog.component';
import { PanelModule } from 'primeng/panel';
import { MessagesModule } from 'primeng/messages';

@Component({
  selector: 'app-account-sync',
  standalone: true,
  imports: [ButtonModule, DialogModule, InputTextModule, FormsModule, ProgressSpinnerModule, PanelModule, TanDialogComponent, MessagesModule],
  templateUrl: './account-sync.component.html',
  styleUrl: './account-sync.component.scss'
})
export class AccountSyncComponent {

  logMessages: LogMessage[] = [];
  isVisible = false;
  result?: { type: "Success", newTransactionCount: number } | { type: "Error", error: string } | { type: "Canceled" }

  @ViewChild(TanDialogComponent) tanDialog!: TanDialogComponent;

  async onSyncButtonClicked() {

    this.result = undefined;
    this.isVisible = true;

    const connection = new HubConnectionBuilder()
      .withUrl("/api/account-sync")
      .build();

    connection.on("requestTan", async (message: string) => {
      return { tan: await this.tanDialog.showDialog(message) }
    });

    connection.on("requestTanCanceled", async () => {
      this.tanDialog.cancelDialog();
    });

    connection.on("requestSecurityMechanism", (options) => {
      return prompt(JSON.stringify(options));
    });

    connection.on("requestSecurityMechanismCanceled", () => {
      throw Error("Not implemented");
    });

    await connection.on("logMessage", (severity: number, message: string) => {
      this.logMessages.push({
        severity: severity,
        message: message
      })

      console.log(severity, message);
    });

    await connection.start();
    const result = await connection.invoke("start");

    if (result.canceledByUser) {
      this.result = { type: "Canceled" }
    } else if (result.error === undefined || result.error === null) {
      this.result = { type: "Success", newTransactionCount: result.newTransactions.length }
    } else {
      this.result = { type: "Error", error: result.error }
    }
  }

  onCancelClicked() {
    this.isVisible = false;
  }

  onCloseClicked() {
    this.isVisible = false;
  }

}

interface LogMessage {
  severity: number,
  message: string
}
