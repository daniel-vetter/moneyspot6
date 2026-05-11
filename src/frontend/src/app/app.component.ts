import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MenuComponent } from './menu/menu.component';
import { GlobalErrorHandlerDialogComponent } from './global-error-handler-dialog/global-error-handler-dialog.component';
import { ToastModule } from 'primeng/toast';
import { TopBarComponent } from './top-bar/top-bar.component';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ParticlesBackgroundComponent } from './common/particles-background/particles-background.component';
import { AppStateService } from './common/app-state.service';
import { CurrentUserService } from './common/current-user.service';
import { WelcomeScreenComponent } from './welcome-screen/welcome-screen.component';


@Component({
    selector: 'app-root',
    imports: [RouterOutlet, ButtonModule, MenuComponent, GlobalErrorHandlerDialogComponent, ToastModule, TopBarComponent, ConfirmDialogModule, ParticlesBackgroundComponent, WelcomeScreenComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
    private currentUserService = inject(CurrentUserService);
    appStateService = inject(AppStateService);

    isLoggedIn: boolean = false;

    async ngOnInit(): Promise<void> {
        if (!await this.currentUserService.init()) {
            window.location.href = '/api/Auth/Login';
            return;
        }
        await this.appStateService.init();
        this.isLoggedIn = true;
    }
}
