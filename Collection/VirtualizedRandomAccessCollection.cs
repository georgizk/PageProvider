using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace PageProvider.Collection
{
    public class CacheMissEventArgs: EventArgs
    {
        public int index;
        public CacheMissEventArgs(int index)
        {
            this.index = index;
        }
    }

    public interface IVirtualizedRandomAccessCollectionDataSource<T>: IList<T>, INotifyCollectionChanged
    {
        T Placeholder { get; }
        IAsyncOperation<T> getItemAsync(int index);
    }   
  
    public class VirtualizedRandomAccessCollection<T> : IList<T>, IList, INotifyCollectionChanged, IItemsRangeInfo
    {
        private IVirtualizedRandomAccessCollectionDataSource<T> dataSource;
        private SortedList<int, T> cachedItems;
        private SortedSet<int> requestedIndexes;

        public VirtualizedRandomAccessCollection(IVirtualizedRandomAccessCollectionDataSource<T> dataSource = null)
        {
            cachedItems = new SortedList<int, T>();
            requestedIndexes = new SortedSet<int>();
            this.dataSource = dataSource;
        }

        public void SetDataSource(IVirtualizedRandomAccessCollectionDataSource<T> dataSource)
        {
            lock (accessLock)
            {
                cachedItems = new SortedList<int, T>();
            }
            lock (requestedIndexesLock)
            {
                requestedIndexes = new SortedSet<int>();
            }

            if (_pending != null)
            {
                _pending.Cancel();
            }

            if (this.dataSource != null)
            {
                this.dataSource.CollectionChanged -= DataSource_CollectionChanged;
            }

            this.dataSource = dataSource;
            this.dataSource.CollectionChanged += DataSource_CollectionChanged;

            notifyReset();          
        }
        
        public void notifyReset()
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void clearCached(int startingIndex, int count, bool notify = false)
        {
            if (dataSource == null)
            {
                return;
            }

            lock (accessLock)
            {
                for (int i = startingIndex; i < startingIndex + count; ++i)
                {
                    if (cachedItems.ContainsKey(i))
                    {
                        var oldItem = cachedItems[i];
                        cachedItems.Remove(i);
                        if (CollectionChanged != null && !oldItem.Equals(dataSource.Placeholder) && notify)
                        {
                            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, dataSource.Placeholder, cachedItems[i], i);
                            CollectionChanged(this, args);
                        }
                    }
                }
            }
        }

        private void cacheItems(int startingIndex, IList items)
        {
            lock (accessLock)
            {
                int index = startingIndex;
                foreach (var item in items)
                {
                    cachedItems[index] = (T)item;
                    index++;
                }
            }
        }

        private void DataSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    cacheItems(e.NewStartingIndex, e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    cacheItems(e.NewStartingIndex, e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Move:
                    clearCached(e.OldStartingIndex, e.OldItems.Count);
                    cacheItems(e.NewStartingIndex, e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    clearCached(e.OldStartingIndex, e.OldItems.Count);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    cachedItems.Clear();
                    break;
                default:
                    break;
            }

            CollectionChanged?.Invoke(this, e);
        }

        protected event EventHandler CacheMiss;

        private void notifyCacheMiss(int index)
        {
            lock (requestedIndexesLock)
            {
                requestedIndexes.Add(index);
            }
            CacheMiss?.Invoke(this, new CacheMissEventArgs(index));
        }

        private bool _busy = false;
        private void loadRequestedIndexes()
        {
            if (_busy)
            {
                // will get around to it later - limit to one loading at a time
                return;
            }
            int idx;
            lock (requestedIndexesLock)
            {
                if (requestedIndexes.Count == 0)
                {
                    return;
                }
                idx = requestedIndexes.First();
                requestedIndexes.Remove(idx);
            }

            loadItem(idx);
        }

        private object requestedIndexesLock = new object();
        private IAsyncOperation<T> _pending;
        protected virtual async void loadItem(int index)
        {
            if (dataSource == null)
            {
                return;
            }

            if (_busy)
            {
                return;
            }

            try
            {
                _busy = true;
                _pending = dataSource.getItemAsync(index);
                var m = await _pending;
                this[index] = m;
            }
            catch (Exception)
            {

            }
            finally
            {
                _busy = false;
                loadRequestedIndexes();
            }
        }
       
        private object accessLock = new object();
        public T this[int index]
        {
            get
            {
                if (dataSource == null || index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }
                lock(accessLock)
                {
                    // if the item hasn't been cached yet, return a placeholder
                    // value and fire the cache miss event
                    if (!cachedItems.ContainsKey(index))
                    {
                        notifyCacheMiss(index);
                        try
                        {
                            return dataSource.Placeholder;
                        }
                        finally
                        {
                            loadRequestedIndexes();
                        }
                    }

                    return cachedItems[index];
                }
            }

            set
            {
                if (dataSource == null)
                {
                    return;
                }
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }
                lock (accessLock)
                {
                    // this is always going to be a replace operation;
                    // the item being replaced is either the palceholder value, or the
                    // real cached value
                    T oldItem;
                    if (!cachedItems.TryGetValue(index, out oldItem))
                    {
                        // item not found, use placeholder
                        oldItem = dataSource.Placeholder;
                    }
                    cachedItems[index] = value;
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, dataSource.Placeholder, index);
                    CollectionChanged?.Invoke(this, args);
                }

                lock (requestedIndexesLock)
                {
                    requestedIndexes.Remove(index);
                }
            }
        }

        protected virtual void OnCollectionChangedMultiItem(
        NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler handlers = this.CollectionChanged;
            if (handlers != null)
            {
                foreach (NotifyCollectionChangedEventHandler handler in
                    handlers.GetInvocationList())
                {
                    handler(this, e);
                }
            }
        }
        
        public int Count
        {
            get
            {
                if (dataSource == null)
                {
                    return 0;
                }
                return dataSource.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public object MainView { get; private set; }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public object SyncRoot
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                throw new NotImplementedException();
            }
        }
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public virtual void Add(T item)
        {
            throw new NotImplementedException();
        }

        public virtual void Clear()
        {
            throw new NotImplementedException();
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            if (dataSource != null)
            {
                dataSource.CopyTo(array, arrayIndex);
            }            
        }

        public void Dispose()
        {
            cachedItems = null;
            dataSource = null;
            requestedIndexes = null;
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        
        public void RangesChanged(ItemIndexRange visibleRange, IReadOnlyList<ItemIndexRange> trackedItems)
        {
            // get a union of visible and tracked items
            // for each item that is visible or tracked, but not cached, request retrieval
            // for each item that is cached, but neither visible nor tracked, request deletion
            // trackedItems is a superset of visibleRange, so only need to check trackedItems.

            List<int> loadedKeys;
            lock (accessLock)
            {
                loadedKeys = new List<int>(cachedItems.Keys);
            }

            var trackedKeys = new SortedSet<int>();
            foreach (var r in trackedItems)
            {
                for (int i = r.FirstIndex; i <= r.LastIndex; ++i)
                {
                    trackedKeys.Add(i);
                }
            }

            // retained keys are both tracked, and already loaded in cache
            var retainedKeys = new List<int>(trackedKeys.Intersect(loadedKeys));

            // unneeded keys are loaded in cache, but no longer tracked - can be deleted
            var unneededKeys = loadedKeys.Where(p => !retainedKeys.Contains(p));

            // needed keys are tracked, but not in cache - need to be loaded
            var neededKeys = trackedKeys.Where(p => !retainedKeys.Contains(p));

            // first clear space
            lock (accessLock)
            {
                foreach (var idx in unneededKeys)
                {
                    cachedItems.Remove(idx);
                }
            }

            // then request more
            lock (requestedIndexesLock)
            {
                requestedIndexes.Clear();
                if (_pending != null)
                {
                    _pending.Cancel();
                }
                foreach (var idx in neededKeys)
                {
                    requestedIndexes.Add(idx);
                }
            }
            loadRequestedIndexes();
        }

        public virtual bool Remove(T item)
        {
            throw new NotImplementedException();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T item)
        {
            if (dataSource == null)
            {
                return false;
            }
            return dataSource.Contains(item);
        }

        public int IndexOf(T item)
        {
            if (dataSource == null)
            {
                return -1;
            }
            return dataSource.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            if (dataSource == null)
            {
                return false;
            }
            return dataSource.Contains((T)value);
        }

        public int IndexOf(object value)
        {
            if (dataSource == null)
            {
                return -1;
            }
            return dataSource.IndexOf((T)value);
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            if (dataSource == null)
            {
                return;
            }
            dataSource.CopyTo((T[])array, index);
        }
    }
}
