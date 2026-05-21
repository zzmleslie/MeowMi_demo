#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
╔══════════════════════════════════════════════════╗
║     🐱 南雍猫札 - 猫咪数据爬取工具              ║
║     Nanjing University Cat Data Scraper         ║
╚══════════════════════════════════════════════════╝

功能：
  1. 从南雍猫札微信小程序抓取猫咪信息
  2. 支持多种数据源：HAR抓包文件 / API直连 / 手动录入
  3. 自动下载猫咪照片
  4. 导出为游戏可用的 JSON 格式
  5. 提供 GUI 界面方便操作

使用方法：
  【方法一：从抓包文件导入（推荐）】
    1. 用 Charles/Fiddler/mitmproxy 抓取小程序的网络请求
    2. 导出为 HAR 格式 (.har) 或 JSON 格式
    3. 运行本脚本，选择文件导入
  
  【方法二：手动配置API】
    如果知道小程序的API地址，直接填入配置抓取
  
  【方法三：GUI模式】
    python scraper.py --gui

依赖安装：
  pip install requests pillow mitmproxy

作者：南雍猫札开发组
"""

import os
import sys
import json
import hashlib
import time
import re
import argparse
from pathlib import Path
from datetime import datetime
from typing import Optional
from urllib.parse import urlparse, parse_qs

try:
    import requests
    from PIL import Image
    HAS_DEPS = True
except ImportError:
    HAS_DEPS = False
    print("⚠️  缺少依赖，请运行: pip install requests pillow")


# ============================================================
# 配置
# ============================================================

class Config:
    """爬虫配置"""
    # 输出目录
    OUTPUT_DIR = Path(__file__).parent.parent / "output"
    CATS_JSON = OUTPUT_DIR / "cats-scraped.json"
    PHOTOS_DIR = OUTPUT_DIR / "cat-photos"

    # 已知的小程序可能API（需要抓包确认后填入）
    # 南雍猫札小程序 appid（如果能找到的话）
    MINIPROGRAM_APPID = ""  # 待填入
    API_BASE = ""           # 待填入

    # 猫咪数据结构模板
    CAT_TEMPLATE = {
        "id": "",
        "name": "",
        "nickname": "",
        "gender": "unknown",      # male / female / unknown
        "color": "",
        "breed": "中华田园猫",
        "neutered": False,
        "adopted": False,
        "description": "",
        "hangout": "",            # 常出没地点
        "photo": "",              # 照片本地路径
        "photoUrl": "",           # 照片原始URL
        "relations": [],          # [{catId, relation, name}]
        "tags": [],
        "firstSeen": "",
        "lastSeen": "",
        "campusArea": "鼓楼校区",
        "source": "南雍猫札小程序"
    }


# ============================================================
# 数据源：HAR 文件解析
# ============================================================

class HARParser:
    """
    解析 HTTP Archive (HAR) 格式的抓包文件
    Charles Proxy / Chrome DevTools 都可以导出 HAR
    """

    def __init__(self, file_path: str):
        self.file_path = file_path
        self.entries = []

    def load(self) -> list:
        """加载 HAR 文件"""
        with open(self.file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        self.entries = data.get('log', {}).get('entries', [])
        print(f"📄 加载了 {len(self.entries)} 条网络请求")
        return self.entries

    def find_api_calls(self, url_pattern: str = None) -> list:
        """查找匹配的API请求"""
        results = []
        for entry in self.entries:
            request = entry.get('request', {})
            url = request.get('url', '')

            # 查找包含猫咪相关关键词的请求
            if any(kw in url.lower() for kw in ['cat', 'mao', 'pet', 'animal', 'api']):
                results.append(entry)

        print(f"🔍 找到 {len(results)} 条疑似猫咪数据API请求")
        return results

    def extract_json_responses(self) -> list:
        """提取所有JSON响应"""
        responses = []
        for entry in self.entries:
            response = entry.get('response', {})
            content = response.get('content', {})
            mime_type = content.get('mimeType', '')
            text = content.get('text', '')

            if 'json' in mime_type and text:
                try:
                    data = json.loads(text)
                    responses.append({
                        'url': entry['request']['url'],
                        'data': data
                    })
                except json.JSONDecodeError:
                    pass

        print(f"📦 提取了 {len(responses)} 条JSON响应")
        return responses


# ============================================================
# 数据源：通用 JSON 导入
# ============================================================

class JSONImporter:
    """从任意 JSON 文件导入猫咪数据"""

    @staticmethod
    def import_from_file(file_path: str) -> list:
        """导入JSON文件，尝试自动识别猫咪数据"""
        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        cats = []
        # 尝试多种数据结构
        if isinstance(data, list):
            for item in data:
                if JSONImporter._looks_like_cat(item):
                    cats.append(item)
        elif isinstance(data, dict):
            # 可能是 { cats: [...] } 或 { data: { cats: [...] } }
            for key in ['cats', 'data', 'list', 'items', 'animals', 'result']:
                if key in data and isinstance(data[key], list):
                    for item in data[key]:
                        if JSONImporter._looks_like_cat(item):
                            cats.append(item)
                    if cats:
                        break
            # 也可能是单个猫咪对象
            if not cats and JSONImporter._looks_like_cat(data):
                cats.append(data)

        print(f"📥 从 JSON 导入了 {len(cats)} 只猫咪")
        return cats

    @staticmethod
    def _looks_like_cat(obj: dict) -> bool:
        """启发式判断一个对象是否像猫咪数据"""
        cat_keywords = ['name', 'cat', '猫', 'color', '花色', 'gender', '性别', 'breed']
        score = sum(1 for k in cat_keywords if k in str(obj).lower())
        return score >= 2


# ============================================================
# 猫咪数据标准化器
# ============================================================

class CatNormalizer:
    """将不同来源的猫咪数据统一为标准格式"""

    # 字段映射表：源字段名 → 标准字段名
    FIELD_MAP = {
        # 名称相关
        'name': 'name', 'catName': 'name', 'cat_name': 'name',
        '名字': 'name', '猫咪名': 'name', '姓名': 'name', 'title': 'name',
        # 昵称
        'nickname': 'nickname', 'nick': 'nickname', '别名': 'nickname', '外号': 'nickname',
        # 性别
        'gender': 'gender', 'sex': 'gender', '性别': 'gender',
        # 花色
        'color': 'color', 'coat': 'color', 'furColor': 'color',
        '花色': 'color', '毛色': 'color', '颜色': 'color',
        # 绝育
        'neutered': 'neutered', 'sterilized': 'neutered', 'desexed': 'neutered',
        '绝育': 'neutered', '已绝育': 'neutered', 'isNeutered': 'neutered',
        # 收养
        'adopted': 'adopted', 'isAdopted': 'adopted', '收养': 'adopted', '已收养': 'adopted',
        # 描述
        'description': 'description', 'desc': 'description', 'intro': 'description',
        '描述': 'description', '简介': 'description', '性格': 'description',
        'character': 'description', 'personality': 'description',
        # 照片
        'photo': 'photo', 'photoUrl': 'photoUrl', 'image': 'photoUrl',
        'avatar': 'photoUrl', 'pic': 'photoUrl', '照片': 'photoUrl', '图片': 'photoUrl',
        'cover': 'photoUrl', 'img': 'photoUrl', 'imageUrl': 'photoUrl',
        # 出没地
        'hangout': 'hangout', 'location': 'hangout', 'place': 'hangout',
        '出没': 'hangout', '地点': 'hangout', '常出没': 'hangout',
        # 标签
        'tags': 'tags', 'labels': 'tags', '标签': 'tags',
        # 关系
        'relations': 'relations', 'family': 'relations', '亲属': 'relations',
        # 首次发现
        'firstSeen': 'firstSeen', 'first_seen': 'firstSeen', 'foundDate': 'firstSeen',
        '发现时间': 'firstSeen', '首次发现': 'firstSeen',
    }

    @classmethod
    def normalize(cls, raw: dict) -> dict:
        """将原始数据标准化"""
        cat = dict(Config.CAT_TEMPLATE)

        # 遍历原始数据的所有字段
        for key, value in raw.items():
            # 查找字段映射
            target = cls.FIELD_MAP.get(key, cls.FIELD_MAP.get(key.lower(), None))
            if target:
                cat[target] = cls._convert_value(target, value)

        # 生成唯一ID
        if not cat['id']:
            name_hash = hashlib.md5(
                (cat['name'] + cat.get('photoUrl', '')).encode()
            ).hexdigest()[:8]
            cat['id'] = f"cat_{name_hash}"

        # 标准化性别
        gender = str(cat['gender']).lower()
        if gender in ['male', '男', '公', '♂', 'boy', 'm', '1', 'true']:
            cat['gender'] = 'male'
        elif gender in ['female', '女', '母', '♀', 'girl', 'f', '0', 'false']:
            cat['gender'] = 'female'
        else:
            cat['gender'] = 'unknown'

        # 标准化布尔值
        cat['neutered'] = cls._parse_bool(cat['neutered'])
        cat['adopted'] = cls._parse_bool(cat['adopted'])

        # 标准化关系数据
        if isinstance(cat['relations'], str):
            cat['relations'] = cls._parse_relations_string(cat['relations'])

        return cat

    @classmethod
    def _convert_value(cls, target_field: str, value):
        """类型转换"""
        if target_field in ['tags'] and isinstance(value, str):
            return [t.strip() for t in value.split(',') if t.strip()]
        if target_field in ['relations'] and isinstance(value, str):
            return value  # 后续在 normalize 中处理
        return value

    @classmethod
    def _parse_bool(cls, value) -> bool:
        """解析布尔值"""
        if isinstance(value, bool):
            return value
        if isinstance(value, (int, float)):
            return bool(value)
        if isinstance(value, str):
            return value.lower() in ['true', 'yes', '1', '是', '已', 'done', 'ok']
        return False

    @classmethod
    def _parse_relations_string(cls, text: str) -> list:
        """解析关系文本，如 '父亲:大黄, 母亲:花花'"""
        relations = []
        parts = re.split(r'[,，;；、\n]', text)
        for part in parts:
            part = part.strip()
            if not part:
                continue
            if ':' in part or '：' in part:
                rel, name = re.split(r'[:：]', part, 1)
                relations.append({
                    'catId': '',
                    'relation': rel.strip(),
                    'name': name.strip()
                })
            else:
                relations.append({
                    'catId': '',
                    'relation': '关联',
                    'name': part
                })
        return relations


# ============================================================
# 图片下载器
# ============================================================

class ImageDownloader:
    """下载猫咪照片"""

    def __init__(self, output_dir: Path):
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) '
                          'AppleWebKit/537.36 (KHTML, like Gecko) '
                          'Chrome/120.0.0.0 Safari/537.36'
        })

    def download(self, url: str, cat_id: str) -> Optional[str]:
        """下载单张图片，返回本地路径"""
        if not url:
            return None

        # 确定文件扩展名
        ext = '.jpg'
        for e in ['.png', '.jpg', '.jpeg', '.webp', '.gif']:
            if e in url.lower().split('?')[0]:
                ext = e
                break

        filename = f"{cat_id}{ext}"
        filepath = self.output_dir / filename

        # 如果已存在则跳过
        if filepath.exists():
            print(f"  ⏭️  跳过已存在: {filename}")
            return str(filepath)

        try:
            print(f"  📥 下载: {url[:80]}...")
            resp = self.session.get(url, timeout=30, stream=True)
            resp.raise_for_status()

            with open(filepath, 'wb') as f:
                for chunk in resp.iter_content(8192):
                    f.write(chunk)

            # 压缩大图
            self._compress_if_needed(filepath)

            print(f"  ✅ 保存: {filename}")
            return str(filepath)

        except Exception as e:
            print(f"  ❌ 下载失败: {e}")
            return None

    def _compress_if_needed(self, filepath: Path, max_size: int = 500):
        """如果图片太大，压缩到合适尺寸"""
        try:
            img = Image.open(filepath)
            w, h = img.size
            if max(w, h) > max_size:
                ratio = max_size / max(w, h)
                new_size = (int(w * ratio), int(h * ratio))
                img = img.resize(new_size, Image.LANCZOS)
                img.save(filepath, quality=85, optimize=True)
                print(f"    📐 压缩: {w}x{h} → {new_size[0]}x{new_size[1]}")
        except Exception:
            pass


# ============================================================
# 数据导出器
# ============================================================

class DataExporter:
    """将处理好的数据导出为各种格式"""

    @staticmethod
    def to_game_json(cats: list, output_path: Path) -> str:
        """导出为游戏引擎可用的 JSON 格式"""
        output = {
            "_meta": {
                "version": "1.0.0",
                "source": "南雍猫札小程序爬取",
                "exportTime": datetime.now().isoformat(),
                "totalCats": len(cats)
            },
            "cats": cats
        }

        output_path.parent.mkdir(parents=True, exist_ok=True)
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(output, f, ensure_ascii=False, indent=2)

        print(f"\n✨ 导出成功: {output_path}")
        print(f"🐱 共 {len(cats)} 只猫咪")
        return str(output_path)

    @staticmethod
    def to_csv(cats: list, output_path: Path) -> str:
        """导出为 CSV（方便在Excel中查看）"""
        import csv

        output_path.parent.mkdir(parents=True, exist_ok=True)
        fields = ['id', 'name', 'nickname', 'gender', 'color', 'neutered',
                   'adopted', 'hangout', 'description', 'firstSeen', 'tags']

        with open(output_path, 'w', encoding='utf-8-sig', newline='') as f:
            writer = csv.DictWriter(f, fieldnames=fields, extrasaction='ignore')
            writer.writeheader()
            for cat in cats:
                row = dict(cat)
                row['tags'] = ','.join(row.get('tags', []))
                row['neutered'] = '是' if row.get('neutered') else '否'
                row['adopted'] = '是' if row.get('adopted') else '否'
                writer.writerow(row)

        print(f"📊 CSV导出: {output_path}")
        return str(output_path)


# ============================================================
# GUI 界面（tkinter）
# ============================================================

class ScraperGUI:
    """简单的图形界面"""

    def __init__(self):
        self.root = None
        self.cats = []

    def run(self):
        """启动 GUI"""
        import tkinter as tk
        from tkinter import ttk, filedialog, messagebox, scrolledtext

        self.root = tk.Tk()
        self.root.title("🐱 南雍猫札 - 数据爬取工具")
        self.root.geometry("700x600")
        self.root.configure(bg='#FFF8F0')

        # 标题
        title = tk.Label(
            self.root, text="🐱 南雍猫札 · 猫咪数据爬取工具",
            font=('Microsoft YaHei', 16, 'bold'),
            bg='#FFF8F0', fg='#4A3728'
        )
        title.pack(pady=20)

        # 操作区
        frame = tk.Frame(self.root, bg='#FFF8F0')
        frame.pack(pady=10)

        # 导入 HAR
        btn_style = {'font': ('Microsoft YaHei', 11), 'width': 20, 'height': 2,
                      'bg': '#D4776B', 'fg': 'white', 'border': 0, 'cursor': 'hand2'}

        tk.Button(frame, text="📄 导入 HAR 抓包文件",
                  command=self._import_har, **btn_style).pack(pady=5)

        tk.Button(frame, text="📥 导入 JSON 数据文件",
                  command=self._import_json, **btn_style).pack(pady=5)

        tk.Button(frame, text="📋 手动录入猫咪信息",
                  command=self._manual_input,
                  **{**btn_style, 'bg': '#C9A96E'}).pack(pady=5)

        tk.Label(frame, text="", bg='#FFF8F0').pack()

        tk.Button(frame, text="💾 导出游戏 JSON",
                  command=self._export_json,
                  **{**btn_style, 'bg': '#7BA07B'}).pack(pady=5)

        tk.Button(frame, text="📊 导出 CSV 表格",
                  command=self._export_csv,
                  **{**btn_style, 'bg': '#8B7D6B'}).pack(pady=5)

        # 日志区
        tk.Label(self.root, text="📝 操作日志:", font=('Microsoft YaHei', 10),
                 bg='#FFF8F0', fg='#8B7D6B').pack(anchor='w', padx=30, pady=(20, 5))

        self.log_area = scrolledtext.ScrolledText(
            self.root, height=12, font=('Consolas', 9),
            bg='#FFFAF5', fg='#4A3728', border=2, relief='groove'
        )
        self.log_area.pack(fill='both', expand=True, padx=30, pady=(0, 20))

        self._log("✨ 欢迎使用南雍猫札数据爬取工具！")
        self._log("请选择数据导入方式开始~")

        self.root.mainloop()

    def _log(self, msg: str):
        """输出日志到GUI"""
        if self.log_area:
            self.log_area.insert('end', f"[{datetime.now():%H:%M:%S}] {msg}\n")
            self.log_area.see('end')
        print(msg)

    def _import_har(self):
        """导入 HAR 文件"""
        from tkinter import filedialog, messagebox
        filepath = filedialog.askopenfilename(
            title="选择 HAR 抓包文件",
            filetypes=[("HAR files", "*.har"), ("All files", "*.*")]
        )
        if not filepath:
            return

        self._log(f"📂 加载: {filepath}")
        try:
            parser = HARParser(filepath)
            parser.load()
            api_calls = parser.find_api_calls()
            responses = parser.extract_json_responses()

            for resp in responses:
                data = resp['data']
                if isinstance(data, list):
                    for item in data:
                        cat = CatNormalizer.normalize(item)
                        if cat['name']:
                            self.cats.append(cat)
                elif isinstance(data, dict):
                    cat = CatNormalizer.normalize(data)
                    if cat['name']:
                        self.cats.append(cat)

            self._log(f"✅ 成功解析 {len(self.cats)} 只猫咪数据")
            messagebox.showinfo("导入完成", f"成功导入 {len(self.cats)} 只猫咪！")
        except Exception as e:
            self._log(f"❌ 错误: {e}")
            messagebox.showerror("错误", str(e))

    def _import_json(self):
        """导入 JSON 文件"""
        from tkinter import filedialog, messagebox
        filepath = filedialog.askopenfilename(
            title="选择 JSON 数据文件",
            filetypes=[("JSON files", "*.json"), ("All files", "*.*")]
        )
        if not filepath:
            return

        self._log(f"📂 加载: {filepath}")
        try:
            importer = JSONImporter()
            raw_cats = importer.import_from_file(filepath)

            for raw in raw_cats:
                cat = CatNormalizer.normalize(raw)
                self.cats.append(cat)

            self._log(f"✅ 成功解析 {len(self.cats)} 只猫咪数据")
            messagebox.showinfo("导入完成", f"成功导入 {len(self.cats)} 只猫咪！")
        except Exception as e:
            self._log(f"❌ 错误: {e}")
            messagebox.showerror("错误", str(e))

    def _manual_input(self):
        """手动录入"""
        self._log("📋 手动录入功能待完善，请先用 JSON 或 HAR 导入~")

    def _export_json(self):
        """导出JSON"""
        if not self.cats:
            from tkinter import messagebox
            messagebox.showwarning("无数据", "请先导入猫咪数据！")
            return
        try:
            path = DataExporter.to_game_json(self.cats, Config.CATS_JSON)
            self._log(f"💾 已导出: {path}")
            from tkinter import messagebox
            messagebox.showinfo("导出成功", f"已保存到:\n{path}")
        except Exception as e:
            self._log(f"❌ 导出失败: {e}")

    def _export_csv(self):
        """导出CSV"""
        if not self.cats:
            from tkinter import messagebox
            messagebox.showwarning("无数据", "请先导入猫咪数据！")
            return
        try:
            path = DataExporter.to_csv(self.cats, Config.OUTPUT_DIR / 'cats.csv')
            self._log(f"📊 已导出: {path}")
            from tkinter import messagebox
            messagebox.showinfo("导出成功", f"已保存到:\n{path}")
        except Exception as e:
            self._log(f"❌ 导出失败: {e}")


# ============================================================
# 命令行入口
# ============================================================

def main():
    parser = argparse.ArgumentParser(
        description='🐱 南雍猫札 - 猫咪数据爬取工具',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
使用示例:
  python scraper.py --gui                    # 启动图形界面
  python scraper.py --har capture.har        # 解析抓包文件
  python scraper.py --json data.json         # 导入JSON数据
  python scraper.py --json data.json --download  # 导入并下载图片
        """
    )
    parser.add_argument('--gui', action='store_true', help='启动图形界面')
    parser.add_argument('--har', type=str, help='HAR 抓包文件路径')
    parser.add_argument('--json', type=str, help='JSON 数据文件路径')
    parser.add_argument('--download', action='store_true', help='下载猫咪照片')
    parser.add_argument('--output', type=str, help='输出文件路径')

    args = parser.parse_args()

    # GUI 模式
    if args.gui:
        if not HAS_DEPS:
            print("❌ 请先安装依赖: pip install requests pillow")
            return
        gui = ScraperGUI()
        gui.run()
        return

    # 命令行模式
    cats = []

    if args.har:
        print(f"📂 解析 HAR 文件: {args.har}")
        parser_har = HARParser(args.har)
        parser_har.load()
        responses = parser_har.extract_json_responses()
        for resp in responses:
            data = resp['data']
            items = data if isinstance(data, list) else [data]
            for item in items:
                if isinstance(item, dict):
                    cat = CatNormalizer.normalize(item)
                    if cat['name']:
                        cats.append(cat)

    if args.json:
        print(f"📂 导入 JSON 文件: {args.json}")
        raw_cats = JSONImporter.import_from_file(args.json)
        for raw in raw_cats:
            cat = CatNormalizer.normalize(raw)
            if cat['name']:
                cats.append(cat)

    if not cats:
        print("❌ 未找到任何猫咪数据，请检查输入文件")
        print("💡 提示: 使用 --gui 启动图形界面操作")
        return

    # 下载照片
    if args.download:
        print(f"\n📸 开始下载猫咪照片...")
        downloader = ImageDownloader(Config.PHOTOS_DIR)
        for cat in cats:
            if cat.get('photoUrl'):
                local_path = downloader.download(cat['photoUrl'], cat['id'])
                if local_path:
                    cat['photo'] = local_path

    # 导出
    output_path = args.output or Config.CATS_JSON
    DataExporter.to_game_json(cats, Path(output_path))
    DataExporter.to_csv(cats, Config.OUTPUT_DIR / 'cats.csv')


if __name__ == '__main__':
    main()
