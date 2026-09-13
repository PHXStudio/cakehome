# fal.ai Bakery Assets Skill

用于将参考图片风格化成「暖调治愈烘焙风」完整竖版室内场景，并自动抠出家具/物件（支持异形 + alpha）。

## 安装

```bash
pip install fal-client pillow requests
```

## 环境变量

```bash
export FAL_KEY=你的_fal_key
```

## 基本用法

```bash
python tools/fal_bakery_assets.py \
  --input your_reference.jpg \
  --output-dir bakery_output \
  --style "warm cozy healing bakery interior, soft lighting, pastel colors, Yummy Tales style, detailed 2d game art" \
  --objects "wooden counter" "cake display shelf" "tall right cabinet" "dining table area" "small tower shelf" \
  --tiers 4
```

## 输出结构

```
bakery_output/
├── full/
│   ├── shop1_full_tier1.png   # 最粗糙 / 装修中
│   ├── shop1_full_tier2.png
│   ├── shop1_full_tier3.png
│   └── shop1_full_tier4.png   # 最精致
└── objects/
    ├── counter/
    │   ├── shop1_counter_1.png
    │   └── shop1_counter_4.png
    ├── display/
    └── ...
```

## 特点

- 完整竖版场景先出图（保证透视、光影一致）
- 使用 Grounded-SAM 做异形抠图（非矩形）
- 低 tier 自动 ghost（低透明 + 柔化），高 tier 精致
- 直接替换原来 shop1_xxx_N.png 即可

## 注意

自动抠图效果取决于参考图质量和提示词。必要时可以用 tier4 的完整图手动在 PS 精修 alpha，然后再用脚本生成低等级版本。
