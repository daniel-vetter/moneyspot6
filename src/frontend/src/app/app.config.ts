import { ApplicationConfig, ErrorHandler, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MessageService } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { provideHttpClient } from '@angular/common/http';
import { GlobalErrorHandler } from './global-error-handler-dialog/global-error-handler';
import { providePrimeNG } from 'primeng/config';
import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';
import { themes, ThemeId } from './common/theme.service';

registerLocaleData(localeDe)

const savedTheme = (localStorage.getItem('theme') as ThemeId) || 'light';
const themeConfig = themes.find(t => t.id === savedTheme) || themes.find(t => t.id === 'light')!;

export const appConfig: ApplicationConfig = {
    providers: [
        provideZoneChangeDetection({ eventCoalescing: true }),
        provideRouter(routes),
        provideHttpClient(),
        provideAnimationsAsync(),
        {
            provide: ErrorHandler,
            useClass: GlobalErrorHandler,
        },
        {
            provide: MessageService,
        },
        DialogService,
        providePrimeNG({
            ripple: true,
            theme: {
                preset: themeConfig.preset,
                options: {
                    darkModeSelector: '.dark-mode',
                    prefix: 'p',
                    cssLayer: false
                }
            }
        })
    ],
};
