#!/usr/bin/env node

/**
 * ============================================
 *  喵喵喵喵 - 像素艺术预处理脚本 (Undertale 风格)
 *  ============================================
 * 
 * 功能：将真实校园照片处理为 Undertale 像素RPG风格地图数据
 * 
 * 处理流程：
 *   1. 像素化降采样 → 缩小图像获得像素块效果
 *   2. 边缘检测 → 提取像素化建筑轮廓
 *   3. 主导色提取 → 获取像素化纯色
 *   4. 轮廓简化 → Douglas-Peucker 算法（像素量化）
 *   5. 输出 → JSON 地图数据文件
 * 
 * 使用方法：
 *   1. 将校园照片放入 ./photos/ 文件夹
 *   2. 运行: node scripts/preprocess.js
 *   3. 输出: ./output/map-data-generated.json
 * 
 * 安装依赖: npm install sharp
 */

const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

// ==================== 配置 ====================
const CONFIG = {
  photoDir: path.join(__dirname, '..', 'photos'),
  outputDir: path.join(__dirname, '..', 'output'),
  outputFile: 'map-data-generated.json',
  
  // 像素艺术处理参数
  pixelScale: 4,            // 缩小倍数（值越大像素块越大）
  edgeThreshold: 60,        // 边缘检测阈值（略高=更清晰像素边）
  blurSigma: 1,             // 轻微模糊（保持像素锐度）
  minContourArea: 300,      // 最小轮廓面积
  simplifyEpsilon: 4,       // 轮廓简化容差（像素量化）
  
  // 颜色聚类参数
  colorClusterCount: 3,     // 主导色数量（更少=更纯的像素色）
};

// ==================== 工具函数 ====================

/**
 * Douglas-Peucker 轮廓简化算法（像素量化版）
 */
function douglasPeucker(points, epsilon) {
  if (points.length <= 2) return points;

  let maxDist = 0;
  let maxIndex = 0;
  const first = points[0];
  const last = points[points.length - 1];

  for (let i = 1; i < points.length - 1; i++) {
    const dist = perpendicularDistance(points[i], first, last);
    if (dist > maxDist) {
      maxDist = dist;
      maxIndex = i;
    }
  }

  if (maxDist > epsilon) {
    const left = douglasPeucker(points.slice(0, maxIndex + 1), epsilon);
    const right = douglasPeucker(points.slice(maxIndex), epsilon);
    return [...left.slice(0, -1), ...right];
  }

  return [first, last];
}

function perpendicularDistance(point, lineStart, lineEnd) {
  const dx = lineEnd[0] - lineStart[0];
  const dy = lineEnd[1] - lineStart[1];
  const mag = Math.sqrt(dx * dx + dy * dy);
  if (mag === 0) return Math.sqrt((point[0] - lineStart[0]) ** 2 + (point[1] - lineStart[1]) ** 2);
  
  const u = ((point[0] - lineStart[0]) * dx + (point[1] - lineStart[1]) * dy) / (mag * mag);
  const closestX = lineStart[0] + u * dx;
  const closestY = lineStart[1] + u * dy;
  
  return Math.sqrt((point[0] - closestX) ** 2 + (point[1] - closestY) ** 2);
}

/**
 * 像素量化坐标（对齐到像素网格）
 */
function snapToPixelGrid(coord, gridSize = 4) {
  return Math.round(coord / gridSize) * gridSize;
}

/**
 * 简化版 K-means 颜色聚类
 * 提取像素风格主导色（更少的颜色=更纯的像素色）
 */
function extractDominantColors(pixels, k, maxIterations = 10) {
  // 随机初始化 k 个中心点
  let centers = [];
  for (let i = 0; i < k; i++) {
    const idx = Math.floor(Math.random() * pixels.length);
    centers.push([...pixels[idx]]);
  }

  for (let iter = 0; iter < maxIterations; iter++) {
    // 分配每个像素到最近的中心
    const clusters = Array.from({ length: k }, () => []);
    for (const pixel of pixels) {
      let minDist = Infinity;
      let minIdx = 0;
      for (let c = 0; c < k; c++) {
        const dr = pixel[0] - centers[c][0];
        const dg = pixel[1] - centers[c][1];
        const db = pixel[2] - centers[c][2];
        const dist = dr * dr + dg * dg + db * db;
        if (dist < minDist) {
          minDist = dist;
          minIdx = c;
        }
      }
      clusters[minIdx].push(pixel);
    }

    // 更新中心点
    let changed = false;
    for (let c = 0; c < k; c++) {
      if (clusters[c].length === 0) continue;
      const avgR = clusters[c].reduce((s, p) => s + p[0], 0) / clusters[c].length;
      const avgG = clusters[c].reduce((s, p) => s + p[1], 0) / clusters[c].length;
      const avgB = clusters[c].reduce((s, p) => s + p[2], 0) / clusters[c].length;
      const newCenter = [avgR, avgG, avgB];
      if (Math.abs(newCenter[0] - centers[c][0]) > 1) changed = true;
      centers[c] = newCenter;
    }

    if (!changed) break;
  }

  return centers.map(c => ({
    r: Math.round(c[0]),
    g: Math.round(c[1]),
    b: Math.round(c[2]),
    hex: rgbToHex(Math.round(c[0]), Math.round(c[1]), Math.round(c[2]))
  }));
}

