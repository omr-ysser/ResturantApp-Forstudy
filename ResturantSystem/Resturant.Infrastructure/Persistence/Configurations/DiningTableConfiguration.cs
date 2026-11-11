using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class DiningTableConfiguration
    : BaseEntityConfiguration<DiningTable>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<DiningTable> builder)
    {
        builder.ToTable("DiningTables", table =>
        {
            table.HasCheckConstraint(
                "CK_DiningTables_Number",
                "[Number] > 0");

            table.HasCheckConstraint(
                "CK_DiningTables_Capacity",
                "[Capacity] > 0");
        });

        builder.HasIndex(table => table.Number)
            .IsUnique();

        builder.Property(table => table.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
    }
}
