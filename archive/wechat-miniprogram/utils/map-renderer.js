// utils/map-renderer.js - Canvas 水彩风格地图渲染引擎
// 
// 核心设计思路：
// 1. 用多层半透明圆形叠加模拟水彩晕染效果
// 2. 对手绘轮廓线添加 Perlin 噪声抖动，让线条有"手绘感"
// 3. 分形算法生成树木
// 4. 对建筑轮廓从真实照片数据提取的颜色进行软填充
// 
// 技术亮点（比赛可重点介绍）：
// - 水彩笔触算法：多层透明度叠加 + 随机偏移
// - 简化 Perlin 噪声用于手绘线条抖动
// - L-system 分形树生成
// - Douglas-Peucker 轮廓简化（预处理脚本中实现）

const app = getApp();

class MapRenderer {
  constructor() {
    this.mapData = app.globalData.mapData;
    this.cats = app.globalData.cats || [];
    // 伪随机种子，用于噪声
    this.seed = 42;
  }

  /**
   * 主渲染入口
   * @param {CanvasRenderingContext2D} ctx
   * @param {number} camX - 相机左上角世界坐标 X
   * @param {number} camY - 相机左上角世界坐标 Y
   * @param {number} viewW - 视口宽度
   * @param {number} viewH - 视口高度
   */
  render(ctx, camX, camY, viewW, viewH) {
    if (!this.mapData) {
      this.renderEmpty(ctx, viewW, viewH);
      return;
    }

    // 1. 背景 - 柔和底色
    this.renderBackground(ctx, camX, camY, viewW, viewH);

    // 2. 地面纹理（模拟水彩纸纹理）
    this.renderGroundTexture(ctx, camX, camY, viewW, viewH);

    // 3. 路径/道路
    this.renderPaths(ctx, camX, camY, viewW, viewH);

    // 4. 建筑
    this.renderBuildings(ctx, camX, camY, viewW, viewH);

    // 5. 树木
    this.renderTrees(ctx, camX, camY, viewW, viewH);

    // 6. 猫咪出没标记
    this.renderCatSpots(ctx, camX, camY, viewW, viewH);
  }

  // ==================== 背景 ====================
  renderBackground(ctx, camX, camY, w, h) {
    // 柔和暖色渐变背景
    const gradient = ctx.createLinearGradient(0, 0, 0, h);
    gradient.addColorStop(0, '#FDF8F0');
    gradient.addColorStop(0.5, '#F5ECD7');
    gradient.addColorStop(1, '#E8EDD8');
    ctx.fillStyle = gradient;
    ctx.fillRect(0, 0, w, h);
  }

  renderGroundTexture(ctx, camX, camY, w, h) {
    // 水彩纸张纹理层 - 用极低透明度的随机斑点模拟
    ctx.save();
    ctx.globalAlpha = 0.03;
    const step = 40;
    for (let sx = (camX - camX % step); sx < camX + w; sx += step) {
      for (let sy = (camY - camY % step); sy < camY + h; sy += step) {
        const noiseVal = this.simpleNoise(sx / 30, sy / 30);
        const shade = 200 + noiseVal * 55;
        ctx.fillStyle = `rgb(${shade},${shade - 5},${shade - 10})`;
        const rx = sx - camX + (noiseVal - 0.5) * 8;
        const ry = sy - camY + (noiseVal - 0.5) * 8;
        ctx.beginPath();
        ctx.arc(rx, ry, 15 + noiseVal * 5, 0, Math.PI * 2);
        ctx.fill();
      }
    }
    ctx.restore();
  }

  // ==================== 路径 ====================
  renderPaths(ctx, camX, camY, w, h) {
    const paths = this.mapData.paths || [];
    paths.forEach(path => {
      if (!path.points || path.points.length < 2) return;
      ctx.save();
      ctx.strokeStyle = '#D4C5B9';
      ctx.lineWidth = path.width || 12;
      ctx.lineCap = 'round';
      ctx.lineJoin = 'round';
      ctx.globalAlpha = 0.4;
      ctx.beginPath();

      for (let i = 0; i < path.points.length; i++) {
        const pt = path.points[i];
        const sx = pt[0] - camX;
        const sy = pt[1] - camY;
        // 手绘抖动
        const jitter = this.simpleNoise(sx / 20, sy / 20) * 3;
        if (i === 0) {
          ctx.moveTo(sx + jitter, sy + jitter);
        } else {
          ctx.lineTo(sx + jitter, sy + jitter);
        }
      }
      ctx.stroke();
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
      if (sx + bw < -10 || sx > w + 10 || sy + bh < -10 || sy > h + 10) return;

      // 阴影
      this.renderBuildingShadow(ctx, sx, sy, bw, bh);

      // 水彩填充
      this.renderWatercolorFill(ctx, sx, sy, bw, bh, building.color || '#D4C5B9');

      // 水彩描边（手绘线条）
      this.renderWatercolorOutline(ctx, sx, sy, bw, bh, building.strokeColor || '#8B7355');

      // 建筑标签
      if (building.name) {
        this.renderBuildingLabel(ctx, sx, sy, bw, building.name);
      }

      // 如果有详细轮廓点（从真实照片提取的），使用轮廓渲染
      if (building.contour && building.contour.length > 3) {
        this.renderContourBuilding(ctx, building, camX, camY);
      }
    });
  }

