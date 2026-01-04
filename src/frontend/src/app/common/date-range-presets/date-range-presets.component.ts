import { Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DateRange } from '../date-range-picker/date-range-picker.component';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
    selector: 'app-date-range-presets',
    imports: [ButtonModule],
    templateUrl: './date-range-presets.component.html',
    styleUrl: './date-range-presets.component.scss'
})
export class DateRangePresetsComponent implements OnInit {

    buttons = signal<DateRangePresetButton[]>(DateRangePresetsComponent.getDefault());
    currentSelection = signal<DateRange | undefined>(undefined);


    activatedRoute = inject(ActivatedRoute);
    router = inject(Router);

    buttonsResolved = computed<DateRangePresetButtonResolved[]>(() => {
        const r: DateRangePresetButtonResolved[] = [];
        const buttons = this.buttons();
        const current = this.currentSelection();
        for (const b of buttons) {
            r.push({
                label: b.label,
                range: b.range,
                isSelected: (current !== undefined &&
                    b.range !== undefined &&
                    current.start.getTime() === b.range.start.getTime() &&
                    current.end.getTime() === b.range.end.getTime()) ||
                    current === undefined && b.range === undefined
            });
        }
        return r;
    });

    onClick(button: DateRangePresetButtonResolved) {
        this.router.navigate([], {
            relativeTo: this.activatedRoute,
            queryParams: {
                dateRange: button.range ? button.range.toString() : null
            },
            queryParamsHandling: 'merge'
        });
    }

    static getDefault(): DateRangePresetButton[] {
        return [
            new DateRangePresetButtonMonth(0),
            new DateRangePresetButtonMonth(-1),
            new DateRangePresetButtonMonth(-2),
            new DateRangePresetButtonMonthsBackgwards(3),
            new DateRangePresetButtonMonthsBackgwards(6),
            new DateRangePresetButtonMonthsBackgwards(12),
            new DateRangePresetButtonYear(0),
            new DateRangePresetButtonYear(-1),
            new DateRangePresetButtonYear(-2),
            new DateRangePresetButtonYearsBackgwards(5),
            new DateRangePresetButtonAll()
        ];
    }


    constructor() {
    }
    ngOnInit(): void {
        this.activatedRoute.queryParams.subscribe(async (x) => {
            this.currentSelection.set(DateRange.parse(x['dateRange']));
        });
    }
}


interface DateRangePresetButton {
    readonly label: string
    readonly range: DateRange | undefined;
}

interface DateRangePresetButtonResolved {
    readonly label: string
    readonly range: DateRange | undefined;
    readonly isSelected: boolean;
}

class DateRangePresetButtonMonthsBackgwards implements DateRangePresetButton {

    constructor(private readonly monthCount: number) { }
    get label() { return this.monthCount + " Monate" }

    get range(): DateRange | undefined {
        const start = new Date();
        start.setDate(start.getDate() + 1);
        start.setMonth(start.getMonth() - this.monthCount);
        start.setHours(0, 0, 0, 0);
        const end = new Date();
        end.setHours(0, 0, 0, 0);
        return new DateRange(start, end);
    }
}

class DateRangePresetButtonYearsBackgwards implements DateRangePresetButton {

    constructor(private readonly yearCount: number) { }
    get label() { return this.yearCount + " Jahre" }

    get range(): DateRange | undefined {
        const start = new Date();
        start.setFullYear(start.getFullYear() - (this.yearCount - 1));
        start.setMonth(0);
        start.setDate(1);
        start.setHours(0, 0, 0, 0);

        const end = new Date();
        end.setFullYear(end.getFullYear());
        end.setMonth(11);
        end.setDate(31);
        end.setHours(0, 0, 0, 0);
        return new DateRange(start, end);
    }
}


class DateRangePresetButtonMonth implements DateRangePresetButton {

    constructor(private readonly monthOffset: number) { }

    get label(): string {
        const now = new Date();
        const d = new Date(now.getFullYear(), now.getMonth() + this.monthOffset, 1, 0, 0, 0, 0);
        return d.toLocaleString('default', { month: 'long' }) + " " + d.getFullYear();
    }

    get range(): DateRange | undefined {
        const now = new Date();
        const start = new Date(now.getFullYear(), now.getMonth() + this.monthOffset, 1, 0, 0, 0, 0);
        const end = new Date(now.getFullYear(), now.getMonth() + this.monthOffset + 1, 0, 0, 0, 0, 0);
        return new DateRange(start, end);
    }
}

class DateRangePresetButtonYear implements DateRangePresetButton {

    constructor(private readonly currentYearOffset: number) { }

    get label(): string {
        return (new Date().getFullYear() + this.currentYearOffset).toString();
    }

    get range(): DateRange | undefined {
        const start = new Date();
        start.setFullYear(start.getFullYear() + this.currentYearOffset);
        start.setMonth(0);
        start.setDate(1);
        start.setHours(0, 0, 0, 0);
        const end = new Date();
        end.setFullYear(end.getFullYear() + this.currentYearOffset);
        end.setMonth(11);
        end.setDate(31);
        end.setHours(0, 0, 0, 0);
        return new DateRange(start, end);
    }
}


class DateRangePresetButtonAll implements DateRangePresetButton {

    label = "Alles";

    get range(): DateRange | undefined {
        return undefined;
    }
}





