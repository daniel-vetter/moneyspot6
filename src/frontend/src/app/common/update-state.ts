import { computed, inject, Injectable, signal } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { SelfUpdateStatus, SystemClient } from '../server';

@Injectable({ providedIn: 'root' })
export class UpdateState {
    private systemClient = inject(SystemClient);
    private _status = signal<SelfUpdateStatus | undefined>(undefined);

    updateInProgress = false;
    status = this._status.asReadonly();
    isUpdateAvailable = computed(() => this._status()?.isUpdateAvailable ?? false);

    async init(): Promise<void> {
        await this.refresh();
    }

    async refresh(): Promise<void> {
        try {
            const status = await lastValueFrom(this.systemClient.getUpdateStatus());
            this._status.set(status);
        } catch {
            // Status is best-effort - keep previous value on failure
        }
    }
}
