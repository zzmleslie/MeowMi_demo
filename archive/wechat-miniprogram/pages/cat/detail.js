// pages/cat/detail.js
const app = getApp();

Page({
  data: {
    cat: {},
    catId: ''
  },

  onLoad(options) {
    const catId = options.id;
    if (catId) {
      this.loadCat(catId);
    } else {
      // 尝试从缓存加载
      const cached = wx.getStorageSync('viewCat');
      if (cached) {
        this.setData({ cat: cached });
      }
    }
  },

  loadCat(id) {
    const cats = app.globalData.cats || [];
    const cat = cats.find(c => c.id === id);
    if (cat) {
      this.setData({ cat, catId: id });
    }
  },

  goToMap() {
    wx.switchTab({ url: '/pages/map/index' });
  }
});
