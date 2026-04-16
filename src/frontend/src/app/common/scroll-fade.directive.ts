import { Directive, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';

@Directive({
    selector: '[appScrollFade]',
    standalone: true,
    host: {
        '[class.scrolled-bottom]': 'isAtBottom',
        '[class.scrolled-top]': 'isAtTop'
    }
})
export class ScrollFadeDirective implements AfterViewInit, OnDestroy {
    isAtBottom = false;
    isAtTop = true;
    private scrollContainer: Element | null = null;
    private listener = () => this.checkScroll();

    constructor(private el: ElementRef<HTMLElement>) {}

    ngAfterViewInit() {
        this.scrollContainer = this.el.nativeElement.querySelector('.p-scrollpanel-content');
        if (this.scrollContainer) {
            this.scrollContainer.addEventListener('scroll', this.listener);
            setTimeout(() => this.checkScroll());
        }
    }

    ngOnDestroy() {
        this.scrollContainer?.removeEventListener('scroll', this.listener);
    }

    private checkScroll() {
        if (!this.scrollContainer) return;
        const { scrollTop, scrollHeight, clientHeight } = this.scrollContainer;
        this.isAtBottom = scrollTop + clientHeight >= scrollHeight - 10;
        this.isAtTop = scrollTop <= 10;
    }
}
