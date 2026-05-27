import { computed, inject, Injectable, signal } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { EmailSyncStatusResponse, MailIntegrationClient } from '../server';

@Injectable({ providedIn: 'root' })
export class EmailSyncState {
    private mailIntegrationClient = inject(MailIntegrationClient);
    private _status = signal<EmailSyncStatusResponse | undefined>(undefined);

    status = this._status.asReadonly();
    hasFailedSync = computed(() => this._status()?.hasFailedSync ?? false);

    async init(): Promise<void> {
        await this.refresh();
    }

    async refresh(): Promise<void> {
        try {
            const status = await lastValueFrom(this.mailIntegrationClient.getSyncStatus());
            this._status.set(status);
        } catch {
            // Status is best-effort - keep previous value on failure
        }
    }
}
