// utils/map-renderer.js - Undertale 像素RPG风格地图渲染引擎
//
// 核心设计思路（Undertale 风格重构）：
// 1. 纯色平面填充 + 粗黑像素边框（3px硬边）
// 2. 建筑 = 纯色矩形 + 黑色粗描边 + 像素门窗点阵
// 3. 树木 = 几何像素树冠（三角/菱形）+ 粗树干
// 4. 路径 = 虚线像素化路径（Undertale 大地图风格）
// 5. 无渐变、无透明度叠加、无噪声抖动 — 纯粹像素美学
//
// 风格特征：
// - 黑白为主调，绿色草地 + 红色灵魂点缀
// - 图像渲染禁用抗锯齿（imageSmoothingEnabled = false）
// - 字体使用等宽像素字体
// - 猫咪标记 = 白色像素爱心 ♥ + 脉冲方块

const app = getApp();

class MapRenderer {
  constructor() {
    this.mapData = app.globalData.mapData;
    this.cats = app.globalData.cats || [];
  }

  /**
   * 主渲染入口
   */
  render(ctx, camX, camY, viewW, viewH) {
    // ★ 关键！禁用抗锯齿以保证像素硬边
    ctx.imageSmoothingEnabled = false;

    if (!this.mapData) {
      this.renderEmpty(ctx, viewW, viewH);
      return;
    }

    // 1. 背景 - 纯色暗绿草地（Undertale 大地图色调）
    this.renderBackground(ctx, camX, camY, viewW, viewH);

    // 2. 地面像素网格纹理（可选：微妙的方格网）
    this.renderGridOverlay(ctx, camX, camY, viewW, viewH);

    // 3. 路径/道路 - 虚线像素路径
    this.renderPaths(ctx, camX, camY, viewW, viewH);

    // 4. 建筑 - 纯色填充 + 粗黑边框
    this.renderBuildings(ctx, camX, camY, viewW, viewH);

    // 5. 树木 - 几何像素树
    this.renderTrees(ctx, camX, camY, viewW, viewH);

    // 6. 猫咪标记点 - 白色爱心 ♥
    this.renderCatSpots(ctx, camX, camY, viewW, viewH);
  }

  // ==================== 背景 ====================
  renderBackground(ctx, camX, camY, w, h) {
    // Undertale 大地图：纯色暗绿草地
    // 像素级色块交替（模拟不同草地区域）
    const tileSize = 32; // 像素瓦片大小
    ctx.fillStyle = '#1A3A1A'; // 深草绿底色
    ctx.fillRect(0, 0, w, h);

    // 用深浅交替的瓦片模拟 Undertale 大地图的棋盘格草地
    for (let tx = Math.floor(camX / tileSize) * tileSize; tx < camX + w; tx += tileSize) {
      for (let ty = Math.floor(camY / tileSize) * tileSize; ty < camY + h; ty += tileSize) {
        const col = Math.floor(tx / tileSize);
        const row = Math.floor(ty / tileSize);
        // 棋盘格：每两个瓦片交替一次颜色
        if ((col + row) % 2 === 0) {
          ctx.fillStyle = '#1E3F1E'; // 略浅的草地
        } else {
          ctx.fillStyle = '#163216'; // 略深的草地
        }
        const sx = tx - camX;
        const sy = ty - camY;
        ctx.fillRect(sx, sy, tileSize, tileSize);
      }
    }
  }

  renderGridOverlay(ctx, camX, camY, w, h) {
    // 微妙的白色像素网格线（Undertale 某些场景有的地面纹理）
    ctx.save();
    ctx.globalAlpha = 0.04;
    ctx.strokeStyle = '#FFFFFF';
    ctx.lineWidth = 1;
    const tileSize = 32;
    for (let tx = Math.floor(camX / tileSize) * tileSize; tx < camX + w; tx += tileSize) {
      const sx = tx - camX;
      ctx.beginPath();
      ctx.moveTo(sx, 0);
      ctx.lineTo(sx, h);
      ctx.stroke();
    }
    for (let ty = Math.floor(camY / tileSize) * tileSize; ty < camY + h; ty += tileSize) {
      const sy = ty - camY;
      ctx.beginPath();
      ctx.moveTo(0, sy);
      ctx.lineTo(w, sy);
      ctx.stroke();
    }
    ctx.restore();
  }

