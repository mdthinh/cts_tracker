# Runbook triển khai — ITS INF Project Tracker

Tài liệu này gom lại toàn bộ kiến thức cài đặt/khắc phục sự cố đã đúc kết được trong quá trình
dựng hệ thống trên VM Windows Server test. Có 2 cách chạy:

- **Cách A — chạy nhanh bằng `dotnet run`**: dùng để test, đang dùng trên VM hiện tại. Đơn giản,
  không cần cài IIS, nhưng process chạy trong session console, tắt cửa sổ/đăng xuất là app dừng
  (trừ khi chạy nền bằng `nssm`/Task Scheduler — xem cuối mục A).
- **Cách B — triển khai qua IIS**: khuyến nghị khi hệ thống chuyển sang dùng thật lâu dài. App
  chạy như 1 Windows Service ẩn sau IIS, tự khởi động lại khi VM reboot, tự restart nếu process
  crash.

Cả 2 cách đều cần hoàn thành **Mục 1 (cài đặt nền tảng)** trước.

---

## 1. Cài đặt nền tảng trên VM (làm 1 lần)

### 1.1. .NET 8

- Cách A (dotnet run): cài **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
- Cách B (IIS): cài thêm **ASP.NET Core 8.0 Hosting Bundle** (gói riêng, khác SDK) — mục
  "Hosting Bundle" trong cùng trang tải — bắt buộc để IIS nói chuyện được với Kestrel qua module
  ANCM (AspNetCoreModuleV2). Cài xong phải **restart IIS** (`net stop was /y && net start w3svc`)
  hoặc restart VM để IIS nhận module mới.

Kiểm tra: `dotnet --info` (phải thấy `Microsoft.AspNetCore.App 8.0.x` trong danh sách runtime).

### 1.2. SQL Server Express

Cài SQL Server Express (bản free, giới hạn 10GB/DB — đủ dùng cho quy mô hiện tại), instance mặc
định tên **SQLEXPRESS**. Sau khi cài, có 2 sự cố **đã gặp thật** trên VM test, luôn kiểm tra 2 mục
này trước khi báo lỗi kết nối DB:

1. **Dịch vụ SQL Server Browser đang dừng** — instance có tên (SQLEXPRESS) cần dịch vụ này chạy
   để client tìm được đúng port. Mở `services.msc` → tìm **SQL Server Browser** → Start, đặt
   Startup type = Automatic.
2. **Giao thức Named Pipes/TCP bị tắt** — mở **SQL Server Configuration Manager** → SQL Server
   Network Configuration → Protocols for SQLEXPRESS → bật **TCP/IP** (và/hoặc Named Pipes) →
   Enabled. Sau khi đổi phải **restart dịch vụ SQL Server (SQLEXPRESS)** trong `services.msc` để
   áp dụng.

Xác thực bằng Windows Authentication (Trusted_Connection) — không cần tạo SQL login riêng, app
chạy dưới tài khoản Windows nào thì dùng quyền của tài khoản đó để kết nối DB local.

### 1.3. Lấy code

```powershell
git clone https://github.com/mdthinh/cts_tracker.git C:\Apps\CmcTs
cd C:\Apps\CmcTs
```

Cập nhật sau này chỉ cần `git pull` trong đúng thư mục này.

### 1.4. Cấu hình `appsettings.json`

File `src/CmcTs.Web/appsettings.json` (đã có sẵn trong repo, commit công khai — **không đặt mật
khẩu thật vào đây**):

```json
{
  "ConnectionStrings": {
    "CmcTsDb": "Server=tcp:localhost\\SQLEXPRESS;Database=CmcTsTracker;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Ldap": {
    "Host": "10.2.2.171",
    "Port": 389,
    "BaseDn": "DC=mdt,DC=local",
    "ServiceAccountUsername": "report_service",
    "InitialAdminSamAccountNames": [ "mdthinh" ]
  },
  "Storage": {
    "UploadsRootPath": "App_Data/Uploads"
  }
}
```

