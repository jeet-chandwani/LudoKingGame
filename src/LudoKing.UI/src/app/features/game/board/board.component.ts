import {
  Component, Input, OnChanges, ViewChild,
  ElementRef, AfterViewInit, SimpleChanges
} from '@angular/core';
import { GameState, ValidMove, TokenState } from '../../../shared/models/game.models';
import {
  BOARD_SIZE, CELL, COLOR_HEX, YARD_SQUARE,
  HOME_CENTER_SQUARE, HOME_STRETCH_START,
  COLOR_START_SQUARES, isHomeStretchSquare
} from '../../../shared/constants/board-constants';

@Component({
  selector: 'app-board',
  standalone: false,
  template: `
    <canvas #canvas [width]="size" [height]="size"
      (click)="onCanvasClick($event)"
      style="border:2px solid #333;display:block;cursor:pointer">
    </canvas>`,
})
export class BoardComponent implements AfterViewInit, OnChanges {
  @ViewChild('canvas') canvasRef!: ElementRef<HTMLCanvasElement>;
  @Input() state: GameState | null = null;
  @Input() validMoves: ValidMove[] = [];
  @Input() myUserId = '';

  readonly size = BOARD_SIZE;

  ngAfterViewInit(): void { this.draw(); }
  ngOnChanges(_: SimpleChanges): void { if (this.canvasRef) this.draw(); }

  onCanvasClick(e: MouseEvent): void {
    // Token selection handled by game-page component via validMoves input
  }

  private get ctx(): CanvasRenderingContext2D {
    return this.canvasRef.nativeElement.getContext('2d')!;
  }

  private draw(): void {
    const ctx = this.ctx;
    ctx.clearRect(0, 0, this.size, this.size);
    this.drawGrid(ctx);
    this.drawColoredZones(ctx);
    this.drawSafeMarkers(ctx);
    this.drawHomeCenter(ctx);
    if (this.state) this.drawTokens(ctx);
    if (this.validMoves.length) this.drawValidMoveHighlights(ctx);
  }

  private drawGrid(ctx: CanvasRenderingContext2D): void {
    ctx.strokeStyle = '#bbb';
    ctx.lineWidth = 0.5;
    for (let i = 0; i <= 15; i++) {
      ctx.beginPath(); ctx.moveTo(i * CELL, 0); ctx.lineTo(i * CELL, BOARD_SIZE); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(0, i * CELL); ctx.lineTo(BOARD_SIZE, i * CELL); ctx.stroke();
    }
  }

  private drawColoredZones(ctx: CanvasRenderingContext2D): void {
    const zones: [string, number, number][] = [
      ['Red',    0, 0], ['Blue',  9, 0],
      ['Green',  9, 9], ['Yellow', 0, 9],
    ];
    zones.forEach(([color, col, row]) => {
      ctx.fillStyle = COLOR_HEX[color] + '55';
      ctx.fillRect(col * CELL, row * CELL, 6 * CELL, 6 * CELL);
      // Yard (2×2 inner)
      ctx.fillStyle = COLOR_HEX[color] + 'cc';
      ctx.fillRect((col + 1) * CELL, (row + 1) * CELL, 4 * CELL, 4 * CELL);
    });
    // Home stretch columns
    ctx.fillStyle = '#e53935aa'; ctx.fillRect(6 * CELL, 7 * CELL, CELL, 5 * CELL); // Red
    ctx.fillStyle = '#1e88e5aa'; ctx.fillRect(7 * CELL, 1 * CELL, CELL, 5 * CELL); // Blue (col)
    ctx.fillStyle = '#43a047aa'; ctx.fillRect(8 * CELL, 7 * CELL, CELL, 5 * CELL); // Green
    ctx.fillStyle = '#fdd835aa'; ctx.fillRect(7 * CELL, 9 * CELL, CELL, 5 * CELL); // Yellow (row)
  }

  private drawSafeMarkers(ctx: CanvasRenderingContext2D): void {
    ctx.fillStyle = '#388e3c';
    ctx.font = `${CELL * 0.6}px Material Icons`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    // Mark safe squares with a star glyph
    [8, 13, 21, 26, 34, 39, 47].forEach(sq => {
      const [x, y] = this.outerSquareToXY(sq);
      ctx.fillText('★', x + CELL / 2, y + CELL / 2);
    });
  }

  private drawHomeCenter(ctx: CanvasRenderingContext2D): void {
    ctx.fillStyle = '#9e9e9e44';
    ctx.fillRect(6 * CELL, 6 * CELL, 3 * CELL, 3 * CELL);
    ctx.fillStyle = '#333';
    ctx.font = `bold ${CELL * 0.4}px Roboto`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('HOME', 7.5 * CELL, 7.5 * CELL);
  }

