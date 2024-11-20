using LocalEducation.Core.Entities;

namespace LocalEducation.Core.Dto
{
	public class LessonMediaItem
	{
		public Guid Id { get; set; }

		public DateTime CreatedDate { get; set; }

		public Guid LessonId { get; set; }

		public string UrlPath { get; set; }

		public LessonMediaItem(LessonMedia l)
		{
			Id = l.Id;
			CreatedDate = l.CreatedDate;
			LessonId = l.LessonId;
			UrlPath = l.UrlPath;
		}
	}
}
