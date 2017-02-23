using PageProvider.Collection;
using PageProvider.Model;
using PageProvider.PageLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace PageProvider.DataSource
{
    public class PageDataSource : IVirtualizedRandomAccessCollectionDataSource<MangaPage>
    {
        Loader loader;
        private MangaPage placeholder;
        private int width;
        private int height;

        public PageDataSource(Loader loader, int width, int height)
        {
            this.loader = loader;
            this.width = width;
            this.height = height;
            placeholder = new MangaPage(-1);
        }

        public static IAsyncOperation<PageDataSource> FromFileAsync(IStorageFile file, int width, int height)
        {
            return AsyncInfo.Run(async (c) =>
            {
                var archiveStream = await file.OpenStreamForReadAsync();
                var l = new ZipLoader(archiveStream);
                return new PageDataSource(l, width, height);
            });
        }
        
        public int Count
        {
            get
            {
                return loader.Count;
            }
        }

        public MangaPage Placeholder
        {
            get
            {
                return placeholder;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public MangaPage this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged = delegate { };

        public IAsyncOperation<MangaPage> getItemAsync(int index)
        {
            return AsyncInfo.Run((c) => getPageAsyncTask(c, index));
        }

        private IAsyncOperation<SoftwareBitmap> _loadImage;
        private async Task<MangaPage> getPageAsyncTask(CancellationToken c, int index)
        {
            if (_loadImage != null)
            {
                _loadImage.Cancel();
                _loadImage = null;
            }
            c.ThrowIfCancellationRequested();
            _loadImage = loader.loadImage(index, width, height);
            var bmp = await _loadImage;
            var name = loader.ImageName(index);
            c.ThrowIfCancellationRequested();
            var mp = new MangaPage(index, name, bmp);
            return mp;
        }

        public int IndexOf(MangaPage item)
        {
            var idx = item.Index;
            if (idx < Count && idx > -1)
            {
                return idx;
            }
            return -1;
        }

        public void Add(MangaPage item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(MangaPage item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(MangaPage[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(MangaPage item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<MangaPage> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, MangaPage item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
    }
}
