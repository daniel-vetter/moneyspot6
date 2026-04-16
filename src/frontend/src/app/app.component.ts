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
import { ThemeService } from './common/theme.service';


@Component({
    selector: 'app-root',
    imports: [RouterOutlet, ButtonModule, MenuComponent, GlobalErrorHandlerDialogComponent, ToastModule, TopBarComponent, ConfirmDialogModule],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss',
    providers: [ConfirmationService]
})
export class AppComponent implements OnInit {
    private authClient = inject(AuthClient);
    private themeService = inject(ThemeService);

    isLoggedIn: boolean = false;

    async ngOnInit(): Promise<void> {
        const currentUser = await lastValueFrom(this.authClient.getUserDetails());
        if (currentUser === undefined || currentUser === null) {
            window.location.href = '/api/Auth/Login';
            return;
        }
        this.isLoggedIn = true;

        const opts = this.createOptions();
        Highcharts.setOptions(opts);
        HighchartsStock.setOptions(opts);
    }

    private createOptions(): Highcharts.Options {
        const dark = this.themeService.isDark;
        const themeId = this.themeService.current;
        const colorsByTheme: Record<string, string[]> = {
            emerald: ['#10b981', '#06b6d4', '#8b5cf6', '#f59e0b', '#ef4444', '#ec4899', '#6366f1', '#14b8a6', '#f97316'],
            neon: ['#80ff04', '#00ddfa', '#a855f7', '#ff7407', '#f43f5e', '#ec4899', '#6366f1', '#21dc52', '#facc15'],
            light: ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ef4444', '#06b6d4', '#ec4899', '#6366f1', '#f97316'],
        };
        const colors = colorsByTheme[themeId] ?? colorsByTheme['emerald'];
        return {
            colors,
            accessibility: {
                enabled: false,
            },
            credits: {
                enabled: false,
            },
            chart: {
                backgroundColor: 'transparent',
            },
            xAxis: {
                labels: {
                    style: {
                        color: dark ? '#94a3b8' : '#64748b',
                    },
                },
                lineColor: dark ? '#334155' : '#cbd5e1',
                tickColor: dark ? '#334155' : '#cbd5e1',
                gridLineColor: dark ? '#1e293b' : '#e2e8f0',
            },
            yAxis: {
                labels: {
                    style: {
                        color: dark ? '#94a3b8' : '#64748b',
                    },
                },
                lineColor: dark ? '#334155' : '#cbd5e1',
                gridLineColor: dark ? '#1e293b' : '#e2e8f0',
            },
            title: {
                style: {
                    color: dark ? '#e2e8f0' : '#1e293b',
                },
            },
            legend: {
                itemStyle: {
                    color: dark ? '#94a3b8' : '#64748b',
                },
                itemHoverStyle: {
                    color: dark ? '#e2e8f0' : '#1e293b',
                },
            },
            tooltip: {
                backgroundColor: dark ? '#1e293b' : '#ffffff',
                borderColor: dark ? '#334155' : '#cbd5e1',
                style: {
                    color: dark ? '#e2e8f0' : '#1e293b',
                },
            },
            navigator: {
                maskFill: 'rgba(16, 185, 129, 0.1)',
                outlineColor: dark ? '#334155' : '#cbd5e1',
                series: {
                    color: '#10b981',
                    lineColor: '#10b981',
                },
            },
            scrollbar: {
                barBackgroundColor: dark ? '#334155' : '#cbd5e1',
                trackBackgroundColor: dark ? '#0f172a' : '#f1f5f9',
                trackBorderColor: dark ? '#1e293b' : '#e2e8f0',
            },
            rangeSelector: {
                buttonTheme: {
                    fill: dark ? '#1e293b' : '#f1f5f9',
                    stroke: dark ? '#334155' : '#cbd5e1',
                    style: {
                        color: dark ? '#94a3b8' : '#64748b',
                    },
                    states: {
                        hover: {
                            fill: dark ? '#334155' : '#e2e8f0',
                            style: {
                                color: dark ? '#e2e8f0' : '#1e293b',
                            },
                        },
                        select: {
                            fill: '#10b981',
                            style: {
                                color: '#ffffff',
                            },
                        },
                    },
                },
                inputStyle: {
                    color: dark ? '#e2e8f0' : '#1e293b',
                    backgroundColor: dark ? '#1e293b' : '#ffffff',
                },
                labelStyle: {
                    color: dark ? '#94a3b8' : '#64748b',
                },
            },
        }
    }
}