function rgbToHex(r, g, b) {
  return '#' + [r, g, b].map(x => x.toString(16).padStart(2, '0')).join('');
}

/**
 * 简化版边缘检测
 * 使用 Sobel 算子 + 阈值
 */
async function detectEdges(imagePath) {
  const image = sharp(imagePath);
  const metadata = await image.metadata();
  
  // 先缩小图片以提高处理速度
  const scale = Math.min(1, 800 / Math.max(metadata.width, metadata.height));
  const resized = await image
    .resize(Math.round(metadata.width * scale), Math.round(metadata.height * scale))
    .greyscale()
    .raw()
    .toBuffer();

  const w = Math.round(metadata.width * scale);
  const h = Math.round(metadata.height * scale);
  
  // Sobel 边缘检测
  const edges = new Uint8Array(w * h);
  const threshold = CONFIG.edgeThreshold;

  for (let y = 1; y < h - 1; y++) {
    for (let x = 1; x < w - 1; x++) {
      const idx = y * w + x;
      const gx = 
        -1 * resized[(y-1) * w + (x-1)] + 1 * resized[(y-1) * w + (x+1)] +
        -2 * resized[y * w + (x-1)]     + 2 * resized[y * w + (x+1)] +
        -1 * resized[(y+1) * w + (x-1)] + 1 * resized[(y+1) * w + (x+1)];
      const gy = 
        -1 * resized[(y-1) * w + (x-1)] + -2 * resized[(y-1) * w + x] + -1 * resized[(y-1) * w + (x+1)] +
         1 * resized[(y+1) * w + (x-1)] +  2 * resized[(y+1) * w + x] +  1 * resized[(y+1) * w + (x+1)];
      
      const magnitude = Math.sqrt(gx * gx + gy * gy);
      edges[idx] = magnitude > threshold ? 255 : 0;
    }
  }

  return { edges, width: w, height: h, scale };
}

/**
 * 从边缘图中提取轮廓点
 */
function extractContours(edgeData, width, height) {
  const { edges } = edgeData;
  const visited = new Uint8Array(width * height);
  const contours = [];

  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const idx = y * width + x;
      if (edges[idx] === 255 && !visited[idx]) {
        // BFS 追踪轮廓
        const contour = [];
        const queue = [[x, y]];
        visited[idx] = 1;

        while (queue.length > 0) {
          const [cx, cy] = queue.shift();
          contour.push([cx, cy]);

          // 8邻域搜索
          for (let dy = -1; dy <= 1; dy++) {
            for (let dx = -1; dx <= 1; dx++) {
              if (dx === 0 && dy === 0) continue;
              const nx = cx + dx;
              const ny = cy + dy;
              if (nx >= 0 && nx < width && ny >= 0 && ny < height) {
                const nidx = ny * width + nx;
                if (edges[nidx] === 255 && !visited[nidx]) {
                  visited[nidx] = 1;
                  queue.push([nx, ny]);
                }
              }
            }
          }
        }

        if (contour.length >= CONFIG.minContourArea) {
          contours.push(contour);
        }
      }
    }
  }

  return contours;
}

/**
 * 处理单张照片
 */
