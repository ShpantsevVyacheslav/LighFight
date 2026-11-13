#!/usr/bin/env python3
"""
Generates high-resolution PBR textures and wetness maps for Rostov Embankment Arena:
1. granite_albedo.png, granite_normal.png, granite_roughness.png
2. granite_puddle_mask.png (organic wet puddle mask for specular reflections)
3. cast_iron_albedo.png, cast_iron_normal.png
4. water_normal.png (seamless wave normal map)
5. sunset_sky.png (golden hour sky panorama over Don)
6. navigation_sign.png (river navigation rhombus for Voroshilovsky bridge pier)
"""

import math
import os
import random
from PIL import Image, ImageDraw, ImageFilter

OUT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../unity/Assets/LighFight/Art/Arenas/RostovEmbankment/Textures"))
os.makedirs(OUT_DIR, exist_ok=True)

def generate_granite_textures(size=1024):
    print(f"Generating Granite PBR maps ({size}x{size})...")
    random.seed(42)

    height_img = Image.new('L', (size, size), 128)
    draw_h = ImageDraw.Draw(height_img)
    pixels_h = height_img.load()

    for y in range(size):
        for x in range(size):
            n = random.randint(-18, 18)
            pixels_h[x, y] = max(0, min(255, 128 + n))

    height_img = height_img.filter(ImageFilter.GaussianBlur(radius=1.2))
    pixels_h = height_img.load()

    mid = size // 2
    seam_half_w = 6
    for y in range(size):
        for x in range(size):
            dx = abs(x - mid)
            dy = abs(y - mid)
            dx_edge = min(x, size - 1 - x)
            dy_edge = min(y, size - 1 - y)
            d_seam = min(dx, dy, dx_edge, dy_edge)
            if d_seam < seam_half_w:
                factor = (d_seam / seam_half_w)
                pixels_h[x, y] = int(pixels_h[x, y] * (0.35 + 0.65 * factor))
            elif d_seam < seam_half_w + 14:
                bevel = (d_seam - seam_half_w) / 14.0
                pixels_h[x, y] = int(pixels_h[x, y] * (0.82 + 0.18 * bevel))

    # 1. Albedo map (warm Rostov granite)
    albedo_img = Image.new('RGB', (size, size))
    pixels_a = albedo_img.load()
    base_r, base_g, base_b = 168, 148, 138

    for y in range(size):
        for x in range(size):
            h = pixels_h[x, y]
            variation = (h - 128) * 0.45
            r = int(max(0, min(255, base_r + variation)))
            g = int(max(0, min(255, base_g + variation * 0.9)))
            b = int(max(0, min(255, base_b + variation * 0.7)))

            rnd = random.random()
            if rnd < 0.08:
                r, g, b = min(255, r + 42), min(255, g + 40), min(255, b + 38)
            elif rnd < 0.14:
                r, g, b = max(0, r - 55), max(0, g - 55), max(0, b - 50)
            elif rnd < 0.17:
                r, g, b = min(255, r + 50), max(0, g - 15), max(0, b - 15)

            if h < 85:
                r = int(r * 0.45)
                g = int(g * 0.45)
                b = int(b * 0.42)

            pixels_a[x, y] = (r, g, b)

    albedo_path = os.path.join(OUT_DIR, "granite_albedo.png")
    albedo_img.save(albedo_path)

    # 2. Normal map
    normal_img = Image.new('RGB', (size, size))
    pixels_n = normal_img.load()
    strength = 2.2

    for y in range(size):
        ym1 = (y - 1) % size
        yp1 = (y + 1) % size
        for x in range(size):
            xm1 = (x - 1) % size
            xp1 = (x + 1) % size
            dx = (pixels_h[xp1, y] - pixels_h[xm1, y]) / 255.0
            dy = (pixels_h[x, yp1] - pixels_h[x, ym1]) / 255.0
            nx = -dx * strength
            ny = -dy * strength
            nz = 1.0
            length = math.sqrt(nx*nx + ny*ny + nz*nz)
            nx /= length
            ny /= length
            nz /= length
            pixels_n[x, y] = (
                int((nx * 0.5 + 0.5) * 255),
                int((ny * 0.5 + 0.5) * 255),
                int((nz * 0.5 + 0.5) * 255)
            )

    normal_path = os.path.join(OUT_DIR, "granite_normal.png")
    normal_img.save(normal_path)

    # 3. Roughness & Puddle Mask Map (Organic wet puddles reflecting sunset)
    puddle_img = Image.new('L', (size, size), 220) # 220 = dry matte stone
    draw_p = ImageDraw.Draw(puddle_img)

    # Draw organic bezier puddles in centers of slabs
    puddle_centers = [
        (size * 0.28, size * 0.32, 110, 75),
        (size * 0.72, size * 0.26, 95, 60),
        (size * 0.35, size * 0.75, 120, 80),
        (size * 0.78, size * 0.72, 85, 70),
        (mid, mid, 130, 90) # Central splash under bridge
    ]

    for px, py, rx, ry in puddle_centers:
        for r_step in range(int(max(rx, ry)), 0, -4):
            cur_rx = rx * (r_step / max(rx, ry))
            cur_ry = ry * (r_step / max(rx, ry))
            # Black (0) = 100% mirror wet puddle
            val = int(220 * (r_step / max(rx, ry))**1.5)
            draw_p.ellipse([px - cur_rx, py - cur_ry, px + cur_rx, py + cur_ry], fill=val)

    # Add damp joints along seams
    pixels_p = puddle_img.load()
    for y in range(size):
        for x in range(size):
            h = pixels_h[x, y]
            if h < 85:
                # Water settles into stone crevices
                pixels_p[x, y] = min(pixels_p[x, y], 30)

    puddle_img = puddle_img.filter(ImageFilter.GaussianBlur(radius=4.0))
    puddle_path = os.path.join(OUT_DIR, "granite_puddle_mask.png")
    puddle_img.save(puddle_path)
    print("Granite PBR + Wetness Puddle maps saved.")

