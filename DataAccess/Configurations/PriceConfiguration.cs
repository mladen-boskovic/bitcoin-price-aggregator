using Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DataAccess.Configurations
{
    public class PriceConfiguration : EntityConfiguration<Price>
    {
        protected override void ConfigureRules(EntityTypeBuilder<Price> builder)
        {
            builder.HasIndex(x => new { x.Instrument, x.TimestampUtc }).IsUnique();

            var comparer = new ValueComparer<Dictionary<string, double>>(
                (c1, c2) => c1 != null && c2 != null
                            && c1.Count == c2.Count
                            && c1.All(kv => c2.ContainsKey(kv.Key) && c2[kv.Key] == kv.Value),
                c => c.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
                c => new Dictionary<string, double>(c)
            );

            builder.Property(x => x.ProviderPrices)
               .HasConversion(
                   v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                   v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, (JsonSerializerOptions?)null)!)
               .Metadata.SetValueComparer(comparer);
        }
    }
}
