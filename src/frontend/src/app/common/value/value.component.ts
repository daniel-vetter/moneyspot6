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
        if (this.value() === undefined) {
            return '';
        } else {
            if (this.color() === 'default') {
                if (this.value()! > 0) {
                    return 'green';
                }
                if (this.value()! < 0) {
                    return 'red';
                }
            }

            if (this.color() === 'reverse') {
                if (this.value()! > 0) {
                    return 'red';
                }
                if (this.value()! < 0) {
                    return 'green';
                }
            }
        }
        throw Error("unreachable");
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
