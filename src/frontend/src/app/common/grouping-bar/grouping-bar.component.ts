import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DropdownModule } from 'primeng/dropdown';

@Component({
  selector: 'app-grouping-bar',
  standalone: true,
  imports: [DropdownModule, FormsModule],
  templateUrl: './grouping-bar.component.html',
  styleUrl: './grouping-bar.component.scss'
})
export class GroupingBarComponent implements OnInit {

  constructor(private activatedRoute: ActivatedRoute, private router: Router) {
  }

  ngOnInit(): void {

    this.activatedRoute.queryParams.subscribe(x => {
      this.selectedGrouping = x["grouping"] ?? "Monthly";
    });
  }

  groupOptions: GroupingSelecteItem[] = [
    { name: "Keine", key: "None" },
    { name: "Monatlich", key: "Monthly" },
    { name: "Jährlich", key: "Yearly" },
  ]
  selectedGrouping: Grouping = "Monthly";

  async onGroupingChanged(event: any) {
    this.router.navigate([],
      {
        relativeTo: this.activatedRoute,
        queryParams: {
          grouping: this.selectedGrouping == "Monthly" ? undefined : this.selectedGrouping
        }, queryParamsHandling: "merge"
      });
  }

}

interface GroupingSelecteItem {
  name: string;
  key: Grouping
}

export type Grouping = "None" | "Monthly" | "Yearly"