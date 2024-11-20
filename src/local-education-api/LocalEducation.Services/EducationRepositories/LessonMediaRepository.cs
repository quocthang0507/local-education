using LocalEducation.Core.Dto;
using LocalEducation.Core.Entities;
using LocalEducation.Data.Contexts;
using LocalEducation.Services.EducationRepositories.Interfaces;
using LocalEducation.Services.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace LocalEducation.Services.EducationRepositories
{
	public class LessonMediaRepository : ILessonMediaRepository
	{
		private readonly LocalEducationDbContext _context;

		public LessonMediaRepository(LocalEducationDbContext context)
		{
			_context = context;
		}

		public async Task<LessonMedia> AddOrUpdateLessonMediaAsync(LessonMedia lessonMedia, CancellationToken cancellationToken = default)
		{
			List<LessonMedia> listLessonMedia = _context.Set<LessonMedia>().Where(l => l.LessonId == lessonMedia.LessonId).ToList();

			if (lessonMedia.Id == Guid.Empty)
			{
				lessonMedia.CreatedDate = DateTime.Now;

				_context.Set<LessonMedia>().Add(lessonMedia);
			}
			else
			{
				_context.Set<LessonMedia>().Update(lessonMedia);
			}

			await _context.SaveChangesAsync(cancellationToken);

			return lessonMedia;
		}

		public async Task<bool> DeleteLessonMediaAsync(Guid lessonMediaId, CancellationToken cancellationToken = default)
		{
			return await _context.Set<LessonMedia>()
				.Where(s => s.Id == lessonMediaId)
				.ExecuteDeleteAsync(cancellationToken) > 0;
		}

		public async Task<LessonMedia> GetLessonMediaByIdAsync(Guid lessonMediaId, CancellationToken cancellationToken = default)
		{
			DbSet<LessonMedia> lessonMedia = _context.Set<LessonMedia>();

			return await lessonMedia.FirstOrDefaultAsync(s => s.Id == lessonMediaId, cancellationToken);
		}

		public async Task<IList<LessonMediaItem>> GetLessonMediaByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default)
		{
			IQueryable<LessonMedia> lessonMedia = _context.Set<LessonMedia>();

			return await lessonMedia
				.Where(l => l.LessonId == lessonId)
				.Select(s => new LessonMediaItem(s))
				.ToListAsync(cancellationToken);
		}
	}
}
