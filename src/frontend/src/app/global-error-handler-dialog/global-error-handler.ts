import { ErrorHandler, Injectable, NgZone, inject } from '@angular/core';
import { GlobalErrorCallback } from './global-error-callback';

@Injectable({ providedIn: 'root' })
export class GlobalErrorHandler implements ErrorHandler {
    private globalErrorCallback = inject(GlobalErrorCallback);
    private zone = inject(NgZone);


    handleError(error: any) {
        console.error('Error from global error handler', error);

        this.zone.run(() => {
            this.globalErrorCallback.call(error);
        });
    }
}
