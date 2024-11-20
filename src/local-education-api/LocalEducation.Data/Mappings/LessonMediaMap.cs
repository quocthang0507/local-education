using LocalEducation.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalEducation.Data.Mappings
{
	public class LessonMediaMap : IEntityTypeConfiguration<LessonMedia>
	{
		public void Configure(EntityTypeBuilder<LessonMedia> builder)
		{
			builder.ToTable("LessonMedia");

			builder.HasKey(l => l.Id);

			#region Properties config

			builder.Property(l => l.UrlPath)
				.HasMaxLength(512)
				.HasDefaultValue("");

			builder.Property(c => c.CreatedDate)
				.IsRequired()
				.HasColumnType("datetime");

			#endregion

			#region Relationships

			#endregion

		}
	}
}
