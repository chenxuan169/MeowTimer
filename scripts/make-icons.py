"""生成喵时 PWA 图标（渐变圆角底 + 白猫），纯 Python 手写 PNG，不依赖 PIL。"""
import math
import struct
import zlib

SS = 3  # 超采样倍数


def lerp(a, b, t):
    return a + (b - a) * t


def mix(c1, c2, t):
    return tuple(int(round(lerp(c1[i], c2[i], t))) for i in range(3))


def render(size):
    """返回 size x size 的 RGBA bytes。"""
    S = size * SS
    from_c = (0xF9, 0x55, 0x8B)
    to_c = (0xFF, 0x9E, 0xC0)

    # 画布（浮点累积，最后降采样）
    buf = [[(0, 0, 0, 0)] * S for _ in range(S)]

    radius = S * 0.22  # 圆角
    cat_white = (0xFF, 0xFF, 0xFF)

    # 猫脸几何（相对 0..1）
    head_cx, head_cy, head_r = 0.5, 0.56, 0.30
    # 耳朵底边压到头部中线以下，避免和脸之间出现缝
    ear_l = [(0.195, 0.56), (0.255, 0.115), (0.52, 0.40)]
    ear_r = [(0.805, 0.56), (0.745, 0.115), (0.48, 0.40)]

    def in_tri(p, a, b, c):
        def sign(p1, p2, p3):
            return (p1[0] - p3[0]) * (p2[1] - p3[1]) - (p2[0] - p3[0]) * (p1[1] - p3[1])
        d1 = sign(p, a, b)
        d2 = sign(p, b, c)
        d3 = sign(p, c, a)
        neg = (d1 < 0) or (d2 < 0) or (d3 < 0)
        pos = (d1 > 0) or (d2 > 0) or (d3 > 0)
        return not (neg and pos)

    def in_ellipse(x, y, cx, cy, rx, ry):
        return ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0

    for py in range(S):
        y = (py + 0.5) / S
        for px in range(S):
            x = (px + 0.5) / S

            # 圆角矩形遮罩
            inx = min(max(x * S, radius), S - radius)
            iny = min(max(y * S, radius), S - radius)
            dx = x * S - inx
            dy = y * S - iny
            # 判断是否在圆角矩形内（考虑四个角）
            cxr = radius if (x * S < radius or x * S > S - radius) else None
            if cxr is not None:
                if y * S < radius or y * S > S - radius:
                    if dx * dx + dy * dy > radius * radius:
                        continue

            # 背景渐变（对角）
            t = min(1.0, max(0.0, (x + y) / 2))
            bg = mix(from_c, to_c, t)

            col = bg

            # 猫：耳朵
            if in_tri((x, y), *ear_l) or in_tri((x, y), *ear_r):
                col = cat_white
            # 头
            elif in_ellipse(x, y, head_cx, head_cy, head_r, head_r * 0.94):
                col = cat_white
                # 眼睛
                if in_ellipse(x, y, 0.415, 0.525, 0.036, 0.055) or \
                   in_ellipse(x, y, 0.585, 0.525, 0.036, 0.055):
                    col = (0x5C, 0x4A, 0x56)
                # 腮红
                elif in_ellipse(x, y, 0.335, 0.63, 0.055, 0.032) or \
                     in_ellipse(x, y, 0.665, 0.63, 0.055, 0.032):
                    col = mix(cat_white, (0xFF, 0x9E, 0xC0), 0.62)
                # 鼻子
                elif in_tri((x, y), (0.468, 0.60), (0.532, 0.60), (0.5, 0.645)):
                    col = (0xFF, 0x9E, 0xC0)

            buf[py][px] = (col[0], col[1], col[2], 255)

    # 降采样
    out = bytearray()
    for py in range(size):
        out.append(0)  # filter
        for px in range(size):
            r = g = b = a = 0
            for sy in range(SS):
                for sx in range(SS):
                    p = buf[py * SS + sy][px * SS + sx]
                    r += p[0]; g += p[1]; b += p[2]; a += p[3]
            n = SS * SS
            out += bytes((r // n, g // n, b // n, a // n))

    return bytes(out)


def write_png(path, size, raw):
    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    ihdr = struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + \
          chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


if __name__ == "__main__":
    base = "E:/WorkBuddy/2026-11-10-10-44-32/web"
    for size in (180, 192, 512):
        write_png(f"{base}/icon-{size}.png", size, render(size))
        print(f"生成 icon-{size}.png")
