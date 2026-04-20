using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WasteCollection_RecyclingPlatform.Repositories.Data;

#nullable disable

namespace WasteCollectionRecyclingPlatform.API.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260419103000_NormalizeLegacyCollectorTransitStatus")]
    public partial class NormalizeLegacyCollectorTransitStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE waste_reports SET Status = 'Accepted' WHERE Status = 'OnTheWay';");
            migrationBuilder.Sql("UPDATE waste_report_status_histories SET Status = 'Accepted', Note = 'Đã bỏ trạng thái trung gian cũ; dữ liệu cũ được quy đổi về Accepted.' WHERE Status = 'OnTheWay';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
