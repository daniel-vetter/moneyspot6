import { Component, OnInit } from '@angular/core';
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
declare var google: any;

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, ButtonModule, MenuComponent, GlobalErrorHandlerDialogComponent, ToastModule, TopBarComponent, ConfirmDialogModule],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss',
    providers: [ConfirmationService]
})
export class AppComponent implements OnInit {
    isLoggedIn = false;
    constructor(private authClient: AuthClient) { }

    async ngOnInit(): Promise<void> {
        Highcharts.setOptions(this.createOptions());
        HighchartsStock.setOptions(this.createOptions());

        const currentUserResponse = await lastValueFrom(this.authClient.getCurrent());

        if (!currentUserResponse.user) {
            google.accounts.id.initialize({
                client_id: '753503482461-u9og0m5ql1l5gcvl5jqk4vjmg61pongl.apps.googleusercontent.com',
                login_uri: window.location.protocol + "//" + window.location.host + "/api/Auth/Login",
                ux_mode: 'redirect'
            });
            google.accounts.id.renderButton(document.body, {});
        } else {
            this.isLoggedIn = true;
        }
    }

    private createOptions(): Highcharts.Options {
        return {
            colors: ['#058DC7', '#50B432', '#ED561B', '#DDDF00', '#24CBE5', '#64E572', '#FF9655', '#FFF263', '#6AF9C4'],
            accessibility: {
                enabled: false,
            },
            credits: {
                enabled: false,
            }
        }
    }
}
