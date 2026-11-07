import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UiAvatar } from './ui-avatar.component';
import { UiIcon } from './ui-icon.component';

export enum UiTone {
  Muted = 'muted',
  Warn = 'warn',
  Sky = 'sky',
  Ok = 'ok',
  Bad = 'bad',
}

export type BoardItem = {
  id: string;
  title: string;
  time: string;
  meta: string;
  link?: string | unknown[];
  queryParams?: Record<string, string>;
  linkLabel?: string;
};

export type BoardColumn = {
  key: string;
  label: string;
  tone: UiTone;
  items: BoardItem[];
};

@Component({
  selector: 'ui-board',
  imports: [RouterLink, UiAvatar, UiIcon],
  templateUrl: './ui-board.component.html',
  styleUrl: './ui-board.component.css',
})
export class UiBoard {
  readonly columns = input<BoardColumn[]>([]);
  readonly limit = input(6);
}
