using LocalEducation.Core.Dto;
using LocalEducation.Core.Entities;

namespace LocalEducation.Services.EducationRepositories.Interfaces
{
	public interface ILessonMediaRepository
	{
		#region Get data

		Task<IList<LessonMediaItem>> GetLessonMediaByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default);

		Task<LessonMedia> GetLessonMediaByIdAsync(Guid lessonMediaId, CancellationToken cancellationToken = default);

		#endregion

		#region Add, Update & Delete

		Task<LessonMedia> AddOrUpdateLessonMediaAsync(LessonMedia lessonMedia, CancellationToken cancellationToken = default);

		Task<bool> DeleteLessonMediaAsync(Guid lessonMediaId, CancellationToken cancellationToken = default);

		#endregion
	}
}