def generate_cast_iron_textures(size=512):
    print(f"Generating Cast Iron PBR maps ({size}x{size})...")
    random.seed(101)
    albedo = Image.new('RGB', (size, size))
    pixels_a = albedo.load()
    normal = Image.new('RGB', (size, size), (128, 128, 255))
    pixels_n = normal.load()

    base = 28
    for y in range(size):
        for x in range(size):
            grain = random.randint(-6, 6)
            c = max(15, min(50, base + grain))
            pixels_a[x, y] = (c + 3, c + 2, c)
            nx = (random.random() - 0.5) * 0.25
            ny = (random.random() - 0.5) * 0.25
            nz = math.sqrt(max(0.1, 1.0 - nx*nx - ny*ny))
            pixels_n[x, y] = (
                int((nx * 0.5 + 0.5) * 255),
                int((ny * 0.5 + 0.5) * 255),
                int((nz * 0.5 + 0.5) * 255)
            )

    albedo.save(os.path.join(OUT_DIR, "cast_iron_albedo.png"))
    normal.save(os.path.join(OUT_DIR, "cast_iron_normal.png"))
    print("Cast Iron maps saved.")

def generate_water_normal_map(size=512):
    print(f"Generating River Don Water Normal Map ({size}x{size})...")
    height = Image.new('L', (size, size))
    pixels_h = height.load()

    for y in range(size):
        v = y / float(size) * 2.0 * math.pi
        for x in range(size):
            u = x / float(size) * 2.0 * math.pi
            w1 = math.sin(u * 4.0 + v * 2.0)
            w2 = math.cos(u * 2.5 - v * 4.5)
            w3 = math.sin(u * 8.0 + v * 6.0) * 0.4
            val = (w1 + w2 + w3) / 2.4
            pixels_h[x, y] = int((val * 0.5 + 0.5) * 255)

    normal = Image.new('RGB', (size, size))
    pixels_n = normal.load()
    strength = 3.2

    for y in range(size):
        ym1 = (y - 1) % size
        yp1 = (y + 1) % size
        for x in range(size):
            xm1 = (x - 1) % size
            xp1 = (x + 1) % size
            dx = (pixels_h[xp1, y] - pixels_h[xm1, y]) / 255.0
            dy = (pixels_h[x, yp1] - pixels_h[x, ym1]) / 255.0
            nx = -dx * strength
            ny = -dy * strength
            nz = 1.0
            length = math.sqrt(nx*nx + ny*ny + nz*nz)
            pixels_n[x, y] = (
                int((-dx * strength / length * 0.5 + 0.5) * 255),
                int((-dy * strength / length * 0.5 + 0.5) * 255),
                int((nz / length * 0.5 + 0.5) * 255)
            )

    normal.save(os.path.join(OUT_DIR, "water_normal.png"))
    print("Water normal map saved.")

def generate_navigation_sign(size=256):
    print(f"Generating Navigation Clearance Sign ({size}x{size})...")
    # Authentic Volga-Don shipping clearance sign: white rhombus with red border and black center marker
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    pad = 20
    half = size // 2
    rhombus = [(half, pad), (size - pad, half), (half, size - pad), (pad, half)]
    # Red border
    draw.polygon(rhombus, fill=(225, 30, 20, 255))
    # White interior
    inner_pad = pad + 24
    inner_rhombus = [(half, inner_pad), (size - inner_pad, half), (half, size - inner_pad), (inner_pad, half)]
    draw.polygon(inner_rhombus, fill=(245, 245, 245, 255))
    # Center black bar
    draw.rectangle([half - 12, inner_pad + 15, half + 12, size - inner_pad - 15], fill=(20, 20, 20, 255))

    img.save(os.path.join(OUT_DIR, "navigation_sign.png"))
    print("Navigation sign saved.")

