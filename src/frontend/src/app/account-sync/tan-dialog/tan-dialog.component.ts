import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';

@Component({
    selector: 'app-tan-dialog',
    imports: [ButtonModule, DialogModule, FormsModule, InputTextModule],
    templateUrl: './tan-dialog.component.html',
    styleUrl: './tan-dialog.component.scss'
})
export class TanDialogComponent {
    visible = false;
    message = '';
    tan = '';
    tanResolver: ((tan: string | undefined) => void) | undefined;

    onTanOkClicked() {
        if (this.tanResolver === undefined) {
            throw Error('No tan resolver set');
        }
        this.visible = false;
        this.tanResolver(this.tan);
    }

    onTanCancelClicked() {
        if (this.tanResolver === undefined) {
            throw Error('No tan resolver set');
        }
        this.visible = false;
        this.tan = '';
        this.tanResolver(undefined);
    }

    cancelDialog() {
        if (this.tanResolver !== undefined) {
            this.tanResolver(undefined);
            this.visible = false;
        }
    }

    async showDialog(message: string): Promise<string | undefined> {
        this.tan = '';
        this.message = message;
        this.visible = true;

        const tan = await new Promise<string | undefined>((resolve) => {
            this.tanResolver = resolve;
        });
        console.log('TAN: ' + tan);
        return tan;
    }
}
