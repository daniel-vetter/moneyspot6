import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'customDate',
    standalone: true,
})
export class CustomDatePipe implements PipeTransform {
    transform(value: Date | undefined | null): string {
        if (value === null || value === undefined) {
            return '-';
        }

        var day = value.getDate().toString();
        var month = (value.getMonth() + 1).toString();
        var year = value.getFullYear().toString();

        if (day.length < 2) day = '0' + day;
        if (month.length < 2) month = '0' + month;

        return `${day}.${month}.${year}`;
    }
}
