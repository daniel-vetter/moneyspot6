import { Component, computed, model, Signal, signal, inject, OnInit, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';

@Component({
    selector: 'app-date-range-picker',
    imports: [DatePickerModule, FormsModule, ButtonModule, IconField, InputIcon],
    templateUrl: './date-range-picker.component.html',
    styleUrl: './date-range-picker.component.scss'
})
export class DateRangePickerComponent implements OnInit {

    showClearButton = input<boolean>(true);
    value = model<DateRange | undefined>(undefined);
    dateRange = signal<[Date, Date] | undefined>(undefined);
    resetButtonVisible = computed<boolean>(() => this.showClearButton() === true && this.dateRange() !== undefined);
    activatedRoute = inject(ActivatedRoute);
    router = inject(Router);


    ngOnInit(): void {
        this.activatedRoute.queryParams.subscribe(async (x) => {
            this.value.set(DateRange.parse(x['dateRange']));
            this.dateRange.set(this.value() ? [this.value()!.start, this.value()!.end] : undefined);
            await this.update();
        });
    }

    update() {
        const value = this.dateRange();
        if (value !== undefined && value[0] !== undefined && value[0] !== null && value[1] !== undefined && value[1] !== null) {
            const r = new DateRange(value[0], value[1]);
            this.value.set(r);
        } else {
            this.value.set(undefined);
        }

        this.router.navigate([], {
            relativeTo: this.activatedRoute,
            queryParams: {
                dateRange: this.value() ? this.value()!.toString() : null
            },
            queryParamsHandling: 'merge'
        });
    }

    onResetButtonClicked() {
        this.dateRange.set(undefined)
        this.value.set(undefined);

        this.router.navigate([], {
            relativeTo: this.activatedRoute,
            queryParams: {
                dateRange: this.value() ? this.value()!.toString() : null
            },
            queryParamsHandling: 'merge'
        });
    }
}


export class DateRange {
    constructor(public readonly start: Date, public readonly end: Date) {
    }

    public toString(): string {
        return `${this.start.getFullYear()}${this.toTwoDigits(this.start.getMonth() + 1)}${this.toTwoDigits(this.start.getDate())}${this.end.getFullYear()}${this.toTwoDigits(this.end.getMonth() + 1)}${this.toTwoDigits(this.end.getDate())}`;
    }

    private toTwoDigits(value: number): string {
        return value < 10 ? `0${value}` : `${value}`;
    }

    public static parse(value: string | undefined | null): DateRange | undefined {
        if (value === undefined || value === null) {
            return undefined;
        }

        if (value.length !== 16) {
            return undefined;
        }

        const startYear = parseInt(value.substring(0, 4));
        const startMonth = parseInt(value.substring(4, 6));
        const startDay = parseInt(value.substring(6, 8));
        const endYear = parseInt(value.substring(8, 12));
        const endMonth = parseInt(value.substring(12, 14));
        const endDay = parseInt(value.substring(14, 16));

        const startDate = new Date(startYear, startMonth - 1, startDay);
        const endDate = new Date(endYear, endMonth - 1, endDay);
        return new DateRange(startDate, endDate);
    }
}