Lưu ý connection string: bắt buộc phải có `\SQLEXPRESS` sau tên server — thiếu phần này là lỗi
kết nối DB thực tế đã gặp (SQL Server không lắng nghe ở port mặc định 1433 cho named instance).
`UploadsRootPath` để đường dẫn tương đối như trên là an toàn nhất — code tự quy về thư mục chứa
file chạy (`AppContext.BaseDirectory`), không phụ thuộc working directory lúc khởi động (khác
nhau giữa `dotnet run`, IIS, hay double-click .exe). Chỉ đổi thành đường dẫn tuyệt đối (vd
`D:\AppData\CmcTs\Uploads`) nếu muốn tách dữ liệu upload ra khỏi thư mục cài app.

### 1.5. Mật khẩu bí mật (LDAP service account, tài khoản admin local)

**Không bao giờ** đặt mật khẩu thật vào `appsettings.json` (file này nằm trong git, public repo).
Dùng 1 trong 2 cách:

**Cách 1 — .NET User Secrets** (phù hợp khi chạy `dotnet run`, Cách A):

```powershell
cd C:\Apps\CmcTs\src\CmcTs.Web
dotnet user-secrets set "Ldap:ServiceAccountPassword" "<mật khẩu report_service>"
dotnet user-secrets set "LocalAdmin:Username" "admin"
dotnet user-secrets set "LocalAdmin:Password" "<mật khẩu admin local>"
```

Nếu gặp lỗi "could not find UserSecretsId" — csproj đã có sẵn `<UserSecretsId>` (đã fix), chỉ cần
đảm bảo đang đứng đúng thư mục `src/CmcTs.Web` khi chạy lệnh trên.

**Cách 2 — biến môi trường** (bắt buộc khi chạy qua IIS, Cách B — vì user-secrets chỉ đọc được
khi chạy bằng `dotnet run`/`dotnet exec` dưới cùng user đã set, IIS chạy app dưới danh tính App
Pool khác). Đặt ở **System Properties → Environment Variables** (biến hệ thống, không phải biến
user) rồi **restart IIS**:

| Tên biến | Giá trị |
|---|---|
| `Ldap__ServiceAccountPassword` | mật khẩu report_service |
| `LocalAdmin__Username` | admin |
| `LocalAdmin__Password` | mật khẩu admin local |

(2 dấu gạch dưới `__` thay cho dấu `:` trong tên section — quy ước chuẩn của .NET configuration
khi đọc từ biến môi trường.)

### 1.6. Khởi tạo database (migration)

Chạy 1 lần khi cài mới, và mỗi khi pull về có migration mới (thư mục
`src/CmcTs.Core/Migrations` có file mới):

```powershell
cd C:\Apps\CmcTs
dotnet tool install --global dotnet-ef   # chỉ cần làm 1 lần trên máy chưa có
dotnet ef database update --project src\CmcTs.Core --startup-project src\CmcTs.Core
```

Dùng `CmcTs.Core` làm cả `--project` lẫn `--startup-project` — **không dùng `CmcTs.Web`**, vì gói
`Microsoft.EntityFrameworkCore.Design` chỉ được khai báo (với `PrivateAssets=all`) trong
`CmcTs.Core` nên không chảy sang `CmcTs.Web` qua project reference; dùng `CmcTs.Web` làm startup
project sẽ báo lỗi "doesn't reference Microsoft.EntityFrameworkCore.Design". `CmcTs.Core` tự đủ để
chạy migration vì đã có sẵn `CmcTsDbContextFactory.cs` (design-time factory riêng, connection
string hardcode `localhost\SQLEXPRESS`, không đọc `appsettings.json` của `CmcTs.Web`).

Lệnh này tạo database `CmcTsTracker` nếu chưa có, và áp toàn bộ migration. Không cần tạo DB thủ
công trước bằng SSMS.

