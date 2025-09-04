import { ApplicationConfig, ErrorHandler, provideZoneChangeDetection, isDevMode } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MessageService } from 'primeng/api';
import { provideHttpClient } from '@angular/common/http';
import { GlobalErrorHandler } from './global-error-handler-dialog/global-error-handler';
import { provideServiceWorker } from '@angular/service-worker';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';
import Lara from '@primeng/themes/lara';
import Nora from '@primeng/themes/nora';
import { definePreset } from '@primeng/themes';
import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';

registerLocaleData(localeDe)

const MyPreset = definePreset(Nora, {
    semantic: {
        primary: {
            50: '{blue.50}',
            100: '{blue.100}',
            200: '{blue.200}',
            300: '{blue.300}',
            400: '{blue.400}',
            500: '{blue.500}',
            600: '{blue.600}',
            700: '{blue.700}',
            800: '{blue.800}',
            900: '{blue.900}',
            950: '{blue.950}'
        },
        colorScheme: {
            light: {
                surface: {
                    0: '#ffffff',
                    50: '{zinc.50}',
                    100: '{zinc.100}',
                    200: '{zinc.200}',
                    300: '{zinc.300}',
                    400: '{zinc.400}',
                    500: '{zinc.500}',
                    600: '{zinc.600}',
                    700: '{zinc.700}',
                    800: '{zinc.800}',
                    900: '{zinc.900}',
                    950: '{zinc.950}'
                },
                navigation: {
                    item: {
                        focusBackground: '{surface.200}',
                        focusColor: "{primary.hover.color}",
                        activeBackground: '{primary.color}',
                        activeColor: "{primary.contrast.color}",
                    }
                }
            }
        }
    }
});

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
        providePrimeNG({
            ripple: true,
            theme: {
                preset: MyPreset,
                options: {
                    darkModeSelector: '.dark-mode',
                    refix: 'p',
                    cssLayer: false
                }
            }
        })
    ],
};