  // ==================== 路径 ====================
  renderPaths(ctx, camX, camY, w, h) {
    const paths = this.mapData.paths || [];
    paths.forEach(path => {
      if (!path.points || path.points.length < 2) return;
      ctx.save();

      // Undertale 大地图路径：土黄色实线 + 黑色描边
      const pathWidth = path.width || 14;

      // 先画黑色底层（更宽 = 描边效果）
      ctx.strokeStyle = '#000000';
      ctx.lineWidth = pathWidth + 4;
      ctx.lineCap = 'butt';
      ctx.lineJoin = 'miter';
      ctx.beginPath();
      for (let i = 0; i < path.points.length; i++) {
        const pt = path.points[i];
        const sx = pt[0] - camX;
        const sy = pt[1] - camY;
        if (i === 0) ctx.moveTo(sx, sy);
        else ctx.lineTo(sx, sy);
      }
      ctx.stroke();

      // 再画土黄色上层
      ctx.strokeStyle = '#C8A040';
      ctx.lineWidth = pathWidth;
      ctx.beginPath();
      for (let i = 0; i < path.points.length; i++) {
        const pt = path.points[i];
        const sx = pt[0] - camX;
        const sy = pt[1] - camY;
        if (i === 0) ctx.moveTo(sx, sy);
        else ctx.lineTo(sx, sy);
      }
      ctx.stroke();

      // 路径虚线装饰（Undertale 大地图的路径边缘虚线）
      ctx.strokeStyle = '#000000';
      ctx.lineWidth = 2;
      ctx.setLineDash([8, 12]);
      ctx.beginPath();
      for (let i = 0; i < path.points.length; i++) {
        const pt = path.points[i];
        const sx = pt[0] - camX;
        const sy = pt[1] - camY;
        if (i === 0) ctx.moveTo(sx, sy);
        else ctx.lineTo(sx, sy);
      }
      ctx.stroke();
      ctx.setLineDash([]);

      ctx.restore();
    });
  }

  // ==================== 建筑渲染 ====================
  renderBuildings(ctx, camX, camY, w, h) {
    const buildings = this.mapData.buildings || [];
    buildings.forEach(building => {
      const sx = building.x - camX;
      const sy = building.y - camY;
      const bw = building.w;
      const bh = building.h;

      // 视口裁剪
      if (sx + bw < -20 || sx > w + 20 || sy + bh < -20 || sy > h + 20) return;

      // Undertale 风格建筑渲染
      this.renderPixelBuilding(ctx, sx, sy, bw, bh, building);

      // 建筑标签（像素字体）
      if (building.name) {
        this.renderPixelLabel(ctx, sx, sy, bw, bh, building.name);
      }
    });
  }

  /**
   * 像素建筑渲染 — Undertale 风格
   * - 纯色填充 + 3px 黑色边框
   * - 像素门窗（小黑色方块模拟窗户）
   * - 白色像素高光线（顶部+左侧高光模拟光照）
   */
  renderPixelBuilding(ctx, x, y, w, h, building) {
    const fillColor = building.color || '#8B8B8B';
    const isDark = this.isDarkColor(fillColor);

    // === 黑色阴影（右下偏移 3px）===
    ctx.fillStyle = '#000000';
    ctx.fillRect(x + 3, y + 3, w, h);

    // === 纯色填充 ===
    ctx.fillStyle = fillColor;
    ctx.fillRect(x, y, w, h);

    // === 粗黑色边框（3px）===
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 3;
    ctx.strokeRect(x, y, w, h);

    // === 像素门窗 ===
    this.renderPixelWindows(ctx, x, y, w, h, isDark);

    // === 白色高光线（Undertale 风格的顶部/左侧微高光） ===
    ctx.strokeStyle = 'rgba(255,255,255,0.25)';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(x + 2, y + 2);
    ctx.lineTo(x + w - 2, y + 2); // 顶部高光
    ctx.moveTo(x + 2, y + 2);
    ctx.lineTo(x + 2, y + h - 2); // 左侧高光
    ctx.stroke();
  }

