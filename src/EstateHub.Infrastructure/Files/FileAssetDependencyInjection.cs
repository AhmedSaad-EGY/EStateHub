using EstateHub.Application.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.Files;

public static class FileAssetDependencyInjection
{
    public static IServiceCollection AddInfrastructureFileAssets(this IServiceCollection services, IConfiguration configuration, string contentRootPath, string? webRootPath)
    {
        var section = configuration.GetSection("FileStorage");
        var rootPath = section["RootPath"];
        if (!long.TryParse(section["MaximumImageBytes"], out var maximumImageBytes) || maximumImageBytes <= 0) throw new InvalidOperationException("FileStorage:MaximumImageBytes must be positive.");
        if (!long.TryParse(section["MaximumDocumentBytes"], out var maximumDocumentBytes) || maximumDocumentBytes <= 0) throw new InvalidOperationException("FileStorage:MaximumDocumentBytes must be positive.");
        var webRoot = string.IsNullOrWhiteSpace(webRootPath) ? Path.Combine(contentRootPath, "wwwroot") : webRootPath;
        var resolvedRoot = LocalFileStorage.ResolveConfiguredRoot(rootPath ?? string.Empty, contentRootPath, webRoot);
        var options = new FileStorageOptions(resolvedRoot, maximumImageBytes, maximumDocumentBytes);
        services.AddSingleton(options);
        services.AddSingleton<ILocalFileStorage>(_ => new LocalFileStorage(resolvedRoot));
        services.AddScoped<IFileAssetService, FileAssetService>();
        return services;
    }
}
