using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
