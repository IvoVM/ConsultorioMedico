import { TestBed } from '@angular/core/testing';
import { UiButton } from './ui';

describe('UiButton', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiButton],
    }).compileComponents();
  });

  it('creates', () => {
    const fixture = TestBed.createComponent(UiButton);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
