import { Injectable } from "@angular/core";

@Injectable({ providedIn: "root" })
export class GlobalErrorCallback {

    callback?: (error: any) => void;

    public set(callback: (error: any) => void) {
        this.callback = callback;
    }

    public call(error: any) {
        if (this.callback === undefined) {
            throw Error("No error handler callback defined");
        }
        this.callback(error);
    }
}