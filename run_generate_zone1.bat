@echo off
chcp 65001 >nul

set FAL_KEY=2bdcc736-161e-47cd-9e39-2690ac58b835:742b2b9d48784bf8de75646c5e9d87a3

echo ============================================
echo Generating game_background_zone_1 using fal.ai
echo ============================================

python tools/fal_bakery_assets.py ^
  --input "reference_cake_shop.png" ^
  --output-dir "Assets/Project Files/Game/Art/_Pending/Zone1Shop" ^
  --fal-key %FAL_KEY% ^
  --style "warm cozy healing bakery shop interior, soft warm lighting, pastel cream and wood tones, cute detailed 2d illustration style like Yummy Tales, clean perspective, tall vertical composition suitable for mobile game background, 9:16 portrait, no people, no text, high quality" ^
  --objects "main wooden counter" "large multi level cake display shelf" "tall wooden right cabinet" "dining table with chairs" "small tower shelf unit" ^
  --tiers 4 ^
  --prefix game_background_zone_1

echo.
echo Done! Check Assets\Project Files\Game\Art\_Pending\Zone1Shop
echo You will get:
echo   - full\*.png          (complete vertical backgrounds per tier)
echo   - objects\*\*.png     (cut out pieces with alpha, irregular shapes)
pause
