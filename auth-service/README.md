# Auth Service

.NET 10 Web API: JWT (access + refresh), forgot password, RBAC (`User`, `Admin`). SQLite ของ service นี้เท่านั้น

## สิ่งที่ต้องมี

- .NET SDK 10.0.x (ดู `../global.json`)
- Docker + Docker Compose (สำหรับรันผ่าน Compose)
- OpenSSL (สร้าง JWT key)

## 1. สร้าง JWT key (ครั้งแรก)

รันที่โฟลเดอร์ `auth-service`

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out keys/jwt-private.pem
openssl rsa -in keys/jwt-private.pem -pubout -out keys/jwt-public.pem
```

ไฟล์ `.pem` ถูก git-ignore รายละเอียดดู `keys/README.md`

## 2. วิธีรัน (Development)

### แบบ A: `dotnet run` (เร็วสุดสำหรับเขียนโค้ด)

```bash
cd src/Auth.Api
dotnet run --launch-profile http
```

- URL: `http://localhost:5183`, health: `GET /api/auth/health`
- OpenAPI: `/openapi/v1.json` (เปิดเฉพาะ Development)
- ใช้ค่าจาก `appsettings.Development.json` (SQLite ไฟล์ `auth.db` ในโฟลเดอร์ที่รัน, key จาก `../../keys/jwt-private.pem`)
- Migration และ seed admin ทำอัตโนมัติตอน start

### แบบ B: Docker Compose (ผ่าน Kong)

รันที่โฟลเดอร์ root `distributed-product-management`

```bash
cp .env.example .env        # แก้ค่าตามต้องการ
docker compose up -d --build auth-service kong
```

- เรียกผ่าน Kong: `http://localhost:8000/api/auth/...`
- Auth Service ไม่ได้เปิด port ออกนอก Compose
- Log: `docker compose logs -f auth-service`
- DB อยู่ที่ `auth-service/data/auth.db` (bind mount) ล้าง DB: `docker compose down` แล้วลบไฟล์ `auth-service/data/auth.db*`

### รัน test

```bash
dotnet test AuthService.slnx
```

## 3. เพิ่ม Seeder account สำหรับทดสอบ

Seeder (`AdminSeeder`) สร้าง **Admin 1 บัญชี** ตอน start ถ้ายังไม่มี user ที่ email นี้ (รันซ้ำได้ ไม่สร้างซ้ำ) ถ้าเว้นว่าง จะข้ามพร้อม log warning

| Key | Env var | ค่าตัวอย่าง (dev) |
|---|---|---|
| `Seed:AdminEmail` | `Seed__AdminEmail` | `admin@example.com` |
| `Seed:AdminPassword` | `Seed__AdminPassword` | `Admin#12345` |
| `Seed:AdminName` | `Seed__AdminName` | `Administrator` |

- **dotnet run:** แก้ที่ `src/Auth.Api/appsettings.Development.json` หัวข้อ `Seed`
- **Compose:** แก้ `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` ใน `.env` แล้ว `docker compose up -d auth-service`
- เปลี่ยน email/password แล้วต้องไม่ซ้ำกับของเดิมใน DB (seeder ไม่อัปเดตบัญชีเดิม) ถ้าจะเริ่มใหม่ให้ลบ DB (`auth.db`)

### บัญชี User ทดสอบ

Seeder สร้างได้เฉพาะ Admin ส่วน User ธรรมดาให้สมัครผ่าน API (role = `User` เสมอ)

```bash
curl -X POST http://localhost:8000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"User#12345","name":"Test User"}'
```

(รันแบบ `dotnet run` ใช้ `http://localhost:5183` แทน) รหัสผ่านอย่างน้อย 8 ตัวอักษร

ทดสอบ login:

```bash
curl -X POST http://localhost:8000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin#12345"}'
```

> Login ผ่าน Kong จำกัด 5 ครั้ง/15 นาที/IP (ประมาณด้วย minute + hour limit)

## 4. Setup Environment

ตั้งค่าผ่าน environment variable รูปแบบ `Section__Key` (ตัวอย่างทุกค่าดู `src/Auth.Api/appsettings.example.json`)

| Env var | Development | PROD |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Production` (ค่า default ของ image) |
| `Jwt__PrivateKeyPath` | `../../keys/jwt-private.pem` | path ของ private key (Compose secret `/run/secrets/jwt_private_key`) |
| `Csrf__Secret` | ค่า dev ใน `appsettings.Development.json` | สุ่มใหม่ ยาว ≥ 32 ตัวอักษร (น้อยกว่านี้ service ไม่ start) |
| `Spa__Origin` | `http://localhost:5173` | origin จริงของ SPA (HTTPS) |
| `ResetLink__BaseUrl` | `http://localhost:5173/reset-password` | URL หน้า reset password จริง |
| `AuthCookie__Secure` / `AuthCookie__SameSite` | `true` / `Strict` (ใช้ `false` / `Lax` เฉพาะ SPA เป็น HTTP คนละ host) | `true` / `Strict` |
| `Seed__AdminEmail` / `Seed__AdminPassword` | ค่าทดสอบ | ตั้งครั้งแรกเพื่อสร้าง Admin จริง |
| `Smtp__Host/Port/Username/Password/Crypto/FromAddress` | Mailtrap sandbox (`sandbox.smtp.mailtrap.io:2525`, `tls`) | SMTP จริง เช่น Gmail `smtp.gmail.com:587` + app password |
| `ConnectionStrings__Auth` | `Data Source=auth.db` | `Data Source=/data/auth.db` (ตั้งใน Dockerfile แล้ว) |

### Development

1. สร้าง JWT key (ข้อ 1)
2. ใช้ `appsettings.Development.json` (dotnet run) หรือ `.env` (Compose)
3. SMTP ใช้ Mailtrap sandbox เพื่อดูอีเมล forgot-password โดยไม่ส่งออกจริง

### PROD

1. สร้าง JWT key คู่ใหม่ **เฉพาะ PROD** อย่าใช้ key จาก dev และห้าม commit
2. สร้าง `.env` บน server ด้วยค่าจริง (secret สุ่มใหม่, `SEED_ADMIN_PASSWORD` ที่คาดเดายาก) ห้ามใช้ค่าตัวอย่างจาก `.env.example`
3. ใช้ HTTPS ที่ด้านหน้า Kong, ตั้ง `SPA_ORIGIN` และ `RESET_LINK_BASE_URL` เป็นโดเมนจริง และเก็บ `AUTH_COOKIE_SECURE=true`
4. ตั้ง SMTP จริง (ถ้าเว้นว่างจะไม่ส่งอีเมล forgot-password)
5. Build และรัน

   ```bash
   docker compose up -d --build
   ```

6. หลัง Admin ถูกสร้างและ login ได้แล้ว ให้เอา `SEED_ADMIN_*` ออกจาก `.env` หรือเปลี่ยนรหัสผ่านทันที
7. สำรองข้อมูลโฟลเดอร์ `auth-service/data` (`/data/auth.db`) และห้าม service อื่นเปิด DB นี้
8. ตรวจสอบ: `GET /api/auth/health` ผ่าน Kong ต้องตอบ 200

> หมายเหตุ: `appsettings.Development.json` มี credential Mailtrap ของ dev อยู่ ไม่ถูกโหลดเมื่อ `ASPNETCORE_ENVIRONMENT=Production` แต่ไม่ควรใช้ซ้ำใน PROD
