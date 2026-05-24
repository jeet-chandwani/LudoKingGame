namespace LudoKing.Interfaces.Infrastructure;

public interface IFileStore
{
    Task<string> UploadAvatarAsync(string userId, Stream fileStream, string contentType);
    Task DeleteAvatarAsync(string userId);
}
