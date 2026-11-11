using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Common;

namespace Resturant.Infrastructure.Persistence.Configurations;

public abstract class BaseEntityConfiguration<TEntity>
    : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(entity => entity.Id);

        // الـ ID بيتولد داخل BaseEntity.
        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.CreatedAtUtc)
            .IsRequired();

        builder.Property(entity => entity.UpdatedAtUtc);

        // SQL Server rowversion for optimistic concurrency.
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();

        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(
        EntityTypeBuilder<TEntity> builder);
}
