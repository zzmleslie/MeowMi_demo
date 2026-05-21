// pages/gallery/index.js
const app = getApp();

Page({
  data: {
    cats: [],
    photos: [],
    filteredPhotos: [],
    previewVisible: false,
    previewPhoto: {}
  },

  onShow() {
    this.loadData();
  },

  loadData() {
    const cats = app.globalData.cats || [];
    this.setData({ cats });

    // 示例照片数据（后续替换为用户上传 + 猫咪真实照片）
    const samplePhotos = cats.map(cat => ({
      id: `photo_${cat.id}`,
      catId: cat.id,
      catName: cat.name,
      url: cat.photo || '/assets/default-cat.png',
      date: '2024年春',
      author: '热心同学'
    }));

    this.setData({ 
      photos: samplePhotos,
      filteredPhotos: samplePhotos
    });
  },

  filterByCat(e) {
    const catId = e.currentTarget.dataset.id;
    if (!catId) {
      this.setData({ filteredPhotos: this.data.photos });
    } else {
      const filtered = this.data.photos.filter(p => p.catId === catId);
      this.setData({ filteredPhotos: filtered });
    }
  },

  viewPhoto(e) {
    const photo = e.currentTarget.dataset.photo;
    this.setData({ previewVisible: true, previewPhoto: photo });
  },

  closePreview() {
    this.setData({ previewVisible: false });
  }
});
