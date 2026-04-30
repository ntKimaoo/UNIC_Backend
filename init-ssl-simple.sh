#!/bin/bash
# ============================================================
# init-ssl-simple.sh — Script đơn giản hơn để lấy SSL
# Dùng khi init-ssl.sh gặp lỗi
# ============================================================

set -e

DOMAIN="chuyencongnhan.io.vn"
EMAIL="kientrung2004x@gmail.com"

echo "=== Bước 1: Dừng tất cả ==="
docker compose down 2>/dev/null || true

echo "=== Bước 2: Tạo thư mục certbot ==="
docker volume create certbot-www 2>/dev/null || true
docker volume create certbot-certs 2>/dev/null || true

echo "=== Bước 3: Lấy cert bằng standalone mode ==="
# Cách này không cần nginx, certbot tự tạo web server tạm trên port 80
docker run --rm \
  -p 80:80 \
  -v certbot-certs:/etc/letsencrypt \
  -v certbot-www:/var/www/certbot \
  certbot/certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email "${EMAIL}" \
    --agree-tos \
    --no-eff-email \
    -d "${DOMAIN}"

echo "=== Bước 4: Khởi động full stack ==="
docker compose up -d --build

echo ""
echo "✅ Xong! Truy cập: https://${DOMAIN}"
echo "   Swagger: https://${DOMAIN}/swagger"
