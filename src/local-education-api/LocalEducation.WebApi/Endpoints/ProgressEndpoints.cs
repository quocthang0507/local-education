﻿using LocalEducation.Services.EducationRepositories.Interfaces;
using LocalEducation.WebApi.Models.ProgressModel;
using LocalEducation.Core.Collections;
using LocalEducation.WebApi.Utilities;
using LocalEducation.Core.Constants;
using LocalEducation.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using MapsterMapper;
using System.Net;
using LocalEducation.Core.Dto;
using LocalEducation.Core.Entities;
using Mapster;

namespace LocalEducation.WebApi.Endpoints;

public static class ProgressEndpoints
{
    public static WebApplication MapProgressEndpoints(this WebApplication app)
    {
        var builder = app.MapGroup("/api/progress");

        #region Get Method

        builder.MapGet("/", GetPagedProgress)
            .WithName("GetPagedProgress")
            .RequireAuthorization("User")
            .Produces<ApiResponse<PaginationResult<ProgressDto>>>();

        builder.MapGet("/{progressId:guid}", GetProgressById)
            .WithName("GetProgressById")
            .RequireAuthorization("User")
            .Produces<ApiResponse<ProgressDto>>();

        builder.MapGet("/completionPercentage/{courseId:guid}", CompletionPercentage)
            .WithName("CompletionPercentageCourse")
            .RequireAuthorization("User")
            .Produces<ApiResponse<ProgressDto>>();

        #endregion

        #region POST Method

        builder.MapPost("/completed/{slideId:guid}", SetCompletedSlide)
            .WithName("SetCompletedSlide")
            .RequireAuthorization("User")
            .Produces<ApiResponse>();

        #endregion

        return app;
    }

    #region Get function

    private static async Task<IResult> GetPagedProgress(
        HttpContext context,
        [AsParameters] ProgressFilterModel model,
        [FromServices] IProgressRepository repository,
        [FromServices] IMapper mapper)
    {
        try
        {
            var pagedProgress =
                await repository.GetPagedProgressAsync(
                    model.Keyword,
                    context.GetCurrentUser().Id,
                    model, p => p.ProjectToType<ProgressDto>());

            var paginationResult = new PaginationResult<ProgressDto>(pagedProgress);

            foreach (var item in paginationResult.Items)
            {
                item.Completed = await repository.GetProgressPercentageAsync(item.Id);
            }

            return Results.Ok(ApiResponse.Success(paginationResult));
        }
        catch (Exception e)
        {
            return Results.Ok(ApiResponse.Fail(HttpStatusCode.BadRequest, e.Message));
        }
    }

    private static async Task<IResult> GetProgressById(
        HttpContext context,
        [FromRoute] Guid progressId,
        [FromServices] IProgressRepository repository,
        [FromServices] IMapper mapper)
    {
        try
        {
            var progress = await repository.GetProgressByIdAsync(context.GetCurrentUser().Id, progressId);

            if (progress == null)
            {
                return Results.Ok(ApiResponse.Fail(HttpStatusCode.NotFound, "Bạn chưa tham gia khóa học"));
            }

            var completionPercentage = await repository.GetProgressPercentageAsync(progress.Id);

            var result = mapper.Map<ProgressDto>(progress);
            result.Completed = completionPercentage;
            result.Slides = JsonConvert.DeserializeObject<IList<SlideProgress>>(progress.Slides);

            return Results.Ok(ApiResponse.Success(result));
        }
        catch (Exception e)
        {
            return Results.Ok(ApiResponse.Fail(HttpStatusCode.BadRequest, e.Message));
        }
    }

