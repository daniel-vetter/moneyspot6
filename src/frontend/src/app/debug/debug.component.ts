import { Component, OnDestroy, OnInit } from '@angular/core';
import { DebugClient, RunningProcessResponse } from '../server';
import { ButtonModule } from 'primeng/button';
import { lastValueFrom } from 'rxjs';

@Component({
  selector: 'app-debug',
  standalone: true,
  imports: [ButtonModule],
  templateUrl: './debug.component.html',
  styleUrl: './debug.component.scss'
})
export class DebugComponent implements OnDestroy, OnInit {
  runningProcesses: RunningProcessResponse[] = [];
  intervall?: any;

  constructor(private debugClient: DebugClient) { }
  
  async OnReprocessTransactionpParsingClicked() {
    await lastValueFrom(this.debugClient.reprocessTransactionParsing());
  }

  ngOnInit(): void {
    this.intervall = setInterval(async () => {
      this.runningProcesses = await lastValueFrom(this.debugClient.getRunningProcesses());
    }, 1000);
  }

  ngOnDestroy(): void {
    if (this.intervall !== undefined) {
      clearInterval(this.intervall);
    }
  }
}
