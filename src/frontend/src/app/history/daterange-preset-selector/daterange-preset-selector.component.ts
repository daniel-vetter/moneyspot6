import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { ButtonGroupModule } from 'primeng/buttongroup';

@Component({
    selector: 'app-daterange-preset-selector',
    imports: [ButtonGroupModule, ButtonModule],
    templateUrl: './daterange-preset-selector.component.html',
    styleUrl: './daterange-preset-selector.component.scss'
})
export class DaterangePresetSelectorComponent implements OnChanges {
    currentYear: string = '';
    lastYear: string = '';
    current = '';

    @Input() range: [Date, Date] = [new Date(), new Date()];
    @Output() rangeChange: EventEmitter<[Date, Date]> = new EventEmitter();

    constructor() {
        this.currentYear = new Date().getFullYear().toString();
        this.lastYear = (new Date().getFullYear() - 1).toString();
    }
    ngOnChanges(changes: SimpleChanges): void {
        this.updateHighlight();
    }

    onCurrentYearClicked() {
        this.range = this.getCurrentYearRange();
        this.rangeChange.emit(this.range);
        this.updateHighlight();
    }

    onLastYearClicked() {
        this.range = this.getLastYearRange();
        this.rangeChange.emit(this.range);
        this.updateHighlight();
    }

    on5YearClicked() {
        this.range = this.get5YearRange();
        this.rangeChange.emit(this.range);
        this.updateHighlight();
    }

    onAllClicked() {
        this.range = this.getAllRange();
        this.rangeChange.emit(this.range);
        this.updateHighlight();
    }

    updateHighlight() {
        this.current = '';
        if (this.isEqual(this.getCurrentYearRange(), this.range)) this.current = 'current';
        if (this.isEqual(this.getLastYearRange(), this.range)) this.current = 'last';
        if (this.isEqual(this.get5YearRange(), this.range)) this.current = '5year';
        if (this.isEqual(this.getAllRange(), this.range)) this.current = 'all';
    }

    isEqual(v1: [Date, Date], v2: [Date, Date]): boolean {
        if (v1[0] === null || undefined) return false;
        if (v1[1] === null || undefined) return false;
        if (v2[0] === null || undefined) return false;
        if (v2[1] === null || undefined) return false;

        return v1[0].valueOf() == v2[0].valueOf() && v1[1].valueOf() == v2[1].valueOf();
    }

    getCurrentYearRange(): [Date, Date] {
        const start = new Date();
        start.setMonth(0);
        start.setDate(1);
        start.setHours(0, 0, 0, 0);

        const end = new Date();
        end.setMonth(11);
        end.setDate(1);
        end.setHours(0, 0, 0, 0);

        return [start, end];
    }

    getLastYearRange(): [Date, Date] {
        const start = new Date();
        start.setFullYear(start.getFullYear() - 1);
        start.setMonth(0);
        start.setDate(1);
        start.setHours(0, 0, 0, 0);

        const end = new Date();
        end.setFullYear(end.getFullYear() - 1);
        end.setMonth(11);
        end.setDate(1);
        end.setHours(0, 0, 0, 0);

        return [start, end];
    }

    get5YearRange(): [Date, Date] {
        const start = new Date();
        start.setFullYear(start.getFullYear() - 4);
        start.setMonth(0);
        start.setDate(1);
        start.setHours(0, 0, 0, 0);

        const end = new Date();
        end.setFullYear(end.getFullYear());
        end.setMonth(11);
        end.setDate(1);
        end.setHours(0, 0, 0, 0);

        return [start, end];
    }

    getAllRange(): [Date, Date] {
        const start = new Date();
        start.setFullYear(2009);
        start.setMonth(0);
        start.setDate(1);
        start.setHours(0, 0, 0, 0);

        const end = new Date();
        end.setFullYear(end.getFullYear());
        end.setMonth(0);
        end.setDate(1);
        end.setHours(0, 0, 0, 0);

        return [start, end];
    }
}
