import {Component, inject, OnInit} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {ButtonModule} from 'primeng/button';
import {InputNumber} from 'primeng/inputnumber';
import {DecimalPipe} from '@angular/common';
import {lastValueFrom} from 'rxjs';
import {DynamicDialogRef} from 'primeng/dynamicdialog';
import {DatePickerModule} from 'primeng/datepicker';
import {CalculateAdjustedValueRequest, InflationDataClient} from "../../../server";

@Component({
    selector: 'app-inflation-calculator',
    imports: [FormsModule, ButtonModule, InputNumber, DecimalPipe, DatePickerModule],
    templateUrl: './inflation-calculator.component.html',
    styleUrl: './inflation-calculator.component.scss',
    standalone: true
})
export class InflationCalculatorComponent implements OnInit {
    private inflationDataClient = inject(InflationDataClient);
    private dialogRef = inject(DynamicDialogRef);

    value: number = 100;
    fromDate: Date = new Date();
    toDate: Date = new Date();

    adjustedValue: number | null = null;
    calculating: boolean = false;

    async ngOnInit() {
        await this.calculate();
    }

    async calculate() {
        this.calculating = true;
        try {
            const request = new CalculateAdjustedValueRequest({
                value: this.value,
                fromYear: this.fromDate.getFullYear(),
                fromMonth: this.fromDate.getMonth() + 1,
                toYear: this.toDate.getFullYear(),
                toMonth: this.toDate.getMonth() + 1
            });

            const response = await lastValueFrom(
                this.inflationDataClient.calculateAdjustedValue(request)
            );

            this.adjustedValue = response.adjustedValue;
        } finally {
            this.calculating = false;
        }
    }

    formatDate(date: Date): string {
        const months = ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni',
            'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];
        return `${months[date.getMonth()]} ${date.getFullYear()}`;
    }
}