  private drawTokens(ctx: CanvasRenderingContext2D): void {
    this.state!.players.forEach(player => {
      player.tokens.forEach((token, _) => {
        if (token.isHome) return;
        const [x, y] = this.tokenToXY(token, player.color);
        const r = CELL * 0.35;
        ctx.beginPath();
        ctx.arc(x, y, r, 0, Math.PI * 2);
        ctx.fillStyle = COLOR_HEX[player.color] ?? '#888';
        ctx.fill();
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 2;
        ctx.stroke();
        // Token index label
        ctx.fillStyle = '#fff';
        ctx.font = `bold ${CELL * 0.3}px Roboto`;
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(String(token.index + 1), x, y);
      });
    });
  }

  private drawValidMoveHighlights(ctx: CanvasRenderingContext2D): void {
    const me = this.state?.players.find(p => p.userId === this.myUserId);
    if (!me) return;
    this.validMoves.forEach(m => {
      const token = me.tokens.find(t => t.index === m.tokenIndex);
      if (!token) return;
      const [x, y] = this.tokenToXY(token, me.color);
      ctx.beginPath();
      ctx.arc(x, y, CELL * 0.42, 0, Math.PI * 2);
      ctx.strokeStyle = m.wouldCut ? '#f44336' : '#4caf50';
      ctx.lineWidth = 3;
      ctx.setLineDash([4, 4]);
      ctx.stroke();
      ctx.setLineDash([]);
    });
  }

  private tokenToXY(token: TokenState, color: string): [number, number] {
    if (token.square === YARD_SQUARE) return this.yardXY(color);
    if (token.square === HOME_CENTER_SQUARE) return [7.5 * CELL, 7.5 * CELL];
    if (isHomeStretchSquare(token.square)) return this.homeStretchXY(token.square, color);
    return this.outerSquareToXY(token.square).map((v, i) => v + (i === 0 ? CELL / 2 : CELL / 2)) as [number, number];
  }

  private outerSquareToXY(square: number): [number, number] {
    // Map outer track squares 0–51 to board col/row (top-left corner of cell)
    const path = this.buildOuterPath();
    const [col, row] = path[square] ?? [0, 0];
    return [col * CELL, row * CELL];
  }

  private homeStretchXY(square: number, color: string): [number, number] {
    const base = HOME_STRETCH_START[color];
    const step = square - base; // 0–4
    const cx = 7.5 * CELL, cy = 7.5 * CELL;
    if (color === 'Red')    return [cx - (step + 1) * CELL, cy];
    if (color === 'Blue')   return [cx, cy - (step + 1) * CELL];
    if (color === 'Green')  return [cx + (step + 1) * CELL, cy];
    return [cx, cy + (step + 1) * CELL]; // Yellow
  }

  private yardXY(color: string): [number, number] {
    // Yard center for each color
    if (color === 'Red')    return [3 * CELL, 3 * CELL];
    if (color === 'Blue')   return [12 * CELL, 3 * CELL];
    if (color === 'Green')  return [12 * CELL, 12 * CELL];
    return [3 * CELL, 12 * CELL]; // Yellow
  }

  // Outer track CCW starting at Red start (square 0 = col 6, row 14)
  private buildOuterPath(): [number, number][] {
    const path: [number, number][] = [];
    // Bottom col 6, rows 14→9 (6 steps: 0..5)
    for (let r = 14; r >= 9; r--) path.push([6, r]);
    // Left row 8, cols 0→5 (6 steps: 6..11)
    for (let c = 0; c <= 5; c++) path.push([c, 8]);
    // Up col 0..5 on row 7... actually standard Ludo outer is:
    // We'll use a simplified mapping consistent with BoardConstants
    // Top row 6, cols 0→5 (6 steps: 12..17... )
    for (let c = 0; c <= 5; c++) path.push([c, 6]);
    // Up col 6, rows 5→0 (6 steps: 18..23)
    for (let r = 5; r >= 0; r--) path.push([6, r]);
    // Right row 0, cols 7→14 but skip center col (24..29)
    for (let c = 7; c <= 12; c++) path.push([c, 0]);
    // Down col 14, rows 0→5 (30..35)
    for (let r = 1; r <= 6; r++) path.push([14, r]);
    // right row 8, cols 9→14 (36..41)
    for (let c = 9; c <= 14; c++) path.push([c, 8]);
    // Down col 8, rows 9→14 (42..47)
    for (let r = 9; r <= 14; r++) path.push([8, r]);
    // bottom row 14, cols 7→8  (48..51) — 4 more
    for (let c = 7; c <= 8; c++) path.push([c, 14]);
    return path.slice(0, 52);
  }
}
