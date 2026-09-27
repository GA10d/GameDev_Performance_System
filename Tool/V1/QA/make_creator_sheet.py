from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).parent / "CreatorScreenshots"
width, height = 230, 310
for angle in ("front", "profile"):
    files = sorted(root.glob(f"creator_*_{angle}.png"))
    sheet = Image.new("RGB", (width * 8, height * 4), "#10140f")
    draw = ImageDraw.Draw(sheet)
    for index, path in enumerate(files):
        image = Image.open(path).convert("RGB")
        image = image.crop((385, 90, 895, 690)).resize((width, height - 24))
        x, y = (index % 8) * width, (index // 8) * height
        sheet.paste(image, (x, y))
        draw.text((x + 8, y + height - 21), path.stem, fill="#d9dcc9")
    sheet.save(root / f"contact_sheet_{angle}.png")
