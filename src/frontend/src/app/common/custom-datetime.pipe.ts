import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'customDateTime',
    standalone: true,
})
export class CustomDateTimePipe implements PipeTransform {
    transform(value: Date | undefined | null): string {
        if (value === null || value === undefined) {
            return '-';
        }

        var day = value.getDate().toString();
        var month = (value.getMonth() + 1).toString();
        var year = value.getFullYear().toString();
        var hour = value.getHours().toString();
        var minute = value.getMinutes().toString();
        var second = value.getSeconds().toString();

        if (day.length < 2) day = '0' + day;
        if (month.length < 2) month = '0' + month;
        if (hour.length < 2) hour = '0' + hour;
        if (minute.length < 2) minute = '0' + minute;
        if (second.length < 2) second = '0' + second;

        return `${day}.${month}.${year} ${hour}.${minute}.${second}`;
    }
}