    private static async Task<IResult> SetCompletedSlide(
        HttpContext context,
        [FromRoute] Guid slideId,
        [FromBody] IList<AnswerItem> answers,
        [FromServices] IProgressRepository repository,
        [FromServices] IQuestionRepository questionRepo,
        [FromServices] ISlideRepository slideRepository)
    {
        try
        {
            var slide = await slideRepository.GetSlideByIdAsync(slideId, true);

            if (slide == null)
            {
                return Results.Ok(ApiResponse.Fail(HttpStatusCode.NotFound,
                    "Bài giảng không tồn tại"));
            }

            var progress = await repository.GetProgressByUserIdAsync(context.GetCurrentUser().Id, slide.Lesson.CourseId);

            if (progress == null)
            {
                return Results.Ok(ApiResponse.Fail(HttpStatusCode.NotFound,
                    "Bạn chưa tham gia khóa học"));
            }

            if (slide.IsTest)
            {
                if (answers.Count < slide.MinPoint)
                {
                    return Results.Ok(ApiResponse.Fail(HttpStatusCode.NotAcceptable, $"Bạn chưa làm đủ {answers.Count}/{slide.MinPoint} số câu yêu cầu"));
                }

                var questions = await questionRepo.GetQuestionBySlideIdAsync(slideId);
                if (questions == null)
                {
                    return Results.Ok(
                        ApiResponse.Fail(HttpStatusCode.NotFound, "Không tìm thấy câu hỏi"));
                }

                var resultDetail = await questionRepo.CheckAnswerAsync(context.GetCurrentUser().Id, slideId, answers);

                if (resultDetail != null)
                {
                    var incorrect = JsonConvert.DeserializeObject<IList<AnswerItem>>(resultDetail.Answer);
                    var correctAnswer = JsonConvert.DeserializeObject<IList<AnswerItem>>(resultDetail.CorrectAnswer);

                    if (correctAnswer.Count < slide.MinPoint)
                    {
                        return Results.Ok(ApiResponse.Fail(HttpStatusCode.NoContent, new ResultAnswerDto()
                        {
                            Incorrects = incorrect,
                            Corrects = correctAnswer,
                            CountCorrect = correctAnswer.Count,
                            CountIncorrect = incorrect.Count
                        }, $"Đúng {correctAnswer.Count}/{slide.MinPoint}, không đạt yêu cầu"));
                    }
                    else
                    {
                        await repository.SetCompletedSlideAsync(progress.Id, slide, resultDetail);
                        return Results.Ok(ApiResponse.Success(new ResultAnswerDto()
                        {
                            Incorrects = incorrect,
                            Corrects = correctAnswer,
                            CountCorrect = correctAnswer.Count,
                            CountIncorrect = incorrect.Count
                        }));
                    }
                }
                else
                {
                    return Results.Ok(ApiResponse.Fail(HttpStatusCode.NoContent, "Không tìm thấy kết quả"));
                }
            }
            else
            {
                await repository.SetCompletedSlideAsync(progress.Id, slide, new ResultDetail());
            }

            return Results.Ok(ApiResponse.Success("", HttpStatusCode.NoContent));
        }
        catch (Exception e)
        {
            return Results.Ok(ApiResponse.Fail(HttpStatusCode.BadRequest, e.Message));
        }
    }

    private static async Task<IResult> CompletionPercentage(
        HttpContext context,
        [FromRoute] Guid courseId,
        [FromServices] IProgressRepository repository,
        [FromServices] IMapper mapper)
    {
        try
        {
            var progress = await repository.GetProgressByUserIdAsync(context.GetCurrentUser().Id, courseId);

            if (progress == null)
            {
                return Results.Ok(ApiResponse.Fail(HttpStatusCode.NotFound, "Bạn chưa tham gia khóa học"));
            }

            var completionPercentage = await repository.GetProgressPercentageAsync(progress.Id);

            var result = mapper.Map<ProgressDto>(progress);
            result.Completed = completionPercentage;
            result.Slides = JsonConvert.DeserializeObject<IList<SlideProgress>>(progress.Slides);

            return Results.Ok(ApiResponse.Success(result));
        }
        catch (Exception e)
        {
            return Results.Ok(ApiResponse.Fail(HttpStatusCode.BadRequest, e.Message));
        }
    }

    #endregion
}