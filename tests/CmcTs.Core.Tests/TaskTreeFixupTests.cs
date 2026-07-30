using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CmcTs.Core.Tests;

// Bug thật đã gặp khi test: trang xem cây công việc (ProjectTasks.razor) tự nối
// ParentTask/Children bằng tay sau khi load — nhưng EF Core đã tự "fixup" 2 chiều navigation
// này ngay khi load hết Task của 1 dự án vào cùng 1 DbContext, nên code tự nối khiến mỗi node
// bị thêm vào Children 2 lần, nhân đôi toàn bộ cây lúc hiển thị. Test này xác nhận: chỉ cần
// query rồi lọc ParentTaskId == null là đủ, KHÔNG được tự nối Children bằng tay.
public class TaskTreeFixupTests
{
    [Fact]
    public async Task LoadingAllProjectTasks_AutoFixesUpChildrenWithoutManualLinking()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<CmcTsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var projectId = 1;
        await using (var seedDb = new CmcTsDbContext(options))
        {
            var user = new User { SamAccountName = "u1", DisplayName = "U1", CreatedAt = DateTime.UtcNow };
            var project = new Project
            {
                Id = projectId,
                Name = "Test",
                FiscalYear = "2026-2027",
                BusinessUnit = BusinessUnit.ENT,
                CreatedByUser = user,
                CreatedAt = DateTime.UtcNow,
            };

            var level1 = new TaskItem { ProjectId = projectId, Level = TaskLevel.Level1, Code = "1", Name = "CHUẨN BỊ", SourceRow = 1 };
            var level2 = new TaskItem { ProjectId = projectId, Level = TaskLevel.Level2, Code = "1.1", Name = "Quản trị", SourceRow = 2, ParentTask = level1 };
            var leaf1 = new TaskItem { ProjectId = projectId, Level = TaskLevel.Level3, Name = "Leaf A", SourceRow = 3, ParentTask = level2 };
            var leaf2 = new TaskItem { ProjectId = projectId, Level = TaskLevel.Level3, Name = "Leaf B", SourceRow = 4, ParentTask = level2 };

            seedDb.Projects.Add(project);
            seedDb.Tasks.AddRange(level1, level2, leaf1, leaf2);
            await seedDb.SaveChangesAsync();
        }

        // DbContext mới, giống hệt cách ProjectTasks.razor load: query hết Task của dự án rồi lọc root.
        await using var readDb = new CmcTsDbContext(options);
        var allTasks = await readDb.Tasks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.SourceRow)
            .ToListAsync();

        var roots = allTasks.Where(t => t.ParentTaskId is null).ToList();

        var root = Assert.Single(roots);
        var level2Loaded = Assert.Single(root.Children); // trước khi sửa bug: ra 2 phần tử trùng nhau
        Assert.Equal(2, level2Loaded.Children.Count); // trước khi sửa bug: ra 4 phần tử (mỗi leaf x2)
    }
}
