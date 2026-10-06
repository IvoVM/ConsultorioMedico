import { Component, input } from '@angular/core';

@Component({
  selector: 'ui-icon',
  host: { class: 'inline-flex shrink-0' },
  templateUrl: './ui-icon.component.html',
  styles: `
    .ms {
      display: inline-block;
      overflow: hidden;
      width: 1em;
      height: 1em;
      font-family: 'Material Symbols Outlined';
      font-size: inherit;
      font-weight: normal;
      font-style: normal;
      line-height: 1;
      letter-spacing: normal;
      text-transform: none;
      white-space: nowrap;
      word-wrap: normal;
      direction: ltr;
      font-feature-settings: 'liga';
      -webkit-font-smoothing: antialiased;
      user-select: none;
    }
  `,
})
export class UiIcon {
  readonly name = input.required<string>();
  readonly size = input(20);
}
