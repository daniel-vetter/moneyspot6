import { Injectable } from "@angular/core";
import { Subject } from "rxjs";

@Injectable({ providedIn: "root" })
export class GlobalEvents {
    public readonly onAccountSyncDone: Subject<void> = new Subject<void>();   
}