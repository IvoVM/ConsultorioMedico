import { AfterViewInit, Component, DestroyRef, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicalService, SignatureStampDto, errorMessage } from 'sdk';
import { FechaPipe, ToastService, UiButton, UiField, UiHelp, UiPassword, UiSkeleton } from 'ui';
import { firstValueFrom } from 'rxjs';
import { Stamp } from './stamp';

const ink = '#1d2433';
// The server takes up to 300 KB; a cropped signature in JPEG is far below that.
const maxBytes = 300 * 1024;

// The professional's stamp and handwritten signature. The stamp comes from their file (name, specialty,
// license); the signature they draw here or upload as a picture. Both show on the pages they sign from now on.
@Component({
  imports: [FormsModule, FechaPipe, UiButton, UiField, UiHelp, UiPassword, UiSkeleton, Stamp],
  templateUrl: './signature.html',
})
export class SignaturePage implements AfterViewInit {
  private readonly clinical = inject(ClinicalService);
  private readonly toasts = inject(ToastService);
  private readonly pad = viewChild<ElementRef<HTMLCanvasElement>>('pad');
  readonly stamp = signal<SignatureStampDto | null>(null);
  readonly image = signal<string | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly removing = signal(false);
  readonly error = signal('');
  readonly drawn = signal(false);
  password = '';
  private drawing = false;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.setImage(null));
    void this.load();
  }

  ngAfterViewInit() {
    this.clear();
  }

  clear() {
    const canvas = this.pad()?.nativeElement;
    const context = canvas?.getContext('2d');
    if (!canvas || !context) return;
    context.fillStyle = '#ffffff';
    context.fillRect(0, 0, canvas.width, canvas.height);
    this.drawn.set(false);
  }

  start(event: PointerEvent) {
    const context = this.pad()?.nativeElement.getContext('2d');
    if (!context) return;
    (event.target as HTMLElement).setPointerCapture(event.pointerId);
    this.drawing = true;
    const [x, y] = this.point(event);
    context.strokeStyle = ink;
    context.lineWidth = 3;
    context.lineCap = 'round';
    context.lineJoin = 'round';
    context.beginPath();
    context.moveTo(x, y);
    context.lineTo(x + 0.1, y + 0.1);
    context.stroke();
    this.drawn.set(true);
  }

  move(event: PointerEvent) {
    if (!this.drawing) return;
    const context = this.pad()?.nativeElement.getContext('2d');
    if (!context) return;
    const [x, y] = this.point(event);
    context.lineTo(x, y);
    context.stroke();
  }

  end() {
    this.drawing = false;
  }

  // A scanned signature goes on the white pad, scaled to fit; it can then be saved like a drawn one.
  async upload(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (!['image/png', 'image/jpeg'].includes(file.type)) {
      this.error.set('La firma tiene que ser una imagen PNG o JPG.');
      return;
    }
    const canvas = this.pad()?.nativeElement;
    const context = canvas?.getContext('2d');
    if (!canvas || !context) return;
    try {
      const bitmap = await createImageBitmap(file);
      this.clear();
      const scale = Math.min(canvas.width / bitmap.width, canvas.height / bitmap.height, 1);
      const width = bitmap.width * scale;
      const height = bitmap.height * scale;
      context.drawImage(bitmap, (canvas.width - width) / 2, (canvas.height - height) / 2, width, height);
      bitmap.close();
      this.drawn.set(true);
      this.error.set('');
    } catch {
      this.error.set('No se pudo leer la imagen.');
    }
  }

  async save() {
    const canvas = this.pad()?.nativeElement;
    if (!canvas || !this.drawn() || !this.password) return;
    const content = cropped(canvas);
    if (!content) {
      this.error.set('El recuadro está vacío. Dibujá o subí tu firma.');
      return;
    }
    if (content.length * 0.75 > maxBytes) {
      this.error.set('La firma pesa demasiado. Probá con una imagen más chica.');
      return;
    }
    this.saving.set(true);
    this.error.set('');
    try {
      await this.apply(await firstValueFrom(this.clinical.saveSignature({ contentBase64: content, password: this.password })));
      this.password = '';
      this.clear();
      this.toasts.success('Firma guardada', 'Va a aparecer con tu sello en los próximos folios que firmes.');
    } catch (reason) {
      this.error.set(errorMessage(reason));
    } finally {
      this.saving.set(false);
    }
  }

  async remove() {
    this.removing.set(true);
    this.error.set('');
    try {
      await this.apply(await firstValueFrom(this.clinical.removeSignature()));
      this.toasts.success('Firma quitada', 'Los folios que ya firmaste la conservan.');
    } catch (reason) {
      this.error.set(errorMessage(reason));
    } finally {
      this.removing.set(false);
    }
  }

  private async load() {
    try {
      await this.apply(await firstValueFrom(this.clinical.mySignature()));
    } catch (reason) {
      this.error.set(errorMessage(reason));
    } finally {
      this.loading.set(false);
    }
  }

  private async apply(stamp: SignatureStampDto) {
    this.stamp.set(stamp);
    this.setImage(stamp.hasImage ? URL.createObjectURL(await firstValueFrom(this.clinical.mySignatureImage())) : null);
  }

  private setImage(url: string | null) {
    const previous = this.image();
    if (previous) URL.revokeObjectURL(previous);
    this.image.set(url);
  }

  private point(event: PointerEvent): [number, number] {
    const canvas = event.target as HTMLCanvasElement;
    const box = canvas.getBoundingClientRect();
    return [((event.clientX - box.left) * canvas.width) / box.width, ((event.clientY - box.top) * canvas.height) / box.height];
  }
}

// The signature cut to what was drawn, with a small margin, as a JPEG on white in base64. Null if nothing was drawn.
function cropped(canvas: HTMLCanvasElement): string | null {
  const context = canvas.getContext('2d');
  if (!context) return null;
  const { width, height } = canvas;
  const pixels = context.getImageData(0, 0, width, height).data;
  let left = width;
  let right = -1;
  let top = height;
  let bottom = -1;
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const i = (y * width + x) * 4;
      if (pixels[i] < 220 || pixels[i + 1] < 220 || pixels[i + 2] < 220) {
        if (x < left) left = x;
        if (x > right) right = x;
        if (y < top) top = y;
        if (y > bottom) bottom = y;
      }
    }
  }
  if (right < 0) return null;
  const margin = 8;
  left = Math.max(0, left - margin);
  top = Math.max(0, top - margin);
  const cropWidth = Math.max(80, Math.min(width, right + margin) - left);
  const cropHeight = Math.max(30, Math.min(height, bottom + margin) - top);
  const out = document.createElement('canvas');
  out.width = cropWidth;
  out.height = cropHeight;
  const target = out.getContext('2d');
  if (!target) return null;
  target.fillStyle = '#ffffff';
  target.fillRect(0, 0, cropWidth, cropHeight);
  target.drawImage(canvas, left, top, cropWidth, cropHeight, 0, 0, cropWidth, cropHeight);
  return out.toDataURL('image/jpeg', 0.9).split(',')[1] ?? null;
}