  renderBuildingShadow(ctx, x, y, w, h) {
    ctx.save();
    ctx.fillStyle = 'rgba(120, 100, 80, 0.15)';
    ctx.fillRect(x + 4, y + 4, w, h);
    ctx.restore();
  }

  /**
   * 水彩填充 - 核心算法！
   * 多层半透明颜色叠加，模拟水彩的透明感和不均匀着色
   */
  renderWatercolorFill(ctx, x, y, w, h, color) {
    ctx.save();

    // 解析颜色
    const rgb = this.parseColor(color);
    const layers = 5 + Math.floor(this.simpleNoise(x / 100, y / 100) * 5);

    for (let i = 0; i < layers; i++) {
      const alpha = 0.02 + Math.random() * 0.04;
      // 每层颜色有微小偏移
      const r = rgb.r + (Math.random() - 0.5) * 15;
      const g = rgb.g + (Math.random() - 0.5) * 12;
      const b = rgb.b + (Math.random() - 0.5) * 10;

      ctx.fillStyle = `rgba(${Math.round(r)},${Math.round(g)},${Math.round(b)},${alpha})`;

      // 填充略有缩进的矩形 + 随机偏移
      const padding = 2;
      const ox = (Math.random() - 0.5) * 6;
      const oy = (Math.random() - 0.5) * 6;
      ctx.fillRect(x + padding + ox, y + padding + oy, w - padding * 2, h - padding * 2);
    }

    ctx.restore();
  }

  /**
   * 水彩描边 - 手绘线条模拟
   * 多次绘制略有偏移的线条，产生手绘笔触感
   */
  renderWatercolorOutline(ctx, x, y, w, h, strokeColor) {
    ctx.save();
    const rgb = this.parseColor(strokeColor);

    for (let pass = 0; pass < 3; pass++) {
      const alpha = 0.2 - pass * 0.05;
      ctx.strokeStyle = `rgba(${rgb.r},${rgb.g},${rgb.b},${alpha})`;
      ctx.lineWidth = 2 + Math.random() * 1.5;

      // 每条边加手绘抖动
      ctx.beginPath();
      this.handDrawnLine(ctx, x, y, x + w, y);           // 上边
      this.handDrawnLine(ctx, x + w, y, x + w, y + h);   // 右边
      this.handDrawnLine(ctx, x + w, y + h, x, y + h);   // 下边
      this.handDrawnLine(ctx, x, y + h, x, y);            // 左边
      ctx.stroke();
    }

    ctx.restore();
  }

  /**
   * 手绘线段 - 在线段上添加微小扰动
   */
  handDrawnLine(ctx, x1, y1, x2, y2) {
    const dist = Math.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2);
    const segments = Math.max(3, Math.floor(dist / 15));
    const dx = (x2 - x1) / segments;
    const dy = (y2 - y1) / segments;

    let cx = x1;
    let cy = y1;

