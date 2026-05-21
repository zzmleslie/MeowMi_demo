// app.js - 南雍猫札 小程序入口
App({
  globalData: {
    // 猫咪数据库
    cats: [],
    // 地图数据
    mapData: null,
    // 用户信息
    userInfo: null,
    // 用户当前在地图上的位置
    userPosition: { x: 0, y: 0 },
    // 已发现的猫咪列表
    discoveredCats: [],
    // Canvas 相关
    canvasWidth: 0,
    canvasHeight: 0,
    dpr: 1
  },

  onLaunch() {
    // 加载猫咪数据
    this.loadCatsData();
    // 加载地图数据
    this.loadMapData();
    // 获取设备信息
    this.getDeviceInfo();
  },

  loadCatsData() {
    try {
      const cats = require('./data/cats.json');
      this.globalData.cats = cats;
    } catch (e) {
      console.warn('猫咪数据加载失败，使用空数据', e);
      this.globalData.cats = [];
    }
  },

  loadMapData() {
    try {
      const mapData = require('./data/map-data.json');
      this.globalData.mapData = mapData;
    } catch (e) {
      console.warn('地图数据加载失败', e);
      this.globalData.mapData = null;
    }
  },

  getDeviceInfo() {
    const sysInfo = wx.getSystemInfoSync();
    this.globalData.canvasWidth = sysInfo.windowWidth;
    this.globalData.canvasHeight = sysInfo.windowHeight;
    this.globalData.dpr = sysInfo.pixelRatio || 1;
  },

  /** 标记猫咪为已发现 */
  discoverCat(catId) {
    if (!this.globalData.discoveredCats.includes(catId)) {
      this.globalData.discoveredCats.push(catId);
      wx.setStorageSync('discoveredCats', this.globalData.discoveredCats);
    }
  }
});
