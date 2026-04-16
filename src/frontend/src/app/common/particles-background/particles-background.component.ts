import { Component, OnInit, OnDestroy, ElementRef, inject, ViewChild } from '@angular/core';
import { ThemeService } from '../theme.service';
import { tsParticles, type Container } from '@tsparticles/engine';
import { loadSlim } from '@tsparticles/slim';

@Component({
    selector: 'app-particles-background',
    standalone: true,
    template: `<div #container></div>`,
    styles: [`
        :host {
            position: fixed;
            top: -300px;
            left: -300px;
            width: calc(100% + 600px);
            height: calc(100% + 600px);
            pointer-events: none;
            z-index: 10;
        }
        div {
            width: 100%;
            height: 100%;
            filter: blur(300px);
            opacity: 0.2;
        }
    `]
})
export class ParticlesBackgroundComponent implements OnInit, OnDestroy {
    private themeService = inject(ThemeService);
    private particles: Container | undefined;

    @ViewChild('container', { static: true }) containerRef!: ElementRef<HTMLElement>;

    async ngOnInit() {
        if (this.themeService.current !== 'neon') return;

        await loadSlim(tsParticles);

        this.particles = await tsParticles.load({
            element: this.containerRef.nativeElement,
            options: {
                fullScreen: { enable: false },
                background: { color: { value: 'transparent' } },
                fpsLimit: 120,
                detectRetina: true,
                particles: {
                    color: {
                        value: ['#80ff04', '#66cc03', '#00ddfa', '#4d9902', '#a855f7']
                    },
                    links: { enable: false },
                    move: {
                        enable: true,
                        speed: 1,
                        direction: 'top',
                        random: false,
                        straight: false,
                        outModes: {
                            default: 'out',
                            bottom: 'out',
                            left: 'out',
                            right: 'out',
                            top: 'out',
                        },
                    },
                    number: {
                        density: { enable: false },
                        value: 30,
                    },
                    opacity: {
                        value: { min: 0.4, max: 0.8 },
                    },
                    shape: { type: 'circle' },
                    size: {
                        value: { min: 300, max: 400 },
                        animation: {
                            enable: true,
                            speed: 100,
                            sync: false,
                            startValue: 'random',
                        },
                    },
                },
                motion: {
                    disable: true,
                    reduce: {
                        factor: 4,
                        value: true,
                    },
                },
            }
        });
    }

    ngOnDestroy() {
        this.particles?.destroy();
    }
}
