namespace GridAcademy.Data.Entities.VideoLearning;

public class VideoLearningFeatures
{
    public bool Domains       { get; set; } = true;
    public bool VideoLibrary  { get; set; } = true;
    public bool LearningPaths { get; set; } = true;
    public bool Programs      { get; set; } = true;
    public bool Offers        { get; set; } = true;
    public bool SalesChannels { get; set; } = true;
    public bool Enrollments   { get; set; } = true;
}

public class VideoLearningStorageOptions
{
    public string VideoUploadPath           { get; set; } = "uploads/videos";
    public string ThumbnailUploadPath       { get; set; } = "uploads/thumbnails";
    public int    MaxVideoFileSizeMb        { get; set; } = 2048;
    public string[] AllowedVideoExtensions  { get; set; } = [".mp4", ".webm"];
    public string[] AllowedImageExtensions  { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
}
