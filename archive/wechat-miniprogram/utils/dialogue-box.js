// utils/dialogue-box.js — Undertale 风格对话框组件（小程序Canvas版）
//
// 功能：
// - 黑底白字对话框（底部覆盖层）
// - 逐字打字机效果
// - * 星号旁白 / 角色名对话
// - 点击继续 / 跳过打字
// - 选项分支（▶ FIGHT / ACT / ITEM / MERCY 风格）
// - 像素字体 + 无抗锯齿
//
// 使用方法：
//   const dialogue = new DialogueBox(ctx, viewW, viewH);
//   dialogue.start(sceneData, onEndCallback);

class DialogueBox {
  constructor(ctx, viewW, viewH) {
    this.ctx = ctx;
    this.viewW = viewW;
    this.viewH = viewH;

    // 对话框尺寸
    this.boxH = Math.floor(viewH * 0.28);
    this.boxY = viewH - this.boxH;
    this.padding = 20;
    this.fontSize = 14;
    this.lineHeight = 20;

    // 状态
    this.scene = null;
    this.lineIndex = 0;
    this.charIndex = 0;
    this.isTyping = false;
    this.isWaitingForChoice = false;
    this.isActive = false;
    this.typingTimer = 0;
    this.typingSpeed = 0.03; // 秒/字
    this.showContinueArrow = false;
    this.arrowBlink = 0;

    // 回调
    this.onEnd = null;
    this.onTrigger = null;

    // 当前行
    this.currentSpeaker = '';
    this.currentText = '';
    this.currentType = 'dialogue';
    this.choices = [];
  }

  /**
   * 开始一段对话
   */
  start(scene, onEnd, onTrigger) {
    this.scene = scene;
    this.lineIndex = 0;
    this.isActive = true;
    this.isWaitingForChoice = false;
    this.onEnd = onEnd || null;
    this.onTrigger = onTrigger || null;
    this._showCurrentLine();
  }

  /**
   * 显示当前行
   */
  _showCurrentLine() {
    if (!this.scene || this.lineIndex >= this.scene.lines.length) {
      this._endDialogue();
      return;
    }

    const line = this.scene.lines[this.lineIndex];
    this.currentSpeaker = line.speaker || '';
    this.currentText = line.text || '';
    this.currentType = line.type || 'dialogue';
    this.choices = line.choices || [];
    this.showContinueArrow = false;
    this.isWaitingForChoice = false;

    // 处理 trigger 类型
    if (this.currentType === 'trigger') {
      if (this.onTrigger) this.onTrigger(this.currentText);
      this.lineIndex++;
      this._showCurrentLine();
      return;
    }

    // 处理 choice 类型
    if (this.currentType === 'choice') {
      this.isWaitingForChoice = true;
      this.currentText = '';
      return;
    }

    // 普通对话 / 旁白 —— 开始打字
    this.charIndex = 0;
    this.isTyping = true;
    this.typingTimer = 0;
  }

  /**
   * 每帧更新
   */
  update(dt) {
    if (!this.isActive) return;

    // 打字机效果
    if (this.isTyping) {
      this.typingTimer += dt;
      const prefix = this.currentType === 'narration' ? '* ' : '';
      const fullText = prefix + this.currentText;

      while (this.typingTimer >= this.typingSpeed && this.charIndex < fullText.length) {
        this.charIndex++;
        this.typingTimer -= this.typingSpeed;
      }

      if (this.charIndex >= fullText.length) {
        this.isTyping = false;
        this.showContinueArrow = true;
      }
    }

    // 箭头闪烁
    if (this.showContinueArrow) {
      this.arrowBlink += dt * 4;
    }
  }

