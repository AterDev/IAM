import { Component, OnInit } from '@angular/core';
import { Router, NavigationEnd, ActivatedRoute } from '@angular/router';
import { filter, distinctUntilChanged } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';

export interface Breadcrumb {
  label: string;
  url: string;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, TranslateModule],
  templateUrl: './breadcrumb.html',
  styleUrl: './breadcrumb.scss'
})
export class BreadcrumbComponent implements OnInit {
  breadcrumbs: Breadcrumb[] = [];

  constructor(
    private router: Router,
    private activatedRoute: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.breadcrumbs = this.buildBreadcrumbs(this.activatedRoute.root);
      });

    // Initial breadcrumb
    this.breadcrumbs = this.buildBreadcrumbs(this.activatedRoute.root);
  }

  /**
   * Build breadcrumbs from route data
   */
  private buildBreadcrumbs(
    route: ActivatedRoute,
    url: string = '',
    breadcrumbs: Breadcrumb[] = []
  ): Breadcrumb[] {
    // Get the child routes
    const children: ActivatedRoute[] = route.children;

    // Return if there are no more children
    if (children.length === 0) {
      return breadcrumbs;
    }

    // Iterate over each child
    for (const child of children) {
      // Verify primary route
      if (child.outlet !== 'primary') {
        continue;
      }

      // Get the route's URL segment
      const routeURL: string = child.snapshot.url
        .map(segment => segment.path)
        .join('/');

      // Append route URL to URL
      if (routeURL) {
        url += `/${routeURL}`;
      }

      // Add breadcrumb if route data has breadcrumb label
      const label = child.snapshot.data['breadcrumb'];
      if (label) {
        breadcrumbs.push({ label, url });
      }

      // Recursive call
      return this.buildBreadcrumbs(child, url, breadcrumbs);
    }

    return breadcrumbs;
  }
}
