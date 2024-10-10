import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MenuComponent } from './menu/menu.component';
import { GlobalErrorHandlerDialogComponent } from './global-error-handler-dialog/global-error-handler-dialog.component';
import { ToastModule } from 'primeng/toast';
import { TopBarComponent } from './top-bar/top-bar.component';
import * as Highcharts from 'highcharts';
import * as HighchartsStock from 'highcharts/highstock';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterOutlet, ButtonModule, MenuComponent, GlobalErrorHandlerDialogComponent, ToastModule, TopBarComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
    constructor() { }

    async ngOnInit(): Promise<void> {
        Highcharts.setOptions(this.createOptions());
        HighchartsStock.setOptions(this.createOptions());
    }

    private createOptions(): Highcharts.Options {
        return {
            colors: ['#058DC7', '#50B432', '#ED561B', '#DDDF00', '#24CBE5', '#64E572', '#FF9655', '#FFF263', '#6AF9C4'],
            accessibility: {
                enabled: false,
            },
            chart: {
                backgroundColor: '#18181b',
            },
            xAxis: {
                labels: {
                    style: {
                        color: 'var(--text-color)',
                    },
                },
                lineColor: 'var(--gray-600)',
                tickColor: 'var(--gray-600)',
                minorGridLineColor: '#FF0000',
                gridLineColor: '#FF0000',
            },
            yAxis: {
                labels: {
                    style: {
                        color: 'var(--text-color)',
                    },
                },
                lineColor: 'var(--gray-600)',
                gridLineColor: 'var(--gray-600)',
            },
            title: {
                style: {
                    color: 'var(--text-color)',
                    font: 'bold 16px var(--font-family)',
                },
            },
            subtitle: {
                style: {
                    color: 'var(--text-color)',
                    font: 'bold 12px var(--font-family)',
                },
            },
            legend: {
                itemStyle: {
                    font: '9pt var(--font-family)',
                    color: 'var(--text-color)',
                },
                itemHoverStyle: {
                    color: 'var(--text-color)',
                },
            },
            credits: {
                enabled: false,
            },
        }
    }
}
