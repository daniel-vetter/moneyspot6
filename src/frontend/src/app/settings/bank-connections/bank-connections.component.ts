import { Component, OnInit, inject } from '@angular/core';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { DialogService } from 'primeng/dynamicdialog';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { BankConnectionClient, BankConnectionListResponse } from '../../server';
import { firstValueFrom } from 'rxjs';
import { BankConnectionDialogComponent } from './bank-connection-dialog/bank-connection-dialog.component';
import { DatePipe } from '@angular/common';

@Component({
    selector: 'app-bank-connections',
    imports: [PanelModule, TableModule, ButtonModule, TooltipModule, ConfirmDialogModule, ToastModule, DatePipe],
    providers: [DialogService, ConfirmationService, MessageService],
    templateUrl: './bank-connections.component.html',
    styleUrl: './bank-connections.component.scss',
    standalone: true
})
export class BankConnectionsComponent implements OnInit {
    private bankConnectionClient = inject(BankConnectionClient);
    private dialogService = inject(DialogService);
    private confirmationService = inject(ConfirmationService);
    private messageService = inject(MessageService);

    connections: BankConnectionListResponse[] = [];
    loading: boolean = false;

    async ngOnInit() {
        await this.loadConnections();
    }

    async loadConnections() {
        this.loading = true;
        try {
            const response = await firstValueFrom(this.bankConnectionClient.getAll());
            this.connections = response;
        } finally {
            this.loading = false;
        }
    }

    openCreateDialog() {
        const dialog = this.dialogService.open(BankConnectionDialogComponent, {
            data: { id: null },
            focusOnShow: false
        });

        dialog.onClose.subscribe(async (result) => {
            if (result === true) {
                await this.loadConnections();
                this.messageService.add({
                    severity: 'success',
                    summary: 'Verbindung erstellt',
                    detail: 'Die Bankverbindung wurde erfolgreich angelegt.'
                });
            }
        });
    }

    openEditDialog(connection: BankConnectionListResponse) {
        const dialog = this.dialogService.open(BankConnectionDialogComponent, {
            data: { id: connection.id },
            focusOnShow: false
        });

        dialog.onClose.subscribe(async (result) => {
            if (result === true) {
                await this.loadConnections();
                this.messageService.add({
                    severity: 'success',
                    summary: 'Verbindung aktualisiert',
                    detail: 'Die Bankverbindung wurde erfolgreich aktualisiert.'
                });
            }
        });
    }

    confirmDelete(connection: BankConnectionListResponse) {
        this.confirmationService.confirm({
            message: `Möchten Sie die Verbindung "${connection.name}" wirklich löschen? Alle zugehörigen Konten und Transaktionen werden ebenfalls gelöscht.`,
            header: 'Löschen bestätigen',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Ja, löschen',
            rejectLabel: 'Abbrechen',
            accept: async () => {
                await this.deleteConnection(connection.id);
            }
        });
    }

    async deleteConnection(id: number) {
        try {
            await firstValueFrom(this.bankConnectionClient.delete(id));
            await this.loadConnections();
            this.messageService.add({
                severity: 'success',
                summary: 'Verbindung gelöscht',
                detail: 'Die Bankverbindung wurde erfolgreich gelöscht.'
            });
        } catch (error) {
            this.messageService.add({
                severity: 'error',
                summary: 'Fehler',
                detail: 'Die Bankverbindung konnte nicht gelöscht werden.'
            });
        }
    }
}