  /**
   * 像素窗户 — 在建筑面上绘制小方块窗户
   */
  renderPixelWindows(ctx, x, y, w, h, isDark) {
    const winW = 10;
    const winH = 12;
    const winGapX = 22;
    const winGapY = 24;
    const marginX = 16;
    const marginY = 20;

    const cols = Math.max(1, Math.floor((w - marginX * 2) / winGapX));
    const rows = Math.max(1, Math.floor((h - marginY * 2) / winGapY));

    // 窗户颜色
    ctx.fillStyle = isDark ? '#FFFFFF' : '#1A1A2E';
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 1;

    for (let row = 0; row < rows; row++) {
      for (let col = 0; col < cols; col++) {
        const wx = x + marginX + col * winGapX;
        const wy = y + marginY + row * winGapY;

        // 小窗户
        ctx.fillRect(wx, wy, winW, winH);
        ctx.strokeRect(wx, wy, winW, winH);

        // 窗户十字框
        ctx.beginPath();
        ctx.moveTo(wx + winW / 2, wy);
        ctx.lineTo(wx + winW / 2, wy + winH);
        ctx.moveTo(wx, wy + winH / 2);
        ctx.lineTo(wx + winW, wy + winH / 2);
        ctx.stroke();
      }
    }

    // 建筑门（底部中央）
    if (w > 30 && h > 40) {
      const doorW = 16;
      const doorH = 28;
      const dx = x + w / 2 - doorW / 2;
      const dy = y + h - doorH - 2;

      ctx.fillStyle = isDark ? '#FFFFFF' : '#1A1A2E';
      ctx.fillRect(dx, dy, doorW, doorH);
      ctx.strokeStyle = '#000000';
      ctx.lineWidth = 2;
      ctx.strokeRect(dx, dy, doorW, doorH);

      // 门把手（小白点）
      ctx.fillStyle = isDark ? '#FF0000' : '#FFFFFF';
      ctx.fillRect(dx + doorW - 5, dy + doorH / 2, 3, 3);
    }
  }

  /**
   * 像素标签 — Undertale 风格白色文字 + 黑色描边
   */
  renderPixelLabel(ctx, x, y, w, h, name) {
    ctx.save();
    // 黑色背景条（对话框风格）
    const labelW = ctx.measureText(name).width + 16;
    const labelH = 20;
    const lx = x + w / 2 - labelW / 2;
    const ly = y - labelH - 4;

    ctx.fillStyle = '#000000';
    ctx.fillRect(lx, ly, labelW, labelH);
    ctx.strokeStyle = '#FFFFFF';
    ctx.lineWidth = 2;
    ctx.strokeRect(lx, ly, labelW, labelH);

    // 白色像素字体
    ctx.fillStyle = '#FFFFFF';
    ctx.font = 'bold 13px "Courier New", "Consolas", monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(name, x + w / 2, ly + labelH / 2);
    ctx.restore();
  }

  // ==================== 树木渲染 ====================
  renderTrees(ctx, camX, camY, w, h) {
    const trees = this.mapData.trees || [];
    trees.forEach(tree => {
      const sx = tree.x - camX;
      const sy = tree.y - camY;
      if (sx < -80 || sx > w + 80 || sy < -80 || sy > h + 80) return;

      this.renderPixelTree(ctx, sx, sy, tree.radius || 35, tree.type);
    });
  }

  /**
   * 像素树 — Undertale 风格几何树
   * - 梧桐：深绿三角形树冠 + 棕色粗树干
   * - 樱花：粉红菱形树冠 + 棕色树干
   */
  renderPixelTree(ctx, x, y, size, type) {
    ctx.save();
    const crownColor = type === 'wutong' ? '#1E5C1E' : '#C86478';
    const crownHighlight = type === 'wutong' ? '#2D7A2D' : '#E08898';

    // 树冠 — 像素化三角形/菱形
    const crownSize = size * 1.2;
    const crownTop = y - size * 0.8;

    // 先画树冠阴影
    ctx.fillStyle = '#000000';
    ctx.beginPath();
    ctx.moveTo(x + 2, crownTop + 2);
    ctx.lineTo(x - crownSize + 2, y - size * 0.15 + 2);
    ctx.lineTo(x + crownSize + 2, y - size * 0.15 + 2);
    ctx.closePath();
    ctx.fill();

    // 树冠主体 — 三角几何形
    ctx.fillStyle = crownColor;
    ctx.beginPath();
    ctx.moveTo(x, crownTop);
    ctx.lineTo(x - crownSize, y - size * 0.15);
    ctx.lineTo(x + crownSize, y - size * 0.15);
    ctx.closePath();
    ctx.fill();

    // 树冠高光（上层小三角）
    ctx.fillStyle = crownHighlight;
    ctx.beginPath();
    ctx.moveTo(x, crownTop + size * 0.2);
    ctx.lineTo(x - crownSize * 0.6, y - size * 0.15);
    ctx.lineTo(x + crownSize * 0.6, y - size * 0.15);
    ctx.closePath();
    ctx.fill();

    // 树冠黑色边框
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(x, crownTop);
    ctx.lineTo(x - crownSize, y - size * 0.15);
    ctx.lineTo(x + crownSize, y - size * 0.15);
    ctx.closePath();
    ctx.stroke();

    // 树干 — 粗矩形
    const trunkW = size * 0.22;
    const trunkH = size * 0.5;
    ctx.fillStyle = '#6B4226';
    ctx.fillRect(x - trunkW / 2, y - size * 0.15, trunkW, trunkH);
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 2;
    ctx.strokeRect(x - trunkW / 2, y - size * 0.15, trunkW, trunkH);

    ctx.restore();
  }

