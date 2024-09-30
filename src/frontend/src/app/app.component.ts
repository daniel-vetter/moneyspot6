import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MenuComponent } from './menu/menu.component';
import { GlobalErrorHandlerDialogComponent } from "./global-error-handler-dialog/global-error-handler-dialog.component";
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { TopBarComponent } from "./top-bar/top-bar.component";

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ButtonModule, MenuComponent, GlobalErrorHandlerDialogComponent, ToastModule, TopBarComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  
  constructor() {
  }

  async ngOnInit(): Promise<void> {
  }
}
