namespace CmcTs.Core.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    // Đường dẫn tuyệt đối trên VM, vd "D:\\AppData\\CmcTs\\Uploads". Mặc định dùng thư mục tương
    // đối "App_Data/Uploads" (dưới content root) để chạy được ngay cả khi chưa cấu hình gì.
    public string UploadsRootPath { get; set; } = "App_Data/Uploads";
}
