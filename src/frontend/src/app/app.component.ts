import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MenuComponent } from './menu/menu.component';
import { GlobalErrorHandlerDialogComponent } from './global-error-handler-dialog/global-error-handler-dialog.component';
import { ToastModule } from 'primeng/toast';
import { TopBarComponent } from './top-bar/top-bar.component';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { AuthClient } from './server';
import { lastValueFrom } from 'rxjs';
import { ParticlesBackgroundComponent } from './common/particles-background/particles-background.component';


@Component({
    selector: 'app-root',
    imports: [RouterOutlet, ButtonModule, MenuComponent, GlobalErrorHandlerDialogComponent, ToastModule, TopBarComponent, ConfirmDialogModule, ParticlesBackgroundComponent],
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
    }
}
