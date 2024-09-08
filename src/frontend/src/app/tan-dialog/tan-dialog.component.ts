import { Component, OnDestroy, OnInit } from '@angular/core';
import { DialogModule } from 'primeng/dialog';

@Component({
  selector: 'app-tan-dialog',
  standalone: true,
  imports: [DialogModule],
  templateUrl: './tan-dialog.component.html',
  styleUrl: './tan-dialog.component.scss'
})
export class TanDialogComponent implements OnInit, OnDestroy {
  visible = false;

  constructor() {}
  
  ngOnInit(): void {
  }

  ngOnDestroy(): void {
  }
}