async function processPhoto(filePath, fileName) {
  console.log(`  📷 处理: ${fileName}`);

  // 1. 边缘检测
  const edgeData = await detectEdges(filePath);
  console.log(`    边缘检测完成: ${edgeData.width}x${edgeData.height}`);

  // 2. 提取轮廓
  const contours = extractContours(edgeData, edgeData.width, edgeData.height);
  console.log(`    提取到 ${contours.length} 个轮廓`);

  // 3. 简化轮廓
  const simplified = contours.map(c => douglasPeucker(c, CONFIG.simplifyEpsilon));
  console.log(`    轮廓简化完成`);

  // 4. 提取主导色
  const image = sharp(filePath);
  const { data } = await image
    .resize(200, 200, { fit: 'inside' })
    .raw()
    .toBuffer({ resolveWithObject: true });

  // 采样像素
  const pixels = [];
  for (let i = 0; i < data.length; i += 4) {
    pixels.push([data[i], data[i + 1], data[i + 2]]);
  }

  const colors = extractDominantColors(
    pixels.slice(0, Math.min(1000, pixels.length)), 
    CONFIG.colorClusterCount
  );
  console.log(`    主导色: ${colors.map(c => c.hex).join(', ')}`);

  // 5. 构建建筑数据
  // 选择最大的几个轮廓作为建筑
  const mainContours = simplified
    .sort((a, b) => b.length - a.length)
    .slice(0, 3);

  const building = {
    id: `gen_${fileName.replace(/\.[^.]+$/, '')}`,
    name: fileName.replace(/\.[^.]+$/, ''),
    contour: mainContours[0] || [],
    fillColor: colors[0]?.hex || '#D4C5B9',
    strokeColor: colors[1]?.hex || '#8B7355',
    dominantColors: colors.map(c => c.hex),
    // 需要手动指定在地图上的位置和大小
    x: 0,  // ← 请手动填写！
    y: 0,  // ← 请手动填写！
    w: 160,
    h: 120
  };

  return building;
}

// ==================== 主流程 ====================
async function main() {
  console.log('╔══════════════════════════════════╗');
  console.log('║   🐱 喵喵喵喵 - 照片预处理器  ║');
  console.log('╚══════════════════════════════════╝');
  console.log('');

  // 检查输入目录
  if (!fs.existsSync(CONFIG.photoDir)) {
    console.log(`📁 创建照片目录: ${CONFIG.photoDir}`);
    fs.mkdirSync(CONFIG.photoDir, { recursive: true });
    console.log('⚠️  请将校园照片放入 photos/ 文件夹后重新运行');
    return;
  }

  const files = fs.readdirSync(CONFIG.photoDir)
    .filter(f => /\.(jpg|jpeg|png|webp)$/i.test(f));

  if (files.length === 0) {
    console.log('⚠️  photos/ 文件夹中没有找到图片文件');
    console.log('   请放入校园建筑照片（支持 .jpg .png .webp）');
    return;
  }

  console.log(`📁 找到 ${files.length} 张照片\n`);

  // 处理每张照片
  const buildings = [];
  for (const file of files) {
    try {
      const building = await processPhoto(
        path.join(CONFIG.photoDir, file),
        file
      );
      buildings.push(building);
    } catch (err) {
      console.error(`  ❌ 处理失败: ${file}`, err.message);
    }
    console.log('');
  }

  // 生成输出
  const outputData = {
    _comment: "⚠️ 请手动编辑此文件：填写每个建筑的 x, y, w, h 坐标",
    _instruction: "将 x/y 设为建筑在地图上的位置（地图范围 0~2000），w/h 为宽高",
    mapName: "南京大学鼓楼校区（生成中）",
    mapSize: { width: 2000, height: 2000 },
    buildings: buildings,
    trees: [],
    paths: [],
    catSpots: []
  };

  // 确保输出目录存在
  if (!fs.existsSync(CONFIG.outputDir)) {
    fs.mkdirSync(CONFIG.outputDir, { recursive: true });
  }

  const outputPath = path.join(CONFIG.outputDir, CONFIG.outputFile);
  fs.writeFileSync(outputPath, JSON.stringify(outputData, null, 2), 'utf-8');

  console.log('═══════════════════════════════════');
  console.log(`✨ 处理完成！`);
  console.log(`📄 输出文件: ${outputPath}`);
  console.log(`🏠 提取了 ${buildings.length} 个建筑的轮廓和颜色`);
  console.log('');
  console.log('📝 接下来的步骤：');
  console.log('   1. 打开输出的 JSON 文件');
  console.log('   2. 手动设置每个建筑的 x, y 坐标');
  console.log('   3. 将其整合到 data/map-data.json 中');
  console.log('   4. 导入 Unity 项目查看效果~');
  console.log('═══════════════════════════════════');
}

main().catch(console.error);