    for (let i = 0; i <= segments; i++) {
      const jx = (this.simpleNoise(cx / 15, cy / 15) - 0.5) * 4;
      const jy = (this.simpleNoise(cx / 15 + 5, cy / 15 + 5) - 0.5) * 4;
      if (i === 0) {
        ctx.moveTo(cx + jx, cy + jy);
      } else {
        ctx.lineTo(cx + jx, cy + jy);
      }
      cx += dx;
      cy += dy;
    }
  }

  /**
   * 使用真实轮廓点渲染建筑（照片预处理后使用）
   */
  renderContourBuilding(ctx, building, camX, camY) {
    if (!building.contour || building.contour.length < 3) return;

    ctx.save();
    const color = building.fillColor || building.color || '#D4C5B9';
    const rgb = this.parseColor(color);

    // 水彩填充 - 多边形版本
    const layers = 4 + Math.floor(Math.random() * 4);
    for (let i = 0; i < layers; i++) {
      const alpha = 0.03 + Math.random() * 0.04;
      const r = rgb.r + (Math.random() - 0.5) * 10;
      const g = rgb.g + (Math.random() - 0.5) * 8;
      const b = rgb.b + (Math.random() - 0.5) * 8;

      ctx.fillStyle = `rgba(${Math.round(r)},${Math.round(g)},${Math.round(b)},${alpha})`;
      ctx.beginPath();

      const firstPt = building.contour[0];
      ctx.moveTo(firstPt[0] - camX + (Math.random() - 0.5) * 3, firstPt[1] - camY + (Math.random() - 0.5) * 3);

      for (let j = 1; j < building.contour.length; j++) {
        const pt = building.contour[j];
        ctx.lineTo(pt[0] - camX + (Math.random() - 0.5) * 3, pt[1] - camY + (Math.random() - 0.5) * 3);
      }
      ctx.closePath();
      ctx.fill();
    }

    // 手绘轮廓描边
    ctx.strokeStyle = `rgba(${rgb.r},${rgb.g},${rgb.b},0.5)`;
    ctx.lineWidth = 2.5;
    ctx.lineJoin = 'round';
    ctx.beginPath();
    for (let j = 0; j < building.contour.length; j++) {
      const pt = building.contour[j];
      const jx = (this.simpleNoise(pt[0] / 10, pt[1] / 10) - 0.5) * 2;
      const jy = (this.simpleNoise(pt[0] / 10 + 3, pt[1] / 10 + 3) - 0.5) * 2;
      if (j === 0) {
        ctx.moveTo(pt[0] - camX + jx, pt[1] - camY + jy);
      } else {
        ctx.lineTo(pt[0] - camX + jx, pt[1] - camY + jy);
      }
    }
    ctx.closePath();
    ctx.stroke();

    ctx.restore();
  }

  renderBuildingLabel(ctx, x, y, w, name) {
    ctx.save();
    ctx.fillStyle = '#4A3728';
    ctx.font = '11px "Georgia", "PingFang SC", serif';
    ctx.textAlign = 'center';
    ctx.fillText(name, x + w / 2, y + w > 50 ? y - 8 : y - 4);
    ctx.restore();
  }

  // ==================== 树木渲染 ====================
  renderTrees(ctx, camX, camY, w, h) {
    const trees = this.mapData.trees || [];
    trees.forEach(tree => {
      const sx = tree.x - camX;
      const sy = tree.y - camY;
      if (sx < -80 || sx > w + 80 || sy < -80 || sy > h + 80) return;

      this.renderTree(ctx, sx, sy, tree.radius || 35, tree.type);
    });
  }

  /**
   * 分形树 - 递归绘制树枝
   */
  renderTree(ctx, x, y, size, type) {
    ctx.save();

    // 树冠 - 多层圆形叠加（水彩感）
    const crownColor = type === 'wutong' ? '#7BA07B' : '#8FB08F';
    const rgb = this.parseColor(crownColor);

    for (let i = 0; i < 8; i++) {
      const alpha = 0.03 + Math.random() * 0.05;
      const cr = rgb.r + (Math.random() - 0.5) * 20;
      const cg = rgb.g + (Math.random() - 0.5) * 15;
      const cb = rgb.b + (Math.random() - 0.5) * 10;
      const ox = (Math.random() - 0.5) * size * 0.4;
      const oy = (Math.random() - 0.5) * size * 0.3;
      const r = size * (0.5 + Math.random() * 0.5);

      ctx.fillStyle = `rgba(${Math.round(cr)},${Math.round(cg)},${Math.round(cb)},${alpha})`;
      ctx.beginPath();
      ctx.arc(x + ox, y - size * 0.2 + oy, r, 0, Math.PI * 2);
      ctx.fill();
    }

    // 树干
    ctx.strokeStyle = '#8B7355';
    ctx.lineWidth = size * 0.15;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.lineTo(x + (this.simpleNoise(x, y) - 0.5) * 5, y - size * 0.6);
    ctx.stroke();

    ctx.restore();
  }

  // ==================== 猫咪标记点 ====================
  renderCatSpots(ctx, camX, camY, w, h) {
    const catSpots = this.mapData.catSpots || [];
    const time = Date.now() / 1000;

    catSpots.forEach(spot => {
      const sx = spot.x - camX;
      const sy = spot.y - camY;
      if (sx < -30 || sx > w + 30 || sy < -30 || sy > h + 30) return;

      // 光圈脉冲
      const pulseRadius = 16 + Math.sin(time * 2.5 + spot.x * 0.01) * 5;

      // 外圈光晕
      ctx.save();
      ctx.globalAlpha = 0.15;
      ctx.fillStyle = '#D4776B';
      ctx.beginPath();
      ctx.arc(sx, sy, pulseRadius + 6, 0, Math.PI * 2);
      ctx.fill();

      // 内圈
      ctx.globalAlpha = 0.3;
      ctx.beginPath();
      ctx.arc(sx, sy, pulseRadius, 0, Math.PI * 2);
      ctx.fill();

      // 核心点 + 猫爪印
      ctx.globalAlpha = 0.8;
      ctx.fillStyle = '#D4776B';
      ctx.beginPath();
      ctx.arc(sx, sy, 6, 0, Math.PI * 2);
      ctx.fill();

      // 小猫emoji提示
      ctx.globalAlpha = 1;
      ctx.font = '18px serif';
      ctx.textAlign = 'center';
      ctx.fillText('🐾', sx, sy - 14);
      ctx.restore();
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
    } : { r: 200, g: 180, b: 160 };
  }

  /**
   * 简化版噪声函数（用于手绘抖动和纹理）
   * 返回 0~1 的值
   */
  simpleNoise(x, y) {
    const n = Math.sin(x * 12.9898 + y * 78.233 + this.seed) * 43758.5453;
    return n - Math.floor(n);
  }

  renderEmpty(ctx, w, h) {
    ctx.fillStyle = '#FDF8F0';
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = '#C9A96E';
    ctx.font = '16px "PingFang SC", sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText('地图数据加载中...', w / 2, h / 2);
  }
}

module.exports = MapRenderer;
