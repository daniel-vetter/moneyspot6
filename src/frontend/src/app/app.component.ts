import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MenuComponent } from './menu/menu.component';
import { GlobalErrorHandlerDialogComponent } from './global-error-handler-dialog/global-error-handler-dialog.component';
import { ToastModule } from 'primeng/toast';
import { TopBarComponent } from './top-bar/top-bar.component';
import * as Highcharts from 'highcharts';
import * as HighchartsStock from 'highcharts/highstock';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { AuthClient } from './server';
import { lastValueFrom } from 'rxjs';


@Component({
    selector: 'app-root',
    imports: [RouterOutlet, ButtonModule, MenuComponent, GlobalErrorHandlerDialogComponent, ToastModule, TopBarComponent, ConfirmDialogModule],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss',
    providers: [ConfirmationService]
})
export class AppComponent implements OnInit {
    private authClient = inject(AuthClient);

    isLoggedIn: boolean = false;

    async ngOnInit(): Promise<void> {
        const currentUser = await lastValueFrom(this.authClient.getUserDetails());
        if (currentUser === undefined || currentUser === null) {
            window.location.href = '/api/Auth/Login';
            return;
        }
        this.isLoggedIn = true;
        Highcharts.setOptions(this.createOptions());
        HighchartsStock.setOptions(this.createOptions());
    }

    private createOptions(): Highcharts.Options {
        return {
            colors: ['#058DC7', '#50B432', '#ED561B', '#DDDF00', '#24CBE5', '#64E572', '#FF9655', '#FFF263', '#6AF9C4'],
            accessibility: {
                enabled: false,
            },
            credits: {
                enabled: false,
            },
            /*
            chart: {
                backgroundColor: '#18181b',
            },
            xAxis: {
                labels: {
                    style: {
                        color: 'var(--p-text-color)',
                    },
                },
                lineColor: 'var(--p-gray-600)',
                tickColor: 'var(--p-gray-600)',
                minorGridLineColor: '#FF0000',
                gridLineColor: '#FF0000',
            },
            yAxis: {
                labels: {
                    style: {
                        color: 'var(--p-text-color)',
                    },
                },
                lineColor: 'var(--p-gray-600)',
                gridLineColor: 'var(--p-gray-600)',
            },
            title: {
                style: {
                    color: 'var(--p-text-color)',
                    font: 'bold 16px var(--p-font-family)',
                },
            },
            subtitle: {
                style: {
                    color: 'var(--p-text-color)',
                    font: 'bold 12px var(--p-font-family)',
                },
            },
            legend: {
                itemStyle: {
                    font: '9pt var(--p-font-family)',
                    color: 'var(--p-text-color)',
                },
                itemHoverStyle: {
                    color: 'var(--p-text-color)',
                },
            }
            */
        }
    }
}
