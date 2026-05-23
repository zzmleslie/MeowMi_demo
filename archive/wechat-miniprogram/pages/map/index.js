// pages/map/index.js - 地图探索主逻辑（Undertale 对话版）
const app = getApp();

Page({
  data: {
    // Canvas 尺寸
    canvasWidth: 0,
    canvasHeight: 0,

    // 用户位置（地图世界坐标）
    userX: 400,
    userY: 600,

    // 摇杆
    joystickX: 0,
    joystickY: 0,
    joystickActive: false,

    // 地图偏移（用于滚动）
    mapOffsetX: 0,
    mapOffsetY: 0,

    // 发现提示
    discoverVisible: false,
    nearbyCat: null,

    // 弹窗
    modalVisible: false,
    activeCat: null,

    // 统计
    totalCats: 0,
    discoveredCount: 0,

    // ★ 对话状态
    dialogueActive: false
  },

  // Canvas 相关
  canvas: null,
  ctx: null,
  minimapCanvas: null,
  minimapCtx: null,
  mapRenderer: null,
  dialogueBox: null,  // ★ Undertale 对话框

  // 游戏循环
  animFrameId: null,
  lastTime: 0,

  // 摇杆相关
  joystickBaseRadius: 50,
  joystickCenter: { x: 0, y: 0 },

  // ★ 剧情进度
  storyChapter: 0,
  completedScenes: [],
  sceneData: null,     // 当前对话场景数据

  onLoad() {
    const sysInfo = wx.getSystemInfoSync();
    this.setData({
      canvasWidth: sysInfo.windowWidth,
      canvasHeight: sysInfo.windowHeight
    });
  },

  onReady() {
    this.initCanvas();
    this.initMinimap();
    this.loadMapRenderer();
    this.loadDialogueSystem();     // ★ 加载对话系统
    this.loadStats();
    this.loadStoryProgress();      // ★ 加载剧情进度
    this.startGameLoop();
  },

  onUnload() {
    if (this.animFrameId) {
      this.canvas.cancelAnimationFrame(this.animFrameId);
    }
  },

  // ==================== 初始化 Canvas ====================
  initCanvas() {
    const query = wx.createSelectorQuery();
    query.select('#mapCanvas')
      .fields({ node: true, size: true })
      .exec((res) => {
        if (!res[0]) return;
        const canvas = res[0].node;
        const ctx = canvas.getContext('2d');
        const dpr = app.globalData.dpr;

        canvas.width = res[0].width * dpr;
        canvas.height = res[0].height * dpr;
        ctx.scale(dpr, dpr);

        this.canvas = canvas;
        this.ctx = ctx;
      });
  },

  initMinimap() {
    const query = wx.createSelectorQuery();
    query.select('#minimapCanvas')
      .fields({ node: true, size: true })
      .exec((res) => {
        if (!res[0]) return;
        const canvas = res[0].node;
        const ctx = canvas.getContext('2d');
        const dpr = app.globalData.dpr;

        canvas.width = res[0].width * dpr;
        canvas.height = res[0].height * dpr;
        ctx.scale(dpr, dpr);

        this.minimapCanvas = canvas;
        this.minimapCtx = ctx;
      });
  },

  loadMapRenderer() {
    try {
      const MapRenderer = require('../../utils/map-renderer');
      this.mapRenderer = new MapRenderer();
    } catch (e) {
      console.error('地图渲染器加载失败', e);
    }
  },

  // ★ 加载 Undertale 对话系统
  loadDialogueSystem() {
    try {
      const DialogueBox = require('../../utils/dialogue-box');
      // 将在 ctx 可用后初始化
      this._DialogueBoxClass = DialogueBox;
    } catch (e) {
      console.error('对话系统加载失败', e);
    }
  },

  // ★ 加载剧情进度
  loadStoryProgress() {
    this.storyChapter = wx.getStorageSync('storyChapter') || 0;
    this.completedScenes = wx.getStorageSync('completedScenes') || [];
  },

  // ★ 保存剧情进度
  saveStoryProgress() {
    wx.setStorageSync('storyChapter', this.storyChapter);
    wx.setStorageSync('completedScenes', this.completedScenes);
  },

  // ★ 完成剧情场景
  completeScene(sceneId) {
    if (!this.completedScenes.includes(sceneId)) {
      this.completedScenes.push(sceneId);
      this.saveStoryProgress();
    }
  },

  // ★ 加载场景对话数据
  loadSceneData(chapterId) {
    try {
      return require(`../../data/dialogue-${chapterId}`);
    } catch (e) {
      // 尝试从全局数据加载
      const dialogueData = app.globalData.dialogueData;
      return dialogueData ? dialogueData[chapterId] : null;
    }
  },

  // ★ 检查并触发剧情对话
  checkAndTriggerStoryDialogue(catId) {
    // 第0章：序章（与大黄初遇）
    if (this.storyChapter === 0 && catId === 'cat_001' && !this.completedScenes.includes('ch0_prologue')) {
      const scene = this.loadSceneData('chapter0');
      if (scene) this.startDialogue(scene, 'ch0_prologue');
      return true;
    }

    // 第1章：认识所有猫
    if (this.storyChapter <= 1 && !this.completedScenes.includes('ch1_meet_all')) {
      const scene = this.loadSceneData('chapter1');
      if (scene) this.startDialogue(scene, 'ch1_meet_all');
      return true;
    }

    return false;
  },

  // ★ 开始对话
  startDialogue(scene, sceneId) {
    if (!this.ctx) return;
    if (!this.dialogueBox) {
      this.dialogueBox = new this._DialogueBoxClass(this.ctx, this.data.canvasWidth, this.data.canvasHeight);
    }

    this.sceneData = scene;
    this.currentSceneId = sceneId;
    this.setData({ dialogueActive: true });

    this.dialogueBox.start(scene,
      // onEnd
      () => {
        this.setData({ dialogueActive: false });
        if (sceneId) this.completeScene(sceneId);
        this.sceneData = null;
        this.currentSceneId = null;
      },
      // onTrigger - 处理场景触发事件
      (triggerName) => {
        console.log('剧情触发:', triggerName);
        this.handleStoryTrigger(triggerName);
      }
    );
  },

  // ★ 处理剧情触发事件
  handleStoryTrigger(triggerName) {
    switch (triggerName) {
      case 'goto_canteen':
        // 引导玩家去食堂
        this.setData({ userX: 600, userY: 700 });
        break;
      case 'meet_雪球':
      case 'meet_小橘':
      case 'meet_花花':
      case 'meet_墨墨':
      case 'meet_奶茶':
        // 引导移动 + 解锁猫点
        break;
      case '小橘生病':
      case '花花守夜':
      case '大黄告白':
        // 章节3关键事件标记
        break;
      case '新生小猫':
      case '传承仪式':
        // 章节4关键事件标记
        break;
      case '玩家找人类帮忙':
        wx.showToast({ title: '你在路中间坐下，对着路过的女生叫了一声...', icon: 'none', duration: 2500 });
        break;
    }
  },

  loadStats() {
    const cats = app.globalData.cats || [];
    const discovered = wx.getStorageSync('discoveredCats') || [];
    this.setData({
      totalCats: cats.length,
      discoveredCount: discovered.length
    });
  },

  // ==================== 游戏循环 ====================
  startGameLoop() {
    const loop = (timestamp) => {
      this.update(timestamp);
      this.render();
      this.animFrameId = this.canvas.requestAnimationFrame(loop);
    };
    this.animFrameId = this.canvas.requestAnimationFrame(loop);
  },

  update(timestamp) {
    if (!this.lastTime) { this.lastTime = timestamp; return; }
    const dt = (timestamp - this.lastTime) / 1000;
    this.lastTime = timestamp;

    // ★ 对话激活时暂停移动
    if (this.data.dialogueActive) {
      if (this.dialogueBox) this.dialogueBox.update(dt);
      return;
    }

    if (this.data.joystickActive) {
      this.applyJoystickMovement(dt);
    }
    this.checkCatProximity();
  },

  applyJoystickMovement(dt) {
    const speed = 120; // 像素/秒
    const jx = this.data.joystickX;
    const jy = this.data.joystickY;
    const mag = Math.sqrt(jx * jx + jy * jy);
    if (mag < 5) return;

    const nx = jx / mag;
    const ny = jy / mag;
    const moveX = nx * speed * dt;
    const moveY = ny * speed * dt;

    const newX = this.data.userX + moveX;
    const newY = this.data.userY + moveY;

    // 边界限制（地图范围 0~2000）
    const clampedX = Math.max(0, Math.min(2000, newX));
    const clampedY = Math.max(0, Math.min(2000, newY));

    this.setData({
      userX: clampedX,
      userY: clampedY
    });
  },

  checkCatProximity() {
    const mapData = app.globalData.mapData;
    if (!mapData?.catSpots) return;

    const ux = this.data.userX;
    const uy = this.data.userY;
    const proximity = 60; // 发现距离

    for (const spot of mapData.catSpots) {
      const dx = ux - spot.x;
      const dy = uy - spot.y;
      const dist = Math.sqrt(dx * dx + dy * dy);

      if (dist < proximity) {
        const cat = (app.globalData.cats || []).find(c => c.id === spot.catId);
        if (cat && !this.data.discoverVisible) {
          this.setData({ discoverVisible: true, nearbyCat: cat });
        }
        return;
      }
    }

    if (this.data.discoverVisible) {
      this.setData({ discoverVisible: false, nearbyCat: null });
    }
  },

  // ==================== 渲染 ====================
  render() {
    if (!this.ctx || !this.canvas) return;
    const ctx = this.ctx;
    const w = this.data.canvasWidth;
    const h = this.data.canvasHeight;

    ctx.clearRect(0, 0, w, h);

    // 渲染地图
    if (this.mapRenderer) {
      const cameraX = this.data.userX - w / 2;
      const cameraY = this.data.userY - h / 2;
      this.mapRenderer.render(ctx, cameraX, cameraY, w, h);
    } else {
      this.renderFallbackMap(ctx, w, h);
    }

    // 渲染用户小猫
    this.renderPlayer(ctx, w, h);

    // ★ 渲染 Undertale 对话框（最上层）
    if (this.data.dialogueActive && this.dialogueBox) {
      this.dialogueBox.render();
    }

    // 渲染小地图
    this.renderMinimap();
  },

  renderFallbackMap(ctx, w, h) {
    // 简单的地图背景
    const camX = this.data.userX - w / 2;
    const camY = this.data.userY - h / 2;

    // 草地色背景
    ctx.fillStyle = '#E8F0D8';
    ctx.fillRect(0, 0, w, h);

    // 绘制一些示意建筑
    const buildings = app.globalData.mapData?.buildings || [];
    buildings.forEach(b => {
      const sx = b.x - camX;
      const sy = b.y - camY;
      if (sx + b.w < 0 || sx > w || sy + b.h < 0 || sy > h) return;

      ctx.fillStyle = b.color || '#D4C5B9';
      ctx.fillRect(sx, sy, b.w, b.h);
      ctx.strokeStyle = '#8B7355';
      ctx.lineWidth = 2;
      ctx.strokeRect(sx, sy, b.w, b.h);
    });

    // 绘制猫咪出没点标记
    const catSpots = app.globalData.mapData?.catSpots || [];
    catSpots.forEach(spot => {
      const sx = spot.x - camX;
      const sy = spot.y - camY;
      if (sx < -20 || sx > w + 20 || sy < -20 || sy > h + 20) return;

      ctx.fillStyle = '#D4776B';
      ctx.beginPath();
      ctx.arc(sx, sy, 10, 0, Math.PI * 2);
      ctx.fill();

      // 浮动动画点
      const time = Date.now() / 1000;
      const pulse = 1 + Math.sin(time * 3) * 0.3;
      ctx.globalAlpha = 0.2;
      ctx.beginPath();
      ctx.arc(sx, sy, 18 * pulse, 0, Math.PI * 2);
      ctx.fill();
      ctx.globalAlpha = 1;
    });
  },

  renderPlayer(ctx, w, h) {
    const px = w / 2;  // 玩家始终在屏幕中心
    const py = h / 2;

    // 小猫身体
    ctx.fillStyle = '#F5DEB3';
    ctx.beginPath();
    ctx.arc(px, py, 16, 0, Math.PI * 2);
    ctx.fill();

    // 耳朵
    ctx.beginPath();
    ctx.moveTo(px - 10, py - 10);
    ctx.lineTo(px - 6, py - 24);
    ctx.lineTo(px - 2, py - 10);
    ctx.fill();
    ctx.beginPath();
    ctx.moveTo(px + 2, py - 10);
    ctx.lineTo(px + 6, py - 24);
    ctx.lineTo(px + 10, py - 10);
    ctx.fill();

    // 眼睛
    ctx.fillStyle = '#4A3728';
    ctx.beginPath();
    ctx.arc(px - 5, py - 2, 3, 0, Math.PI * 2);
    ctx.fill();
    ctx.beginPath();
    ctx.arc(px + 5, py - 2, 3, 0, Math.PI * 2);
    ctx.fill();

    // 尾巴
    ctx.strokeStyle = '#F5DEB3';
    ctx.lineWidth = 4;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(px + 10, py + 5);
    const tailWag = Math.sin(Date.now() / 300) * 10;
    ctx.quadraticCurveTo(px + 20, py + tailWag, px + 28, py - 5 + tailWag);
    ctx.stroke();
  },

  // ==================== 小地图 ====================
  renderMinimap() {
    if (!this.minimapCtx || !this.minimapCanvas) return;
    const ctx = this.minimapCtx;
    const size = 120; // 小地图尺寸
    const scale = size / 2000; // 地图2000x2000缩放到120

    ctx.clearRect(0, 0, size, size);

    // 背景
    ctx.fillStyle = 'rgba(255, 248, 240, 0.9)';
    ctx.fillRect(0, 0, size, size);
    ctx.strokeStyle = '#D4C5B9';
    ctx.lineWidth = 1;
    ctx.strokeRect(0, 0, size, size);

    // 建筑缩略
    const buildings = app.globalData.mapData?.buildings || [];
    buildings.forEach(b => {
      ctx.fillStyle = b.color || '#DDD';
      ctx.fillRect(b.x * scale, b.y * scale, b.w * scale, b.h * scale);
    });

    // 猫咪点
    const catSpots = app.globalData.mapData?.catSpots || [];
    catSpots.forEach(spot => {
      ctx.fillStyle = '#D4776B';
      ctx.beginPath();
      ctx.arc(spot.x * scale, spot.y * scale, 2, 0, Math.PI * 2);
      ctx.fill();
    });

    // 玩家位置
    ctx.fillStyle = '#FF6B6B';
    ctx.beginPath();
    ctx.arc(this.data.userX * scale, this.data.userY * scale, 3, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = '#FFF';
    ctx.lineWidth = 1;
    ctx.stroke();
  },

  // ==================== 摇杆控制 ====================
  onJoystickStart(e) {
    const touch = e.touches[0];
    const query = wx.createSelectorQuery();
    query.select('.joystick-base').boundingClientRect((rect) => {
      if (!rect) return;
      this.joystickCenter = {
        x: rect.left + rect.width / 2,
        y: rect.top + rect.height / 2
      };
      this.joystickBaseRadius = rect.width / 2 - 16;
      this.updateJoystick(touch.clientX, touch.clientY);
    }).exec();
    this.setData({ joystickActive: true });
  },

  onJoystickMove(e) {
    if (!this.data.joystickActive) return;
    const touch = e.touches[0];
    this.updateJoystick(touch.clientX, touch.clientY);
  },

  onJoystickEnd() {
    this.setData({
      joystickActive: false,
      joystickX: 0,
      joystickY: 0
    });
  },

  updateJoystick(tx, ty) {
    let dx = tx - this.joystickCenter.x;
    let dy = ty - this.joystickCenter.y;
    const dist = Math.sqrt(dx * dx + dy * dy);

    if (dist > this.joystickBaseRadius) {
      dx = (dx / dist) * this.joystickBaseRadius;
      dy = (dy / dist) * this.joystickBaseRadius;
    }

    this.setData({ joystickX: dx, joystickY: dy });
  },

  // 地图拖拽 & ★ 对话点击
  onTouchStart(e) {
    // 对话激活时：点击推进对话
    if (this.data.dialogueActive && this.dialogueBox) {
      const touch = e.touches[0];
      if (touch) this.dialogueBox.handleTap(touch.x, touch.y);
      return;
    }
    // 记录拖拽起始点
  },
  onTouchMove(e) {
    if (this.data.dialogueActive) return;
  },
  onTouchEnd(e) {
    // 对话激活时：点击推进对话
    if (this.data.dialogueActive && this.dialogueBox) {
      const touch = e.changedTouches[0];
      if (touch) this.dialogueBox.handleTap(touch.x, touch.y);
      return;
    }
  },

  // ==================== 猫咪交互 ====================
  openCatDetail() {
    const cat = this.data.nearbyCat;
    if (!cat) return;

    // ★ 优先检查剧情对话
    if (this.checkAndTriggerStoryDialogue(cat.id)) {
      this.setData({ discoverVisible: false });
      return;
    }

    this.setData({ modalVisible: true, activeCat: cat, discoverVisible: false });
    app.discoverCat(cat.id);
    this.loadStats();
  },

  closeModal() {
    this.setData({ modalVisible: false, activeCat: null });
  },

  viewDetail() {
    const cat = this.data.activeCat;
    if (!cat) return;
    wx.setStorageSync('viewCat', cat);
    wx.navigateTo({ url: `/pages/cat/detail?id=${cat.id}` });
  },

  preventScroll() {
    return false;
  }
});
