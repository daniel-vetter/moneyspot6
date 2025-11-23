import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';

@Component({
    selector: 'app-inflation-adjustment-dialog',
    imports: [FormsModule, ButtonModule, DatePickerModule],
    templateUrl: './inflation-adjustment-dialog.component.html',
    styleUrl: './inflation-adjustment-dialog.component.scss',
    standalone: true
})
export class InflationAdjustmentDialogComponent {
    private dynamicDialogRef = inject(DynamicDialogRef);

    selectedDate: Date = new Date();

    constructor() {
        const dialogConfig = inject(DynamicDialogConfig);

        dialogConfig.modal = true;
        dialogConfig.width = "500px";
        dialogConfig.height = "600px";
        dialogConfig.header = "Inflationsanpassung";
    }

    onCancelClicked() {
        this.dynamicDialogRef.close();
    }

    onOkClicked() {
        this.dynamicDialogRef.close(this.selectedDate);
    }
}
