import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextModule } from 'primeng/inputtext';

@Component({
    selector: 'app-search-bar',
    standalone: true,
    imports: [ButtonModule, DropdownModule, FormsModule, InputTextModule],
    templateUrl: './search-bar.component.html',
    styleUrl: './search-bar.component.scss',
})
export class SearchBarComponent implements OnInit {
    searchText = '';
    get showClearButton() {
        return this.searchText != '';
    }

    constructor(
        private router: Router,
        private activatedRoute: ActivatedRoute,
    ) {}
    ngOnInit(): void {
        this.activatedRoute.queryParams.subscribe(async (x) => {
            this.searchText = x['search'] ? x['search'] : '';
        });
    }

    onSearchSubmit() {
        this.router.navigate([], {
            relativeTo: this.activatedRoute,
            queryParams: {
                search: this.searchText !== undefined && this.searchText.trim() !== '' ? this.searchText : undefined,
            },
            queryParamsHandling: 'merge',
        });
    }

    onResetButtonClicked() {
        this.searchText = '';
        this.router.navigate([], {
            relativeTo: this.activatedRoute,
            queryParams: {
                search: undefined,
            },
            queryParamsHandling: 'merge',
        });
    }
}
