// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;

namespace AuthoringDemo
{
    public sealed class FolderEnumeration
    {
        public string AllFiles { get; set; } 
        public IList<IList<string>> GroupedFiles { get; set; } = new List<IList<string>>();

        /// <summary>
        /// list all the files and folders in Pictures library 
        /// </summary>
        public IAsyncAction GetFilesAndFoldersAsync()
        {
            return Task.Run(async () =>
            {
                StorageFolder picturesFolder = await KnownFolders.GetFolderForUserAsync(null /* current user */, KnownFolderId.PicturesLibrary);
                IReadOnlyList<StorageFile> fileList = await picturesFolder.GetFilesAsync();
                IReadOnlyList<StorageFolder> folderList = await picturesFolder.GetFoldersAsync();

                var count = fileList.Count + folderList.Count;
                StringBuilder outputText = new(picturesFolder.Name + " (" + count + ")\n\n");

                foreach (StorageFolder folder in folderList)
                {
                    outputText.AppendLine("    " + folder.DisplayName + "\\");
                }
                foreach (StorageFile file in fileList)
                {
                    outputText.AppendLine("    " + file.Name);
                }
                AllFiles = outputText.ToString();
            }).AsAsyncAction();
        }

        /// <summary>
        /// print grouped files from group by functions
        /// </summary>
        public void PrintGroupedFiles()
        {
            foreach (var slist in GroupedFiles)
            {
                foreach (var file in slist)
                {
                    Console.WriteLine(file);
                }
                Console.WriteLine("\n");
            }
        }

        /// <summary>
        /// list all the files and folders in Pictures library by month
        /// </summary>
        public IAsyncAction GroupByMonthAsync()
        {
            return Task.Run(async () =>
            {
                await GroupByHelperAsync(new QueryOptions(CommonFolderQuery.GroupByMonth));
            }).AsAsyncAction();
        }

        /// <summary>
        /// list all the files and folders in Pictures library by rating
        /// </summary>
        public IAsyncAction GroupByRatingAsync()
        {
            return Task.Run(async () =>
            {
                await GroupByHelperAsync(new QueryOptions(CommonFolderQuery.GroupByRating));
            }).AsAsyncAction();
        }

        /// <summary>
        /// list all the files and folders in Pictures library by tag
        /// </summary>
        public IAsyncAction GroupByTagAsync()
        {
            return Task.Run(async () =>
            {
                await GroupByHelperAsync(new QueryOptions(CommonFolderQuery.GroupByTag));
            }).AsAsyncAction();
        }

        /// <summary>
        /// helper for all list by functions
        /// </summary>
        private async Task GroupByHelperAsync(QueryOptions queryOptions)
        {
            GroupedFiles.Clear();

            StorageFolder picturesFolder = await KnownFolders.GetFolderForUserAsync(null /* current user */, KnownFolderId.PicturesLibrary);
            StorageFolderQueryResult queryResult = picturesFolder.CreateFolderQueryWithOptions(queryOptions);

            IReadOnlyList<StorageFolder> folderList = await queryResult.GetFoldersAsync();
            foreach (StorageFolder folder in folderList)
            {
                IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync();
                var newList = new List<string>
                {
                    "Group: " + folder.Name + " (" + fileList.Count + ")"
                };

                GroupedFiles.Add(newList);
                foreach (StorageFile file in fileList)
                {
                    newList.Add(file.Name);
                }
            }
        }
    }
}
