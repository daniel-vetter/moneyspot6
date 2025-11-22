import { Component, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InflationDataClient, UpdateDefaultRateRequest } from '../../../server';
import { lastValueFrom } from 'rxjs';

@Component({
    selector: 'app-default-rate-dialog',
    imports: [ButtonModule, ReactiveFormsModule, InputNumberModule],
    templateUrl: './default-rate-dialog.component.html',
    styleUrl: './default-rate-dialog.component.scss'
})
export class DefaultRateDialogComponent implements OnInit {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    private inflationDataClient = inject(InflationDataClient);

    form = new FormGroup({
        defaultRate: new FormControl<number | null>(null, { nonNullable: false, validators: [Validators.required] })
    });

    async ngOnInit() {
        const currentRate = this.dialogConfig.data?.defaultRate;
        if (currentRate !== undefined && currentRate !== null) {
            this.form.setValue({
                defaultRate: currentRate
            });
        }
    }

    onCancelClicked() {
        this.dialogRef.close();
    }

    async onSubmit() {
        if (!this.form.valid) {
            return;
        }

        this.form.disable();

        try {
            const defaultRate = this.form.value.defaultRate;

            if (defaultRate === null || defaultRate === undefined) {
                return;
            }

            const request = new UpdateDefaultRateRequest({
                defaultRate: defaultRate
            });

            await lastValueFrom(this.inflationDataClient.updateDefaultRate(request));
        } catch (error) {
            console.error(error);
            return;
        } finally {
            this.form.enable();
        }

        this.dialogRef.close(true);
    }
}
