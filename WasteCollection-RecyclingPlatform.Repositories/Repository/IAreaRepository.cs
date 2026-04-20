using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public interface IAreaRepository
{
    Task<List<Area>> GetAllAsync(CancellationToken ct = default);
    Task BulkUpdateAsync(List<Area> areas, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
    Task<bool> UpdateNameAndCapacityAsync(long id, string newName, decimal newCapacity, CancellationToken ct = default);
}
