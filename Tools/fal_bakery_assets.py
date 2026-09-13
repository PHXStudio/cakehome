#!/usr/bin/env python3
"""
fal.ai Bakery Asset Pipeline Skill

用途：
- 输入一张参考图（照片、3D渲染、概念图）
- 用 fal.ai Flux 进行风格迁移，生成「暖调治愈烘焙风」的完整竖版室内场景（支持多等级）
- 自动使用 Grounded-SAM 识别并抠出家具/物件，支持**异形边缘 + alpha 通道**
- 低等级自动做 ghost（半透明、柔化）效果，高等级更精致
- 输出可直接在 Unity 中替换的部件

安装：
    pip install fal-client pillow requests

环境变量：
    export FAL_KEY=你的_fal_ai_key

基本用法：
    python tools/fal_bakery_assets.py \
        --input reference_photo.jpg \
        --output-dir bakery_output \
        --style "warm cozy healing bakery interior, soft lighting, pastel colors, Yummy Tales style, detailed 2d game art" \
        --objects "wooden counter" "cake display" "tall right cabinet" "dining table set" "small shelf unit"

输出结构示例：
bakery_output/
├── full/
│   ├── tier1.png          # 最粗糙 / 装修中
│   ├── tier2.png
│   ├── tier3.png
│   └── tier4.png          # 最精致
└── objects/
    ├── counter/
    │   ├── tier1.png
    │   └── tier4.png
    ├── display/
    └── ...

Unity 使用建议：
- Background 用对应 tier 的 full 图像（或干净背景）
- 5 个 Building Image 用对应 tier 的带 alpha 物件
- 低 tier 物件 alpha 低 → ghost 装修状态
"""

import argparse
import base64
import os
import sys
from io import BytesIO
from pathlib import Path

import requests
from PIL import Image, ImageEnhance, ImageFilter, ImageDraw

try:
    import fal_client
except ImportError:
    print("请先执行：pip install fal-client pillow requests")
    raise

# 支持通过命令行传入 key，如果没设置环境变量则使用
def setup_fal_key(provided_key: str | None = None):
    if provided_key:
        os.environ["FAL_KEY"] = provided_key
    elif not os.getenv("FAL_KEY"):
        print("警告: 未设置 FAL_KEY 环境变量，也未通过 --fal-key 提供 key。")
        print("请设置环境变量: export FAL_KEY=你的key")
        print("或使用参数: --fal-key 你的key")


def image_to_data_url(path: str) -> str:
    with open(path, "rb") as f:
        b64 = base64.b64encode(f.read()).decode("utf-8")
    ext = Path(path).suffix.lower().lstrip(".")
    if ext == "jpg":
        ext = "jpeg"
    return f"data:image/{ext};base64,{b64}"


def download_pil(url: str) -> Image.Image:
    r = requests.get(url, timeout=180)
    r.raise_for_status()
    return Image.open(BytesIO(r.content)).convert("RGBA")


def prepare_vertical_input(input_path: str, width: int = 768, height: int = 1344, tmp_dir: str = "bakery_fal_output/_tmp") -> str:
    """把参考图裁剪/缩放成竖屏比例并保存，供 img2img 使用（Flux 输出尺寸跟随输入图）"""
    os.makedirs(tmp_dir, exist_ok=True)
    im = Image.open(input_path).convert("RGB")

    target_ratio = height / width          # 例如 1344/768 ≈ 1.75
    src_ratio = im.size[1] / im.size[0]

    if src_ratio < target_ratio:
        # 原图太宽 → 按高度适配后裁剪左右
        new_w = int(im.size[1] / target_ratio)
        left = (im.size[0] - new_w) // 2
        im = im.crop((left, 0, left + new_w, im.size[1]))
    else:
        # 原图太高 → 按宽度适配后裁剪上下
        new_h = int(im.size[0] * target_ratio)
        top = (im.size[1] - new_h) // 2
        im = im.crop((0, top, im.size[0], top + new_h))

    im = im.resize((width, height), Image.LANCZOS)
    tmp_path = os.path.join(tmp_dir, "vertical_input.png")
    im.save(tmp_path)
    return tmp_path


