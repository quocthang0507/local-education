using LocalEducation.Core.Contracts;

namespace LocalEducation.Core.Entities
{
	public class LessonMedia : IEntity
	{
		public Guid Id { get; set; }

		public DateTime CreatedDate { get; set; }

		public Guid LessonId { get; set; }

		public string UrlPath { get; set; }
	}
}
