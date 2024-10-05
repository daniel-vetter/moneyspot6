import { APP_INITIALIZER, ApplicationConfig, ErrorHandler, provideZoneChangeDetection, isDevMode } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideAnimations } from '@angular/platform-browser/animations';
import { MessageService, PrimeNGConfig } from 'primeng/api';
import { provideHttpClient } from '@angular/common/http';
import { GlobalErrorHandler } from './global-error-handler-dialog/global-error-handler';
import { provideServiceWorker } from '@angular/service-worker';

const initializeAppFactory = (primeConfig: PrimeNGConfig) => () => {
    primeConfig.ripple = true;
};

export const appConfig: ApplicationConfig = {
    providers: [
        provideZoneChangeDetection({ eventCoalescing: true }),
        provideRouter(routes),
        provideHttpClient(),
        provideAnimations(),
        {
            provide: APP_INITIALIZER,
            useFactory: initializeAppFactory,
            deps: [PrimeNGConfig],
            multi: true,
        },
        {
            provide: ErrorHandler,
            useClass: GlobalErrorHandler,
        },
        {
            provide: MessageService,
        },
        provideServiceWorker('ngsw-worker.js', {
            enabled: !isDevMode(),
            registrationStrategy: 'registerWhenStable:30000',
        }),
    ],
};
