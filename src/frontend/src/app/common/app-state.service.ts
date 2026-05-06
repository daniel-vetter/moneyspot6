import { computed, inject, Injectable, signal } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { AppState, AppStateClient, CompleteFirstSetupRequest } from '../server';

@Injectable({ providedIn: 'root' })
export class AppStateService {
    private appStateClient = inject(AppStateClient);
    private _state = signal<AppState | undefined>(undefined);

    state = computed(() => {
        const state = this._state();
        if (!state) throw new Error('AppStateService.state accessed before init() ran');
        return state;
    });

    isFirstSetupDone = computed(() => this.state().isFirstSetupDone);

    async init(): Promise<void> {
        const state = await lastValueFrom(this.appStateClient.get());
        this._state.set(state);
    }

    async completeFirstSetup(addSampleData: boolean): Promise<void> {
        await lastValueFrom(this.appStateClient.completeFirstSetup(new CompleteFirstSetupRequest({ addSampleData })));
        await this.init();
    }
}