Lần chạy app đầu tiên sau khi migrate xong sẽ tự seed: tài khoản admin local (từ mục 1.5), và
danh sách công nghệ mặc định. Tài khoản AD nằm trong `InitialAdminSamAccountNames` sẽ tự được
nâng quyền Admin ngay lần đầu họ đăng nhập.

---

## 2. Cách A — chạy nhanh bằng `dotnet run` (đang dùng để test)

```powershell
cd C:\Apps\CmcTs\src\CmcTs.Web
dotnet run --urls "http://0.0.0.0:5000"
```

Truy cập từ máy khác trong mạng bằng `http://<IP-của-VM>:5000`. Cửa sổ console phải để mở — đóng
lại (hoặc đăng xuất khỏi VM) sẽ dừng app.

**Cập nhật code:**

```powershell
cd C:\Apps\CmcTs
git pull
dotnet ef database update --project src\CmcTs.Core --startup-project src\CmcTs.Core   # nếu có migration mới
cd src\CmcTs.Web
dotnet run --urls "http://0.0.0.0:5000"
```

**Chạy nền, tự sống qua reboot** (khi chưa muốn chuyển hẳn sang IIS): tạo 1 Scheduled Task chạy
`dotnet run` khi khởi động máy, hoặc dùng công cụ `nssm` (Non-Sucking Service Manager) để bọc
lệnh `dotnet run` thành 1 Windows Service thật — cả 2 cách này vẫn là giải pháp tạm, khi hệ thống
dùng thật nên chuyển sang Cách B.

---

## 3. Cách B — triển khai qua IIS (khuyến nghị cho môi trường dùng thật)

### 3.1. Publish

```powershell
cd C:\Apps\CmcTs
dotnet publish src/CmcTs.Web -c Release -o C:\Apps\CmcTs-publish
```

`appsettings.json` trong thư mục publish là bản copy — nếu sửa cấu hình sau này, sửa file trong
`C:\Apps\CmcTs-publish` (hoặc publish lại) chứ không phải file trong `C:\Apps\CmcTs`.

### 3.2. Tạo site trên IIS

1. Bật role **Web Server (IIS)** trong Server Manager nếu chưa có.
2. Mở **IIS Manager** → Application Pools → **Add Application Pool**: .NET CLR Version =
   **No Managed Code** (bắt buộc — ASP.NET Core tự host runtime riêng, không dùng CLR của IIS).
3. Sites → **Add Website**: Physical path = `C:\Apps\CmcTs-publish`, chọn Application Pool vừa
   tạo, đặt Port (vd 80 hoặc 8080 tùy quy hoạch mạng nội bộ).
4. Đảm bảo tài khoản chạy Application Pool (mặc định `ApplicationPoolIdentity`) có quyền:
   - Đọc/ghi thư mục `C:\Apps\CmcTs-publish\App_Data\Uploads` (tạo trước thư mục này, cấp quyền
     Modify cho `IIS AppPool\<TênAppPool>`).
   - Kết nối SQL Server Express bằng Windows Authentication — nếu dùng `ApplicationPoolIdentity`
     mặc định, phải cấp quyền cho account `IIS AppPool\<TênAppPool>` trong SQL Server (Security →
     Logins → New Login → nhập đúng tên `IIS APPPOOL\<TênAppPool>` → gán quyền db_owner trên
     database `CmcTsTracker`).

### 3.3. Biến môi trường mật khẩu

Set theo mục 1.5 Cách 2 (biến hệ thống) rồi `iisreset` để IIS đọc lại.

### 3.4. Cập nhật phiên bản mới

```powershell
cd C:\Apps\CmcTs
git pull
dotnet ef database update --project src\CmcTs.Core --startup-project src\CmcTs.Core   # nếu có migration mới
dotnet publish src/CmcTs.Web -c Release -o C:\Apps\CmcTs-publish
```

IIS tự phát hiện file thay đổi và recycle app pool (cơ chế `hostingModel: InProcess` mặc định) —
không cần thao tác thêm, nhưng có thể chủ động `iisreset` nếu muốn chắc chắn.

---

