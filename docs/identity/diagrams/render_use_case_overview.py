"""Render one combined authentication use-case diagram (PNG and SVG)."""

from pathlib import Path
from xml.sax.saxutils import escape

from PIL import Image, ImageDraw, ImageFont


HERE = Path(__file__).resolve().parent
WIDTH, HEIGHT = 1700, 1640
INK, EDGE, PANEL = "#183b63", "#64748b", "#f8fafc"
FONT = "C:/Windows/Fonts/arial.ttf"
BOLD = "C:/Windows/Fonts/arialbd.ttf"

GROUPS = [
    (
        "1.1 Đăng ký và đăng nhập",
        [("UC-A01", "Tạo tài khoản"), ("UC-A02", "Xác thực OTP đăng ký"),
         ("UC-A03", "Gửi lại OTP đăng ký"), ("UC-A04", "Đăng nhập")],
        [245, 345, 445, 545], "Khách chưa đăng nhập", 390, {0, 2}, 390,
    ),
    (
        "1.2 Duy trì phiên và đăng xuất",
        [("UC-A08", "Đăng xuất phiên hiện tại"), ("UC-A09", "Đăng xuất mọi phiên"),
         ("UC-A10", "Làm mới access token"), ("UC-A11", "Xem thông tin hiện tại")],
        [735, 835, 935, 1035], "Người dùng có phiên", 885, set(), 0,
    ),
    (
        "1.3 Quên mật khẩu",
        [("UC-A05", "Yêu cầu OTP khôi phục"), ("UC-A06", "Xác minh OTP khôi phục"),
         ("UC-A07", "Đặt mật khẩu mới")],
        [1230, 1340, 1450], "Khách chưa đăng nhập", 1340, {0}, 1230,
    ),
]


def render():
    image = Image.new("RGB", (WIDTH, HEIGHT), "white")
    draw = ImageDraw.Draw(image)
    svg = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" '
           f'viewBox="0 0 {WIDTH} {HEIGHT}"><rect width="100%" height="100%" fill="white"/>']

    def text(x, y, label, size=23, bold=False, color=INK):
        draw.text((x, y), label, anchor="mm", fill=color,
                  font=ImageFont.truetype(BOLD if bold else FONT, size))
        svg.append(f'<text x="{x}" y="{y}" text-anchor="middle" dominant-baseline="central" '
                   f'font-family="Arial, sans-serif" font-size="{size}" '
                   f'font-weight="{700 if bold else 400}" fill="{color}">{escape(label)}</text>')

    def line(x1, y1, x2, y2, color=EDGE, stroke=2):
        draw.line((x1, y1, x2, y2), fill=color, width=stroke)
        svg.append(f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" '
                   f'stroke="{color}" stroke-width="{stroke}"/>')

    def actor(x, y, label):
        draw.ellipse((x-20, y-72, x+20, y-32), outline=INK, width=3)
        svg.append(f'<circle cx="{x}" cy="{y-52}" r="20" fill="none" stroke="{INK}" stroke-width="3"/>')
        line(x, y-32, x, y+35, INK, 3)
        line(x-38, y-5, x+38, y-5, INK, 3)
        line(x, y+35, x-35, y+77, INK, 3)
        line(x, y+35, x+35, y+77, INK, 3)
        for offset, part in enumerate(label.split("\n")):
            text(x, y+108+offset*25, part, 20)

    text(WIDTH//2, 45, "HOMEMATCH — BIỂU ĐỒ USE CASE XÁC THỰC", 30, True)
    draw.rectangle((335, 100, 1325, 1550), fill=PANEL, outline=INK, width=3)
    svg.append(f'<rect x="335" y="100" width="990" height="1450" '
               f'fill="{PANEL}" stroke="{INK}" stroke-width="3"/>')
    text(830, 130, "1. Tài khoản và xác thực", 25, True)
    for y in (625, 1115):
        line(335, y, 1325, y, INK)

    for header, cases, ys, label, actor_y, email_cases, mail_y in GROUPS:
        text(830, ys[0]-68, header, 24, True)
        actor(155, actor_y, label)
        if email_cases:
            actor(1515, mail_y, "Dịch vụ email\n(hệ thống ngoài)")
        for index, ((code, name), y) in enumerate(zip(cases, ys)):
            line(230, actor_y-5, 530, y)
            if index in email_cases:
                line(1130, y, 1435, mail_y-5)
            draw.ellipse((530, y-38, 1130, y+38), fill="white", outline=INK, width=3)
            svg.append(f'<ellipse cx="830" cy="{y}" rx="300" ry="38" '
                       f'fill="white" stroke="{INK}" stroke-width="3"/>')
            text(830, y-10, code, 18, True)
            text(830, y+16, name, 21)

    text(WIDTH//2, 1600,
         "Actor lặp lại để dễ đọc; đường nối thể hiện tham gia, không phải thứ tự xử lý.",
         20, False, EDGE)
    svg.append("</svg>")
    image.save(HERE / "use-case-xac-thuc-tong-hop.png")
    (HERE / "use-case-xac-thuc-tong-hop.svg").write_text("\n".join(svg), encoding="utf-8")


if __name__ == "__main__":
    render()
