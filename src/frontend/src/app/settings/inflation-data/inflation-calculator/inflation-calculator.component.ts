import {Component, inject, OnInit} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {ButtonModule} from 'primeng/button';
import {InputNumber} from 'primeng/inputnumber';
import {DecimalPipe} from '@angular/common';
import {lastValueFrom} from 'rxjs';
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

    inputValue: number = 100;
    inputFromDate: Date = new Date();
    inputToDate: Date = new Date();

    lastCalculatedInputValue?: number;
    lastCalculatedInputFromDate?: Date;
    lastCalculatedInputToDate?: Date;

    adjustedValue?: number;
    isCalculating: boolean = false;

    async ngOnInit() {
        await this.calculate();
    }

    async calculate() {
        this.isCalculating = true;
        try {
            const response = await lastValueFrom(
                this.inflationDataClient.calculateAdjustedValue(new CalculateAdjustedValueRequest({
                    value: this.inputValue,
                    fromYear: this.inputFromDate.getFullYear(),
                    fromMonth: this.inputFromDate.getMonth() + 1,
                    toYear: this.inputToDate.getFullYear(),
                    toMonth: this.inputToDate.getMonth() + 1
                }))
            );

            this.adjustedValue = this.check(response.adjustedValue);
            this.lastCalculatedInputValue = this.check(this.inputValue);
            this.lastCalculatedInputFromDate = this.check(this.inputFromDate);
            this.lastCalculatedInputToDate = this.check(this.inputToDate);
        } finally {
            this.isCalculating = false;
        }
    }

    isValid(): boolean {
        return this.inputValue !== undefined && this.inputValue !== null &&
               this.inputFromDate !== undefined && this.inputFromDate !== null &&
               this.inputToDate !== undefined && this.inputToDate !== null;
    }

    check<T>(input: T | null | undefined): T | undefined {
        if (input === null || input === undefined) return undefined;
        return input;
    }

    formatDate(date?: Date): string {
        if (date === undefined || date === null) {
            return '';
        }

        const months = ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni', 'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];
        return `${months[date.getMonth()]} ${date.getFullYear()}`;
    }
}