## 4. Tra cứu nhanh — lỗi đã gặp thật trên VM test

| Triệu chứng | Nguyên nhân | Cách sửa |
|---|---|---|
| `dotnet ef database update` báo "No project was found" | Đứng ở thư mục gốc repo, không có file `.csproj` ở đó | Thêm `--project src\CmcTs.Core --startup-project src\CmcTs.Core`, hoặc `cd` vào đúng `src\CmcTs.Core` rồi chạy không cần cờ |
| `dotnet ef database update` báo "startup project doesn't reference Microsoft.EntityFrameworkCore.Design" | Dùng `--startup-project src\CmcTs.Web` — gói Design chỉ khai báo (PrivateAssets=all) trong `CmcTs.Core`, không chảy sang `CmcTs.Web` | Dùng `CmcTs.Core` làm cả `--project` lẫn `--startup-project`, không dùng `CmcTs.Web` |
| `dotnet ef database update` báo lỗi không kết nối được SQL Server | Thiếu `\SQLEXPRESS` trong connection string (chỉ có `Server=localhost`) | Sửa thành `Server=tcp:localhost\SQLEXPRESS;...` ở cả `appsettings.json` và `CmcTsDbContextFactory.cs` (file này dùng riêng cho `dotnet ef`) |
| Kết nối SQL Server chập chờn / không tìm thấy instance | Dịch vụ **SQL Server Browser** đang Stopped | `services.msc` → SQL Server Browser → Start + Automatic |
| Kết nối SQL Server bị từ chối dù đã đúng tên instance | Giao thức **Named Pipes/TCP** bị Disabled trong SQL Server Configuration Manager | Bật lại, restart dịch vụ SQL Server (SQLEXPRESS) |
| `dotnet user-secrets set` báo "could not find UserSecretsId" | Thiếu thẻ `<UserSecretsId>` trong `.csproj`, hoặc chạy lệnh sai thư mục | Đảm bảo đứng trong `src/CmcTs.Web`; project đã có sẵn `<UserSecretsId>` từ trước |
| Upload file (Dự toán/tài liệu final) báo lỗi ghi file | `Storage:UploadsRootPath` trỏ tới ổ đĩa không tồn tại trên VM (vd `D:\...` khi VM chỉ có ổ `C:`) | Đổi về đường dẫn tương đối `App_Data/Uploads`, hoặc đường dẫn tuyệt đối đúng ổ đĩa thật có trên VM |
| Đăng nhập AD báo sai mật khẩu dù đúng | Chưa set `Ldap:ServiceAccountPassword` (biến môi trường hoặc user-secrets rỗng) | Set lại theo mục 1.5 |
| (IIS) Site báo lỗi 502.5 - Process Failure | Chưa cài **ASP.NET Core Hosting Bundle** (khác SDK thường), hoặc cài sau khi IIS đã chạy nên chưa nhận module | Cài Hosting Bundle, sau đó `iisreset` |

---

## 5. Bảo trì định kỳ (gợi ý, chưa có lịch tự động)

- **Backup database**: dùng SQL Server Management Studio (SSMS) → chuột phải database
  `CmcTsTracker` → Tasks → Back Up..., hoặc lập job tự động qua SQL Server Agent (bản Express
  không có Agent — cần lên lịch bằng Windows Task Scheduler chạy script `sqlcmd`/`sqlpackage` nếu
  muốn tự động hoá).
- **Backup thư mục upload**: copy toàn bộ `App_Data/Uploads` (chứa file Dự toán Excel gốc + tài
  liệu final đã upload) — dữ liệu này KHÔNG nằm trong database, backup DB không kèm theo các file
  này.
- **Xoay vòng mật khẩu service account LDAP**: mật khẩu `report_service` từng được dán trực tiếp
  vào lịch sử chat lúc trao đổi ban đầu — nên đổi mật khẩu tài khoản này trên AD định kỳ, chỉ cập
  nhật lại ở biến môi trường/user-secrets, không cần sửa code.
