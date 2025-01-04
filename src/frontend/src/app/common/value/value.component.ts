import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';

@Component({
    selector: 'app-value',
    imports: [],
    templateUrl: './value.component.html',
    styleUrl: './value.component.scss'
})
export class ValueComponent implements OnChanges {
    @Input() value: number | undefined;
    @Input() size: string | undefined;
    @Input() color: 'default' | 'reverse' | 'none' = 'default';

    format = new Intl.NumberFormat('de-de', { style: 'currency', currency: 'EUR' });

    valueStr = '-';
    class = '';

    ngOnChanges(changes: SimpleChanges): void {
        if (this.value === undefined) {
            this.valueStr = '-';
            this.class = '';
        } else {
            this.valueStr = this.format.format(this.value / 100);

            let v = this.value;

            this.class = '';
            if (this.color === 'default') {
                if (v > 0) {
                    this.class = 'green';
                }
                if (v < 0) {
                    this.class = 'red';
                }
            }

            if (this.color === 'reverse') {
                if (v < 0) {
                    this.class = 'green';
                }
                if (v > 0) {
                    this.class = 'red';
                }
            }
        }
    }
}