def generate_stadium_facade_texture(size=512):
    print(f"Generating Rostov Arena Daytime Architectural Facade Texture ({size}x{size})...")
    # Clean architectural off-white / light silver-gray PTFE/ETFE membrane and cladding
    img = Image.new('RGBA', (size, size), (242, 245, 248, 255))
    draw = ImageDraw.Draw(img)

    # Populous signature diamond-faceted perforated facade panels in sunlight
    diamond_w = 32
    diamond_h = 48
    for row in range(-1, size // diamond_h + 2):
        for col in range(-1, size // diamond_w + 2):
            cx = col * diamond_w + (diamond_w // 2 if row % 2 == 1 else 0)
            cy = row * diamond_h
            diamond = [
                (cx, cy - diamond_h // 2),
                (cx + diamond_w // 2, cy),
                (cx, cy + diamond_h // 2),
                (cx - diamond_w // 2, cy)
            ]
            # Facet shading: subtle alternating sun reflection on triangular panels
            shade = 236 + ((row + col) % 3) * 6
            draw.polygon(diamond, outline=(198, 206, 214, 255), fill=(shade, shade + 2, shade + 5, 255))
            # Architectural rivet / fastener node
            draw.ellipse([cx - 1, cy - 1, cx + 1, cy + 1], fill=(160, 170, 180, 255))

    img.save(os.path.join(OUT_DIR, "stadium_facade.png"))
    print("Daytime stadium facade texture saved.")

def generate_sunset_sky_panorama(width=2048, height=1024):
    print(f"Generating Authentic Rostov Daytime Sky Panorama ({width}x{height})...")
    sky = Image.new('RGB', (width, height))
    draw = ImageDraw.Draw(sky)

    # Southern Russian summer/autumn daytime sky: deep azure to pale horizon
    stops = [
        (0.00, (28, 102, 196)),    # Deep Zenith Azure
        (0.30, (52, 132, 218)),    # Mid Azure
        (0.55, (105, 172, 236)),   # Sky Blue
        (0.75, (168, 208, 248)),   # Pale Cyan Atmosphere
        (0.90, (206, 228, 252)),   # Warm Horizon Haze
        (1.00, (190, 216, 240))    # Waterline Atmospheric Blend
    ]

    for y in range(height):
        t = y / float(height)
        for i in range(len(stops) - 1):
            t0, c0 = stops[i]
            t1, c1 = stops[i+1]
            if t0 <= t <= t1:
                factor = (t - t0) / (t1 - t0)
                factor = factor * factor * (3.0 - 2.0 * factor)
                base_r = int(c0[0] + (c1[0] - c0[0]) * factor)
                base_g = int(c0[1] + (c1[1] - c0[1]) * factor)
                base_b = int(c0[2] + (c1[2] - c0[2]) * factor)
                break
        draw.line([(0, y), (width, y)], fill=(base_r, base_g, base_b))

    # Add soft, sunlit summer cumulus clouds drifting over the Don River
    random.seed(42)
    cloud_layer = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    cloud_draw = ImageDraw.Draw(cloud_layer)

    for puff in range(120):
        cx = random.randint(0, width)
        cy = random.randint(int(height * 0.15), int(height * 0.65))
        rw = random.randint(90, 260)
        rh = random.randint(25, 75)
        # Sunlit white with subtle warm tint
        c_alpha = random.randint(35, 95)
        c_val = random.randint(245, 255)
        cloud_draw.ellipse([cx - rw, cy - rh, cx + rw, cy + rh], fill=(c_val, c_val, c_val - 5, c_alpha))

    cloud_layer = cloud_layer.filter(ImageFilter.GaussianBlur(radius=18.0))
    sky.paste(Image.alpha_composite(Image.new('RGBA', (width, height), (0,0,0,0)), cloud_layer).convert('RGB'), (0, 0), cloud_layer.split()[3])

    sky.save(os.path.join(OUT_DIR, "sunset_sky.png"))
    print("Daytime Sky panorama saved.")

if __name__ == '__main__':
    generate_granite_textures(1024)
    generate_cast_iron_textures(512)
    generate_water_normal_map(512)
    generate_navigation_sign(256)
    generate_stadium_facade_texture(512)
    generate_sunset_sky_panorama(2048, 1024)
    print("All Rostov PBR textures generated successfully in:", OUT_DIR)
