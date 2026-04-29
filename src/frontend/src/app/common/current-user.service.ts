import { computed, inject, Injectable, signal } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { AuthClient, UserDetails } from '../server';

@Injectable({ providedIn: 'root' })
export class CurrentUserService {
    private authClient = inject(AuthClient);
    private _details = signal<UserDetails | undefined>(undefined);

    details = computed(() => {
        const details = this._details();
        if (!details) throw new Error('CurrentUserService.details accessed before init() ran');
        return details;
    });

    async init(): Promise<boolean> {
        const details = await lastValueFrom(this.authClient.getUserDetails());
        this._details.set(details ?? undefined);
        return !!details;
    }
}
