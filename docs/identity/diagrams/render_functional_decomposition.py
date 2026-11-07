from pathlib import Path
from xml.sax.saxutils import escape
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
INK = "#222222"

def render(name, root, groups, note):
    cw, margin, gap = 360, 35, 28
    width = margin * 2 + len(groups) * cw + (len(groups)-1) * gap
    rows = max(len(children) for _, children in groups)
    height = 355 + rows * 112 + 105
    im = Image.new("RGB", (width,height), "white")
    draw = ImageDraw.Draw(im)
    svg = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}"><rect width="100%" height="100%" fill="white"/>']
    def line(points):
        draw.line(points, fill=INK, width=2)
        svg.append('<polyline points="'+' '.join(f'{x},{y}' for x,y in points)+'" fill="none" stroke="'+INK+'" stroke-width="2"/>')
    def font(size, bold=False):
        return ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf", size)
    def wrapped(label, max_width, size, bold):
        lines=[]
        for paragraph in label.split("\n"):
            current=""
            for word in paragraph.split():
                candidate = (current+" "+word).strip()
                if current and draw.textlength(candidate, font=font(size,bold)) > max_width:
                    lines.append(current); current=word
                else: current=candidate
            if current: lines.append(current)
        return lines
    def label(cx,cy,lines,size=23,bold=False):
        lh=size+7
        for i,t in enumerate(lines):
            y=cy+(i-(len(lines)-1)/2)*lh
            draw.text((cx,y), t, anchor="mm", fill=INK, font=font(size,bold))
            svg.append(f'<text x="{cx}" y="{y}" text-anchor="middle" dominant-baseline="central" font-family="Arial, sans-serif" font-size="{size}" font-weight="{700 if bold else 400}" fill="{INK}">{escape(t)}</text>')
    def box(x,y,w,h,text,bold=False):
        size = 20 if bold and w == cw else 23
        lines=wrapped(text,w-26,size,bold)
        while len(lines)*(size+7) > h-12 and size > 16:
            size -= 1
            lines=wrapped(text,w-26,size,bold)
        assert len(lines)*(size+7) <= h-12, (text,lines)
        draw.rectangle((x,y,x+w,y+h), outline=INK, fill="white", width=2)
        svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="white" stroke="{INK}" stroke-width="2"/>')
        label(x+w/2,y+h/2,lines,size,bold)
    root_w=460
    box((width-root_w)/2,25,root_w,115,root,True)
    centers=[margin+i*(cw+gap)+cw/2 for i in range(len(groups))]
    line([(width/2,140),(width/2,185)])
    line([(centers[0],185),(centers[-1],185)])
    for i,(header,children) in enumerate(groups):
        x=margin+i*(cw+gap); center=x+cw/2
        line([(center,185),(center,225)])
        box(x,225,cw,95,header,True)
        trunk=x+19; child_x=x+55; child_w=cw-55
        last_y=365+(len(children)-1)*112+43
        if children:line([(trunk,320),(trunk,last_y)])
        for j,child in enumerate(children):
            y=365+j*112
            line([(trunk,y+43),(child_x,y+43)])
            box(child_x,y,child_w,86,child)
    label(width/2,height-43,wrapped(note,width-100,21,False),21)
    svg.append("</svg>")
    im.save(HERE/(name+".png"))
    (HERE/(name+".svg")).write_text("\n".join(svg),encoding="utf-8")

if __name__ == "__main__":
    render("phan-ra-chuc-nang-tong-the", "HỆ THỐNG HOMEMATCH", [
        ("1. Tài khoản và xác thực", ["Đăng ký và đăng nhập", "Duy trì phiên đăng nhập", "Quên mật khẩu"]),
        ("2. Tìm kiếm và quản lý tin người thuê", ["Tìm kiếm phòng", "Xem tin và so sánh", "Bản đồ và tìm gần tôi", "Yêu thích và đặt nhắc lịch"]),
        ("3. Lịch xem phòng và liên hệ", ["Quản lý lịch xem", "Nhắn tin"]),
        ("4. Tổng quan người thuê", ["Dashboard tìm phòng", "Lịch sử xem phòng", "Xem thông báo"]),
        ("5. Hợp đồng và thanh toán người thuê", ["Xem hợp đồng", "Xem hóa đơn và trạng thái thu", "Xem chi phí"]),
        ("6. Quản lý tin đăng và căn phòng", ["Tạo xem sửa ẩn tin", "Cập nhật trạng thái phòng"]),
        ("7. Khách thuê và hợp đồng chủ nhà", ["Xem khách quan tâm", "Xem người thuê", "Soạn quản lý hợp đồng", "Hợp đồng sắp hết hạn"]),
        ("8. Dashboard và báo cáo chủ nhà", ["Dashboard và bộ lọc", "Doanh thu và khoản thu", "Công nợ và tiền cọc", "Tin khách và lịch xem"]),
        ("9. Cài đặt tài khoản", ["Quản lý hồ sơ", "Đổi mật khẩu", "Cài đặt thông báo"]),
    ], "Hình 1. Sơ đồ phân rã chức năng hệ thống HomeMatch — mô hình định hướng; không khẳng định tất cả chức năng đã triển khai.")
    render("phan-ra-chuc-nang-xac-thuc", "1. TÀI KHOẢN VÀ XÁC THỰC", [
        ("1.1 Đăng ký và đăng nhập", [
            "1.1.1 Nhập thông tin, chọn vai trò Tenant/Landlord",
            "1.1.2 Tạo tài khoản chưa xác minh email",
            "1.1.3 Gửi/gửi lại OTP đăng ký",
            "1.1.4 Xác thực email bằng OTP",
            "1.1.5 Lấy CSRF và gửi yêu cầu đăng nhập",
            "1.1.6 Kiểm tra mật khẩu, trạng thái, email",
            "1.1.7 Tạo phiên, access token, refresh cookie",
        ]),
        ("1.2 Duy trì phiên đăng nhập", [
            "1.2.1 Làm mới access token và xoay refresh token",
            "1.2.2 Xem thông tin tài khoản hiện tại",
            "1.2.3 Đăng xuất phiên hiện tại",
            "1.2.4 Đăng xuất tất cả phiên",
            "1.2.5 Gửi/xác minh email qua Bearer (luồng cũ)",
        ]),
        ("1.3 Quên mật khẩu", [
            "1.3.1 Yêu cầu gửi OTP khôi phục",
            "1.3.2 Xác minh OTP, nhận reset token",
            "1.3.3 Nhập và đặt mật khẩu mới",
            "1.3.4 Tiêu thụ reset token một lần",
            "1.3.5 Thu hồi phiên và refresh token cũ",
        ]),
    ], "Sơ đồ chi tiết nhánh 1 theo chức năng backend hiện có; đường nối biểu thị phân rã, không biểu thị trình tự xử lý.")
