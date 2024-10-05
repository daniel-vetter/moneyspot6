import { ErrorHandler, Injectable, NgZone } from '@angular/core';
import { GlobalErrorCallback } from './global-error-callback';

@Injectable({ providedIn: 'root' })
export class GlobalErrorHandler implements ErrorHandler {
    constructor(
        private globalErrorCallback: GlobalErrorCallback,
        private zone: NgZone,
    ) {}

    handleError(error: any) {
        console.error('Error from global error handler', error);

        this.zone.run(() => {
            this.globalErrorCallback.call(error);
        });
    }
}