  /**
   * 渲染对话框
   */
  render() {
    if (!this.isActive) return;
    const ctx = this.ctx;
    ctx.save();
    ctx.imageSmoothingEnabled = false;

    // === 黑色对话框背景 ===
    ctx.fillStyle = '#000000';
    ctx.fillRect(0, this.boxY, this.viewW, this.boxH);

    // === 白色粗边框 ===
    ctx.strokeStyle = '#FFFFFF';
    ctx.lineWidth = 3;
    ctx.strokeRect(2, this.boxY + 2, this.viewW - 4, this.boxH - 4);

    // === 说话者名字 ===
    if (this.currentSpeaker && !this.isWaitingForChoice) {
      ctx.fillStyle = '#FF0000';
      ctx.font = `bold ${this.fontSize}px "Courier New", monospace`;
      ctx.textAlign = 'left';
      ctx.fillText(this.currentSpeaker, this.padding, this.boxY + this.padding + this.fontSize);
    }

    // === 对话文本（逐字） ===
    const prefix = this.currentType === 'narration' ? '* ' : '';
    const fullText = prefix + this.currentText;
    const displayText = fullText.slice(0, this.charIndex);

    ctx.fillStyle = '#FFFFFF';
    ctx.font = `${this.fontSize}px "Courier New", monospace`;
    ctx.textAlign = 'left';

    // 自动换行
    const textX = this.padding;
    const textY = this.currentSpeaker
      ? this.boxY + this.padding + this.fontSize + this.lineHeight
      : this.boxY + this.padding + this.fontSize;
    const maxWidth = this.viewW - this.padding * 2;

    this._wrapText(ctx, displayText, textX, textY, maxWidth, this.lineHeight);

    // === 继续箭头 ▼ ===
    if (this.showContinueArrow && Math.sin(this.arrowBlink) > 0) {
      ctx.fillStyle = '#FFFFFF';
      ctx.font = `${this.fontSize}px "Courier New", monospace`;
      ctx.textAlign = 'right';
      ctx.fillText('▼', this.viewW - this.padding - 10, this.boxY + this.boxH - 12);
    }

    // === 选项（choice模式） ===
    if (this.isWaitingForChoice && this.choices.length > 0) {
      const choiceStartY = this.boxY + this.padding + this.fontSize + 8;
      ctx.fillStyle = '#FFD700'; // 金黄色选项
      ctx.font = `${this.fontSize}px "Courier New", monospace`;
      ctx.textAlign = 'left';

      this.choices.forEach((choice, i) => {
        ctx.fillText(`▶ ${choice.label}`, this.padding + 16, choiceStartY + i * (this.lineHeight + 8));
      });
    }

    ctx.restore();
  }

  /**
   * 文字自动换行
   */
  _wrapText(ctx, text, x, y, maxWidth, lineHeight) {
    const words = text.split('');
    let line = '';
    let lineY = y;

    for (let i = 0; i < words.length; i++) {
      const testLine = line + words[i];
      const metrics = ctx.measureText(testLine);

      if (metrics.width > maxWidth && line.length > 0) {
        ctx.fillText(line, x, lineY);
        line = words[i];
        lineY += lineHeight;
      } else {
        line = testLine;
      }
    }
    if (line.length > 0) {
      ctx.fillText(line, x, lineY);
    }
  }

  /**
   * 处理点击/按键
   */
  handleTap(tapX, tapY) {
    if (!this.isActive) return false;

    // 选项模式下检测选项点击
    if (this.isWaitingForChoice && this.choices.length > 0) {
      const choiceStartY = this.boxY + this.padding + this.fontSize + 8;
      const tapInBox = tapY >= this.boxY;
      if (!tapInBox) return false;

      const choiceIndex = Math.floor((tapY - choiceStartY) / (this.lineHeight + 8));
      if (choiceIndex >= 0 && choiceIndex < this.choices.length) {
        const nextLine = this.choices[choiceIndex].nextLineIndex;
        this.lineIndex = nextLine >= 0 ? nextLine : this.lineIndex + 1;
        this._showCurrentLine();
        return true;
      }
      return false;
    }

    // 打字中 → 跳过
    if (this.isTyping) {
      this.charIndex = (this.currentType === 'narration' ? '* ' : '') + this.currentText.length;
      this.isTyping = false;
      this.showContinueArrow = true;
      return true;
    }

    // 等待继续 → 下一句
    if (this.showContinueArrow) {
      this.lineIndex++;
      this._showCurrentLine();
      return true;
    }

    return false;
  }

  /**
   * 结束对话
   */
  _endDialogue() {
    this.isActive = false;
    this.scene = null;
    if (this.onEnd) {
      const cb = this.onEnd;
      this.onEnd = null;
      cb();
    }
  }

  /**
   * 强制结束
   */
  forceEnd() {
    this._endDialogue();
  }

  /**
   * 从JSON数据加载场景
   */
  static loadScene(jsonStr) {
    try {
      return JSON.parse(jsonStr);
    } catch (e) {
      console.error('对话数据解析失败:', e);
      return null;
    }
  }
}

module.exports = DialogueBox;
