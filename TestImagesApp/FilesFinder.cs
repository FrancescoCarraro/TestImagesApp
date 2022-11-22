using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace TestImagesApp
{
    public sealed class FilesFinder
    {
        public async Task<IReadOnlyList<StorageFile>> findFilesAsync()
        {
            List<string> fileTypeFilter = new List<string>();
            fileTypeFilter.Add(".jpg");
            fileTypeFilter.Add(".png");
            fileTypeFilter.Add(".bmp");
            fileTypeFilter.Add(".gif");
            //fileTypeFilter.Add(".mp4");

            var queryOptions = new QueryOptions(CommonFileQuery.OrderByDate, fileTypeFilter);
            queryOptions.FolderDepth = FolderDepth.Deep;

            var cacheFolder = KnownFolders.PicturesLibrary;
            var result = cacheFolder.CreateFileQueryWithOptions(queryOptions);
            var files = await result.GetFilesAsync();

            System.Diagnostics.Debug.WriteLine($"found {files.Count} files");
            //foreach (var file in files)
            //    System.Diagnostics.Debug.WriteLine($"{file.Name}, {file.Path}, {file.DisplayType}");

            return files;
        }
    }
}
