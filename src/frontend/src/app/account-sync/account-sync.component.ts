import { Component, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TanDialogComponent } from './tan-dialog/tan-dialog.component';
import { PanelModule } from 'primeng/panel';
import { MessagesModule } from 'primeng/messages';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { GlobalEvents } from '../common/global-events';
import { ProgressBarModule } from 'primeng/progressbar';

@Component({
    selector: 'app-account-sync',
    imports: [ButtonModule, DialogModule, InputTextModule, FormsModule, ProgressSpinnerModule, PanelModule, TanDialogComponent, MessagesModule, ToastModule, ProgressBarModule],
    templateUrl: './account-sync.component.html',
    styleUrl: './account-sync.component.scss'
})
export class AccountSyncComponent {
    private messageService = inject(MessageService);
    private globalEvents = inject(GlobalEvents);

    isVisible = false;
    logMessage = '';

    @ViewChild(TanDialogComponent) tanDialog!: TanDialogComponent;

    async onSyncButtonClicked() {
        this.isVisible = true;

        const connection = new HubConnectionBuilder().withUrl('/api/account-sync').build();

        connection.on('requestTan', async (message: string) => {
            return { tan: await this.tanDialog.showDialog(message) };
        });

        connection.on('requestTanCanceled', async () => {
            this.tanDialog.cancelDialog();
        });

        connection.on('requestSecurityMechanism', (options) => {
            return prompt(JSON.stringify(options));
        });

        connection.on('requestSecurityMechanismCanceled', () => {
            throw Error('Not implemented');
        });

        connection.on('logMessage', (severity, message) => {
            if (severity >= 1)
                this.logMessage = message;
        })

        await connection.start();
        const result = await connection.invoke('start');

        if (result.canceledByUser) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Der Vorgang wurde vom Benutzer abgebrochen.',
            });
        } else if (result.error === undefined || result.error === null) {
            let details = '';
            const count: number = result.newTransactions.length;
            if (count === 0) {
                details = 'Es wurden keine neuen Buchungen gefunden.';
            } else if (count === 1) {
                details = 'Es wurde eine neue Buchung gefunden.';
            } else {
                details = `Es wurden ${count} neue Buchungen gefunden.`;
            }
            this.messageService.add({
                severity: 'success',
                summary: 'Buchungen wurden erfolgreich geladen.',
                detail: details,
            });
        } else {
            this.messageService.add({
                severity: 'error',
                summary: 'Es ist eine Fehler aufgetreten.',
                detail: 'Buchungen konnten nicht geladen werden.',
            });
        }

        this.globalEvents.onAccountSyncDone.next();
        this.isVisible = false;
    }
}
