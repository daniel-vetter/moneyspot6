import { Injectable } from "@angular/core";
import { Subject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AppEvents {

    private sub = new Subject<AppEvent>();

    public emit(event: AppEvent) {
        this.sub.next(event);
    }

    public get events(): Observable<AppEvent> {
        return this.sub.asObservable();
    }
}

type AppEvent =
    { type: 'NewTransactionsSeenEvent' } |
    { type: 'TransactionSyncDone' };
