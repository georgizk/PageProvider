using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using SharpCompress.Archives;
using Windows.Graphics.Imaging;

namespace PageProvider.PageLoader
{
    public class ZipLoader : Loader
    {
        private List<IArchiveEntry> entries;
        private MangaPageLoader pgLoader;

        public ZipLoader(Stream archiveStream)
        {
            IArchive archive = ArchiveFactory.Open(archiveStream);
            entries = getEntries(archive);
            pgLoader = new MangaPageLoader();
        }

        public override int Count
        {
            get
            {
                return entries.Count;
            }
        }

        public override string ImageName(int index)
        {
            if (index >= Count || index < 0)
            {
                return null;
            }
            return entries[index].Key;
        }

        IAsyncOperation<SoftwareBitmap> _ongoingTask;

        public override IAsyncOperation<SoftwareBitmap> loadImage(int index, int width = 0, int height = 0)
        {
            if (_ongoingTask != null)
            {
                _ongoingTask.Cancel();
                _ongoingTask = null;
            }

            var r = AsyncInfo.Run(async (workItem) =>
            {
                if (index >= Count || index < 0)
                {
                    return null;
                }
                workItem.ThrowIfCancellationRequested();
                var e = entries[index];
                var str = e.OpenEntryStream();
                var strCopy = new MemoryStream();
                await str.CopyToAsync(strCopy);
                SoftwareBitmap bmp;
                workItem.ThrowIfCancellationRequested();
                if (width != 0 && height != 0)
                {
                    _ongoingTask = pgLoader.AsThumbnailAsync(strCopy.AsRandomAccessStream(), width, height);
                }
                else
                {
                    _ongoingTask = pgLoader.AsImageAsync(strCopy.AsRandomAccessStream());
                }
                try
                {
                    bmp = await _ongoingTask;
                    return bmp;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }                
            });
            return r;
        }

        private static List<IArchiveEntry> getEntries(IArchive archive)
        {
            if (archive == null)
            {
                return new List<IArchiveEntry>();
            }
            var e = archive.Entries.Where(item => IsAllowedFiletype(item.Key)).OrderBy(item => item.Key);
            return new List<IArchiveEntry>(e);
        }
    }
}
