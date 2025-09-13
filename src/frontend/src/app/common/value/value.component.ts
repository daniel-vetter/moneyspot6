import { Component, computed, input, Input, OnChanges, SimpleChanges } from '@angular/core';

@Component({
    selector: 'app-value',
    imports: [],
    templateUrl: './value.component.html',
    styleUrl: './value.component.scss'
})
export class ValueComponent {
    public value = input<number | undefined>()
    public size = input<string | undefined>()
    public color = input<'default' | 'reverse' | 'none'>('default')

    private format = new Intl.NumberFormat('de-de', { style: 'currency', currency: 'EUR' });

    class = computed(() => {
        const value = this.value();
        if (value === undefined) {
            return '';
        }

        const color = this.color();
        if (color === 'default') {
            if (value > 0) {
                return 'green';
            }
            if (value < 0) {
                return 'red';
            }
            return '';
        }

        if (color === 'reverse') {
            if (value > 0) {
                return 'red';
            }
            if (value < 0) {
                return 'green';
            }
            return '';
        }

        if (color === 'none') {
            return '';
        }

        throw new Error('Invalid color ' + color);
    });

    valueStr = computed(() => {
        const v = this.value();
        if (v === undefined) {
            return '-';
        } else {
            return this.format.format(v)
        }
    });
}
