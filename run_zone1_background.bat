@echo off
REM Run the fal bakery skill for game_background_zone_1
REM Save the attached cake shop image as reference_cake_shop.png first!

set FAL_KEY=2bdcc736-161e-47cd-9e39-2690ac58b835:742b2b9d48784bf8de75646c5e9d87a3

python tools/fal_bakery_assets.py ^
  --input reference_cake_shop.png ^
  --output-dir "Assets/Project Files/Game/Images/Zone 1 Shop/generated_zone1" ^
  --fal-key %FAL_KEY% ^
  --style "warm cozy healing bakery interior, soft lighting, pastel colors, detailed 2d game art, Yummy Tales style, vertical tall composition for mobile game, portrait 9:16" ^
  --objects "counter" "cake display shelf" "tall right cabinet" "dining table with chairs" "small tower shelf" ^
  --tiers 4 ^
  --prefix game_background_zone_1

echo.
echo Done! Check the generated_zone1 folder for full scenes and cut parts with alpha.
pause
