namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public enum CollectionRequestStatus
{
    Pending,     // Mới, chờ duyệt
    Accepted,    // Đã tiếp nhận hồ sơ
    Assigned,    // Đã phân công nhân viên (Đang vận chuyển)
    Collected,   // Đã hoàn thành thu gom
    Cancelled    // Đã hủy đơn
}
