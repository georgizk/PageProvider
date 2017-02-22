using PageProvider.Collection;
using PageProvider.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace PageProvider.DataSource
{
    public class MangaDataSource : IVirtualizedRandomAccessCollectionDataSource<Manga>
    {
        private ReadingList rl;
        private Manga placeholderManga;

        public MangaDataSource(ReadingList rl)
        {
            placeholderManga = new Manga();
            this.rl = rl;
        }
        public int Count
        {
            get
            {
                return rl.NumItems;
            }
        }

        public Manga Placeholder
        {
            get
            {
                return placeholderManga;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Manga this[int index]
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
        
        public IAsyncOperation<Manga> getItemAsync(int index)
        {
            return AsyncInfo.Run((c) => getMangaAsyncTask(c, index));
        }

        private Task<Manga> getMangaAsyncTask(CancellationToken c, int index)
        {
            return Task.Run(() =>
            {
                string path = rl.Items[index];
                var m = new Manga(path);
                return m;
            }, c);
        }

        public int IndexOf(Manga item)
        {
            var path = item.ArchivePath;
            return rl.Items.IndexOf(path);
        }

        public void Add(Manga item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(Manga item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Manga[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(Manga item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<Manga> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, Manga item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
    }
}