def generate_full_scene(
    input_path: str,
    prompt: str,
    strength: float = 0.75,
    steps: int = 28,
    width: int = 768,
    height: int = 1344,  # 竖屏 9:16 左右
) -> Image.Image:
    """风格迁移生成完整竖版场景（强制竖屏单张）"""
    # Flux img2img 输出跟随输入图尺寸 → 先把输入处理成竖屏
    vertical_input = prepare_vertical_input(input_path, width, height)

    result = fal_client.subscribe(
        "fal-ai/flux/dev/image-to-image",
        arguments={
            "image_url": image_to_data_url(vertical_input),
            "prompt": prompt,
            "strength": strength,
            "guidance_scale": 3.5,
            "num_inference_steps": steps,
            "output_format": "png",
        },
    )
    print("DEBUG: fal 返回类型:", type(result))
    if isinstance(result, dict):
        print("DEBUG: fal 返回 keys:", list(result.keys()))
        url = None
        if "image" in result and isinstance(result["image"], dict):
            url = result["image"].get("url")
        elif "images" in result and result["images"]:
            url = result["images"][0].get("url") if isinstance(result["images"][0], dict) else result["images"][0]
        if not url:
            print("DEBUG: 完整返回内容:", result)
            raise KeyError("No image url found in response")
    else:
        print("DEBUG: 完整返回:", result)
        raise KeyError("Unexpected response type from fal")
    return download_pil(url)


def cut_subject_from_file(image_path: str, box: tuple, pad_ratio: float = 0.08) -> Image.Image:
    """从本地图按 (left, top, right, bottom) 裁出区域，送 BiRefNet 抠出带 alpha 的主体。

    BiRefNet 假设图里前景即要保留主体 → 我们把每个家具的区域裁出来当独立图处理。
    返回 RGBA（透明背景），异形边缘由模型保证。
    """
    full = Image.open(image_path).convert("RGBA")
    w, h = full.size
    l, t, r, b = box
    # 加一点外边距，避免切断家具
    pw, ph = int(w * pad_ratio), int(h * pad_ratio)
    l = max(0, l - pw); t = max(0, t - ph)
    r = min(w, r + pw); b = min(h, b + ph)
    crop = full.crop((l, t, r, b))
    crop_path = image_path.replace(".png", f"_crop_{l}_{t}.png")
    crop.save(crop_path)

    result = fal_client.subscribe(
        "fal-ai/birefnet/v2",
        arguments={
            "image_url": image_to_data_url(crop_path),
            "operate_type": "remove",
            "output_format": "png",
        },
    )
    # BiRefNet 返回去除背景后的图（RGBA）
    if isinstance(result, dict):
        img_url = None
        if "image" in result and isinstance(result["image"], dict):
            img_url = result["image"].get("url")
        elif "images" in result and result["images"]:
            img_url = result["images"][0].get("url") if isinstance(result["images"][0], dict) else result["images"][0]
        if not img_url:
            print("  [BiRefNet] 无 image url，返回 keys:", list(result.keys()))
            return None
    else:
        return None
    subject = download_pil(img_url)  # RGBA
    return subject


