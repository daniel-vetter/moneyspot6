import { Injectable } from '@angular/core';
import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

export type ThemeId = 'emerald' | 'neon' | 'light';

interface ThemeConfig {
    id: ThemeId;
    label: string;
    icon: string;
    darkMode: boolean;
    preset: any;
}

export const themes: ThemeConfig[] = [
    {
        id: 'light', label: 'Light', icon: 'pi pi-sun', darkMode: false,
        preset: definePreset(Aura, {
            semantic: {
                primary: {
                    50: '{blue.50}', 100: '{blue.100}', 200: '{blue.200}', 300: '{blue.300}',
                    400: '{blue.400}', 500: '{blue.500}', 600: '{blue.600}', 700: '{blue.700}',
                    800: '{blue.800}', 900: '{blue.900}', 950: '{blue.950}'
                },
                colorScheme: {
                    light: {
                        content: { borderColor: '#bebebe' },
                        formField: { borderColor: '#bebebe' },
                    }
                }
            }
        })
    },
    {
        id: 'neon', label: 'Neon', icon: 'pi pi-bolt', darkMode: true,
        preset: definePreset(Aura, {
            semantic: {
                primary: {
                    50: '#f0ffe0', 100: '#ddffc2', 200: '#bbff85', 300: '#99ff47',
                    400: '#80ff04', 500: '#80ff04', 600: '#66cc03', 700: '#4d9902',
                    800: '#336602', 900: '#1a3301', 950: '#0d1a00'
                },
                colorScheme: {
                    dark: {
                        primary: {
                            color: '#80ff04',
                            contrastColor: '#050505',
                            hoverColor: '#99ff47',
                            activeColor: '#66cc03',
                        },
                        surface: {
                            0: '#ffffff', 50: '{slate.50}', 100: '{slate.100}', 200: '{slate.200}',
                            300: '{slate.300}', 400: '{slate.400}', 500: '{slate.500}', 600: '#272d36',
                            700: '#1b2028', 800: '#13171d', 900: '#0b0e12', 950: '#050607'
                        }
                    }
                }
            }
        })
    },
    {
        id: 'emerald', label: 'Emerald', icon: 'pi pi-moon', darkMode: true,
        preset: definePreset(Aura, {
            semantic: {
                primary: {
                    50: '{emerald.50}', 100: '{emerald.100}', 200: '{emerald.200}', 300: '{emerald.300}',
                    400: '{emerald.400}', 500: '{emerald.500}', 600: '{emerald.600}', 700: '{emerald.700}',
                    800: '{emerald.800}', 900: '{emerald.900}', 950: '{emerald.950}'
                },
                colorScheme: {
                    dark: {
                        surface: {
                            0: '#ffffff', 50: '{slate.50}', 100: '{slate.100}', 200: '{slate.200}',
                            300: '{slate.300}', 400: '{slate.400}', 500: '{slate.500}', 600: '{slate.600}',
                            700: '{slate.700}', 800: '{slate.800}', 900: '{slate.900}', 950: '{slate.950}'
                        }
                    }
                }
            }
        })
    },
];

const STORAGE_KEY = 'theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {

    get current(): ThemeId {
        return (localStorage.getItem(STORAGE_KEY) as ThemeId) || 'emerald';
    }

    get currentConfig(): ThemeConfig {
        return themes.find(t => t.id === this.current)!;
    }

    get isDark(): boolean {
        return this.currentConfig.darkMode;
    }

    get allThemes() {
        return themes;
    }

    setTheme(id: ThemeId) {
        localStorage.setItem(STORAGE_KEY, id);
        window.location.reload();
    }
}
