import { Component, OnInit, inject } from '@angular/core';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { GlobalErrorCallback } from './global-error-callback';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
    selector: 'app-global-error-handler-dialog',
    imports: [DialogModule, ButtonModule],
    templateUrl: './global-error-handler-dialog.component.html',
    styleUrl: './global-error-handler-dialog.component.scss'
})
export class GlobalErrorHandlerDialogComponent implements OnInit {
    private globalErrorCallback = inject(GlobalErrorCallback);
    private domSanitizer = inject(DomSanitizer);

    errorName: string = '';
    errorMessage: string = '';
    stack: string = '';
    httpStatus?: number;
    src?: SafeResourceUrl;
    rawError: string = '';

    visible = false;

    ngOnInit(): void {
        this.globalErrorCallback.set((error) => {
            this.reset();
            this.parseError(error);
            this.visible = true;
        });
    }

    private reset(): void {
        this.errorName = '';
        this.errorMessage = '';
        this.stack = '';
        this.httpStatus = undefined;
        this.src = undefined;
        this.rawError = '';
    }

    private parseError(error: any): void {
        this.rawError = JSON.stringify(error, Object.getOwnPropertyNames(error), 2);

        // HTTP Error (from NSwag client)
        if (error.status !== undefined && error.response !== undefined) {
            this.httpStatus = error.status;
            this.errorName = this.getHttpErrorTitle(error.status);
            this.errorMessage = error.message || 'Ein Serverfehler ist aufgetreten.';

            if (error.response) {
                this.src = this.domSanitizer.bypassSecurityTrustResourceUrl(
                    'data:text/html;charset=utf-8,' + encodeURIComponent(error.response)
                );
            }
        }
        // Standard Error
        else if (error instanceof Error) {
            this.errorName = error.name || 'Fehler';
            this.errorMessage = error.message || 'Ein unbekannter Fehler ist aufgetreten.';
            if (error.stack) {
                this.stack = error.stack;
            }
        }
        // Unknown error type
        else {
            this.errorName = 'Fehler';
            this.errorMessage = typeof error === 'string' ? error : 'Ein unbekannter Fehler ist aufgetreten.';
        }
    }

    private getHttpErrorTitle(status: number): string {
        switch (status) {
            case 400: return 'Ungültige Anfrage';
            case 401: return 'Nicht autorisiert';
            case 403: return 'Zugriff verweigert';
            case 404: return 'Nicht gefunden';
            case 500: return 'Serverfehler';
            case 502: return 'Bad Gateway';
            case 503: return 'Service nicht verfügbar';
            default: return `HTTP Fehler ${status}`;
        }
    }

    reload(): void {
        window.location.reload();
    }
}
