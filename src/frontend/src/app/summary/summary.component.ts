import { Component } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { AccountSyncClient } from '../server';
import { lastValueFrom } from 'rxjs';

@Component({
  selector: 'app-summary',
  standalone: true,
  imports: [ButtonModule],
  templateUrl: './summary.component.html',
  styleUrl: './summary.component.scss'
})
export class SummaryComponent {

  constructor(private accountSyncClient: AccountSyncClient) {
  }

  async onSyncButtonClicked() {
    await lastValueFrom(this.accountSyncClient.get());
  }

}

