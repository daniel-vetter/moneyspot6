import { Component, OnInit } from '@angular/core';
import { DialogModule } from 'primeng/dialog';
import { GlobalErrorCallback } from './global-error-callback';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
    selector: 'app-global-error-handler-dialog',
    imports: [DialogModule],
    templateUrl: './global-error-handler-dialog.component.html',
    styleUrl: './global-error-handler-dialog.component.scss'
})
export class GlobalErrorHandlerDialogComponent implements OnInit {
    title: string = '';
    message: string = '';
    stack: string = '';
    src?: SafeResourceUrl;

    constructor(
        private globalErrorCallback: GlobalErrorCallback,
        private domSanitizer: DomSanitizer,
    ) {}

    visible = false;

    ngOnInit(): void {
        this.globalErrorCallback.set((error) => {
            this.title = 'An error occurred';
            this.message = JSON.stringify(error, Object.getOwnPropertyNames(error), 2);

            if (error.stack) {
                this.stack = error.stack;
            }

            if (error.response) {
                this.src = this.domSanitizer.bypassSecurityTrustResourceUrl('data:text/html;charset=utf-8,' + error.response);
            }

            this.visible = true;
        });
    }
}
