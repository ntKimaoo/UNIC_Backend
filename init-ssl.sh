#!/bin/bash
# ============================================================
# init-ssl.sh — Lấy SSL certificate Let's Encrypt lần đầu
# Domain: chuyencongnhan.io.vn
# ============================================================
# Chạy script này 1 lần duy nhất trên server VPS.
# Sau đó docker-compose sẽ tự renew mỗi 12 giờ.
# ============================================================

set -e

DOMAIN="chuyencongnhan.io.vn"
EMAIL="kientrung2004x@gmail.com"   # Email nhận thông báo hết hạn SSL

echo "============================================"
echo "  SSL Setup cho ${DOMAIN}"
echo "============================================"

# ── Bước 1: Dừng tất cả containers nếu đang chạy ──
echo ""
echo "[1/5] Dừng containers hiện tại..."
docker compose down 2>/dev/null || true

# ── Bước 2: Khởi động nginx với config tạm (HTTP-only) ──
echo ""
echo "[2/5] Khởi động nginx tạm (HTTP-only) để Certbot verify domain..."

# Dùng config tạm không có SSL
docker compose run -d --name temp_nginx \
  -p 80:80 \
  -v "$(pwd)/nginx-init.conf:/etc/nginx/nginx.conf:ro" \
  -v certbot-www:/var/www/certbot \
  nginx nginx -g "daemon off;"

# Đợi nginx sẵn sàng
sleep 3

# ── Bước 3: Chạy Certbot để lấy certificate ──
echo ""
echo "[3/5] Chạy Certbot để lấy SSL certificate..."

docker compose run --rm certbot certonly \
  --webroot \
  --webroot-path=/var/www/certbot \
  --email "${EMAIL}" \
  --agree-tos \
  --no-eff-email \
  -d "${DOMAIN}"

# ── Bước 4: Dừng nginx tạm ──
echo ""
echo "[4/5] Dừng nginx tạm..."
docker stop temp_nginx 2>/dev/null || true
docker rm temp_nginx 2>/dev/null || true

# ── Bước 5: Khởi động toàn bộ stack với SSL ──
echo ""
echo "[5/5] Khởi động full stack với HTTPS..."
docker compose up -d --build

echo ""
echo "============================================"
echo "  ✅ SSL Setup HOÀN TẤT!"
echo ""
echo "  🔗 https://${DOMAIN}"
echo "  🔗 https://${DOMAIN}/swagger"
echo ""
echo "  📋 Kiểm tra SSL:"
echo "     curl -I https://${DOMAIN}"
echo ""
echo "  📋 Xem certificate:"
echo "     docker compose exec nginx ls -la /etc/letsencrypt/live/${DOMAIN}/"
echo ""
echo "  🔄 Auto-renew: Certbot tự renew mỗi 12 giờ"
echo "============================================"
