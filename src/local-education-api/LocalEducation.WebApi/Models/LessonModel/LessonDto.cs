﻿namespace LocalEducation.WebApi.Models.LessonModel;

public class LessonDto
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }

    public string Title { get; set; }

    public string UrlSlug { get; set; }

    public string Description { get; set; }

    public string ThumbnailPath { get; set; }

    public string UrlPath { get; set; }

    public int Index { get; set; }

    public bool IsVr { get; set; }

    public string TourSlug { get; set; }

    public int TotalSlide { get; set; }

}