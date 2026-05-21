// pages/index/index.js
const app = getApp();

Page({
  data: {
    totalCats: 0,
    discoveredCount: 0,
    catSpots: 0
  },

  onShow() {
    this.loadStats();
  },

  loadStats() {
    const cats = app.globalData.cats || [];
    const discovered = wx.getStorageSync('discoveredCats') || [];
    const mapData = app.globalData.mapData;

    this.setData({
      totalCats: cats.length,
      discoveredCount: discovered.length,
      catSpots: mapData?.catSpots?.length || 0
    });
  },

  navigateToMap() {
    wx.switchTab({ url: '/pages/map/index' });
  },

  navigateToGallery() {
    wx.switchTab({ url: '/pages/gallery/index' });
  }
});