def create_soft_mask_from_box(piece: Image.Image, feather: int = 9) -> Image.Image:
    """当 SAM 效果不好时，退化到带柔边的矩形 mask（仍然比硬切好）"""
    w, h = piece.size
    mask = Image.new("L", (w, h), 255)
    draw = ImageDraw.Draw(mask)
    margin = max(8, min(w, h) // 18)
    draw.rectangle([margin, margin, w - margin, h - margin], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(feather))
    return mask


def extract_object(
    full_image: Image.Image,
    mask_info: dict,
    feather: int = 8,
) -> Image.Image:
    """根据 mask 抠图 + 软边缘"""
    mask_url = mask_info.get("mask") or mask_info.get("url")
    if not mask_url:
        # 退化方案
        # 这里我们用全图的 bounding box 简单处理（实际项目建议手动精修）
        return full_image.copy()

    mask = download_pil(mask_url).convert("L")
    if mask.size != full_image.size:
        mask = mask.resize(full_image.size, Image.LANCZOS)

    mask = mask.filter(ImageFilter.GaussianBlur(feather))

    r, g, b, _ = full_image.split()
    return Image.merge("RGBA", (r, g, b, mask))


def apply_tier_look(img: Image.Image, tier: int, max_tier: int = 4) -> Image.Image:
    """低等级做 ghost + 柔化，高等级保持精致"""
    if tier >= max_tier:
        return img

    # 降低不透明度（ghost 效果）
    ghost_alpha = 0.28 + (tier - 1) * 0.22   # tier1≈0.28, tier3≈0.72
    r, g, b, a = img.split()
    a = a.point(lambda x: int(x * ghost_alpha))

    # 颜色和细节降级
    rgb = Image.merge("RGB", (r, g, b))
    factor = 0.55 + (tier - 1) * 0.15
    rgb = ImageEnhance.Color(rgb).enhance(factor)
    rgb = ImageEnhance.Contrast(rgb).enhance(0.72 + (tier - 1) * 0.09)

    if tier <= 2:
        rgb = rgb.filter(ImageFilter.GaussianBlur(radius=1.2))

    return Image.merge("RGBA", (*rgb.split(), a))


def main():
    parser = argparse.ArgumentParser(description="fal.ai 烘焙店完整场景 + 异形抠图工具")
    parser.add_argument("--input", "-i", required=True, help="输入参考图路径")
    parser.add_argument("--output-dir", "-o", default="bakery_fal_output")
    parser.add_argument("--style", default="warm cozy healing bakery interior, soft lighting, pastel colors, Yummy Tales style, detailed 2d game art")
    parser.add_argument("--objects", nargs="+", default=[
        "wooden counter", "cake display shelf", "tall right cabinet",
        "dining table area", "small tower shelf"
    ])
    parser.add_argument("--tiers", type=int, default=4)
    parser.add_argument("--prefix", default="shop1")
    parser.add_argument("--fal-key", help="直接传入 fal.ai key（优先级高于环境变量）")
    args = parser.parse_args()

    # 如果用户提供了 key，就设置它
    if args.fal_key:
        os.environ["FAL_KEY"] = args.fal_key

    # 校验 key 是否存在
    if not os.getenv("FAL_KEY"):
        print("错误：未找到 FAL_KEY")
        print("请使用以下两种方式之一提供 key：")
        print("  1. 环境变量: set FAL_KEY=你的key   (Windows)")
        print("  2. 参数: --fal-key 你的key")
        sys.exit(1)

    os.makedirs(args.output_dir, exist_ok=True)
    full_dir = os.path.join(args.output_dir, "full")
    obj_dir = os.path.join(args.output_dir, "objects")
    os.makedirs(full_dir, exist_ok=True)
    os.makedirs(obj_dir, exist_ok=True)

    print(f"输入: {args.input}")
    print(f"输出: {args.output_dir}")
    print(f"风格: {args.style}")
    print(f"物件: {args.objects}\n")

    for tier in range(1, args.tiers + 1):
        tier_strength = 0.68 + (tier - 1) * 0.06
        tier_prompt = f"{args.style}, tier {tier} quality"

        print(f"[Tier {tier}] 生成完整场景 (竖屏单张)...")
        full_img = generate_full_scene(
            args.input,
            tier_prompt,
            strength=tier_strength,
            width=768,
            height=1344,   # 强制竖屏 9:16 左右比例，单张场景
        )
        full_path = os.path.join(full_dir, f"{args.prefix}_full_tier{tier}.png")
        full_img.save(full_path)
        print(f"  → {full_path}  ({full_img.size[0]}x{full_img.size[1]})")

        print(f"[Tier {tier}] 抠取物件...")
        seg_results = segment_objects(full_path, args.objects)

        for obj_name in args.objects:
            safe = obj_name.replace(" ", "_").replace("/", "_")
            out_folder = os.path.join(obj_dir, safe)
            os.makedirs(out_folder, exist_ok=True)

            if obj_name not in seg_results:
                # 退化：直接裁剪大区域（用户后续可手动精修）
                # 这里简单用固定区域示例，实际建议用户提供更好的分割提示
                print(f"  {obj_name} 未检测到，使用整图区域（请手动精修）")
                # 为了不让流程中断，先跳过或用全图
                continue

            try:
                cutout = extract_object(full_img, seg_results[obj_name])
                cutout = apply_tier_look(cutout, tier, args.tiers)

                out_path = os.path.join(out_folder, f"{args.prefix}_{safe}_tier{tier}.png")
                cutout.save(out_path)
                print(f"  → {out_path}")
            except Exception as e:
                print(f"  {obj_name} 处理失败: {e}")

    print("\n全部完成！")
    print("使用建议：")
    print("  - full/ 目录可直接作为各等级背景")
    print("  - objects/ 下的带 alpha 图片可叠在背景上实现升级替换")
    print("  - Tier1/2 低 alpha = ghost 装修状态")
    print("  - 如果自动抠边不够理想，推荐用 full_tier4.png 手动在 PS 抠一次干净 alpha，然后批量生成低等级版本。")


if __name__ == "__main__":
    main()