  // ==================== 猫咪标记点 ====================
  renderCatSpots(ctx, camX, camY, w, h) {
    const catSpots = this.mapData.catSpots || [];
    const time = Date.now() / 1000;

    catSpots.forEach(spot => {
      const sx = spot.x - camX;
      const sy = spot.y - camY;
      if (sx < -40 || sx > w + 40 || sy < -40 || sy > h + 40) return;

      // Undertale 风格：像素 ♥ 标记
      const pulse = Math.sin(time * 3 + spot.x * 0.01) * 0.3 + 0.7;

      ctx.save();

      // 脉冲方块光晕（白色像素方框）
      const glowSize = 14 + pulse * 6;
      ctx.strokeStyle = `rgba(255, 255, 255, ${0.3 + pulse * 0.3})`;
      ctx.lineWidth = 2;
      ctx.strokeRect(sx - glowSize / 2, sy - glowSize / 2, glowSize, glowSize);

      // 黑色背景方块
      ctx.fillStyle = '#000000';
      ctx.fillRect(sx - 8, sy - 8, 16, 16);
      ctx.strokeStyle = '#FFFFFF';
      ctx.lineWidth = 2;
      ctx.strokeRect(sx - 8, sy - 8, 16, 16);

      // 红色像素爱心 ♥
      this.renderPixelHeart(ctx, sx, sy, 6, pulse);

      // 猫咪名字标签
      const cat = this.cats.find(c => c.id === spot.catId);
      if (cat && cat.name) {
        ctx.fillStyle = '#000000';
        const nameW = ctx.measureText(cat.name).width + 8;
        ctx.fillRect(sx - nameW / 2, sy + 12, nameW, 16);
        ctx.strokeStyle = '#FFFFFF';
        ctx.lineWidth = 1;
        ctx.strokeRect(sx - nameW / 2, sy + 12, nameW, 16);
        ctx.fillStyle = '#FF0000';
        ctx.font = 'bold 10px "Courier New", monospace';
        ctx.textAlign = 'center';
        ctx.fillText(cat.name, sx, sy + 23);
      }

      ctx.restore();
    });
  }

  /**
   * 像素爱心 ♥ — Undertale 灵魂之心
   */
  renderPixelHeart(ctx, cx, cy, size, pulse) {
    const s = size * pulse;
    ctx.fillStyle = '#FF0000';
    ctx.beginPath();

    // 用像素方块拼出爱心形状
    // 爱心上半（两个小方块）+ 下半（倒三角）
    const pixels = [
      // 行0:   · ■ ■ ·
      [2, 0], [3, 0],
      // 行1:  ■ ■ ■ ■
      [1, 1], [2, 1], [3, 1], [4, 1],
      // 行2: ■ ■ ■ ■ ■
      [0, 2], [1, 2], [2, 2], [3, 2], [4, 2], [5, 2],
      // 行3:  · ■ ■ ·
      [2, 3], [3, 3],
    ];

    const pxSize = s / 2.5;
    pixels.forEach(([px, py]) => {
      ctx.fillRect(
        cx - s * 0.6 + px * pxSize,
        cy - s * 0.5 + py * pxSize,
        pxSize,
        pxSize
      );
    });
  }

  // ==================== 辅助方法 ====================
  parseColor(hex) {
    const shorthand = /^#?([a-f\d])([a-f\d])([a-f\d])$/i;
    const hex2 = hex.replace(shorthand, (m, r, g, b) => r + r + g + g + b + b);
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex2);
    return result ? {
      r: parseInt(result[1], 16),
      g: parseInt(result[2], 16),
      b: parseInt(result[3], 16)
    } : { r: 128, g: 128, b: 128 };
  }

  isDarkColor(hex) {
    const rgb = this.parseColor(hex);
    // 相对亮度公式
    const luminance = (0.299 * rgb.r + 0.587 * rgb.g + 0.114 * rgb.b) / 255;
    return luminance < 0.5;
  }

  renderEmpty(ctx, w, h) {
    ctx.imageSmoothingEnabled = false;
    ctx.fillStyle = '#000000';
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = '#FFFFFF';
    ctx.font = '16px "Courier New", "Consolas", monospace';
    ctx.textAlign = 'center';
    ctx.fillText('* 地图数据加载中...', w / 2, h / 2);
    ctx.fillText('* LOADING...', w / 2, h / 2 + 24);
  }
}

module.exports = MapRenderer;

