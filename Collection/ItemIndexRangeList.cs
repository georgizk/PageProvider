using System.Collections.Generic;
using Windows.UI.Xaml.Data;

namespace PageProvider.Collection
{
    class ItemIndexRangeList: ISelectionInfo
    {
        private object selectionLock = new object();
        private SortedSet<int> selectedIndexes = new SortedSet<int>();

        public void DeselectRange(ItemIndexRange itemIndexRange)
        {
            lock (selectionLock)
            {
                for (int i = itemIndexRange.FirstIndex; i <= itemIndexRange.LastIndex; ++i)
                {
                    if (selectedIndexes.Contains(i))
                    {
                        selectedIndexes.Remove(i);
                    }
                }
            }
        }

        public void SelectRange(ItemIndexRange itemIndexRange)
        {
            lock (selectionLock)
            {
                for (int i = itemIndexRange.FirstIndex; i <= itemIndexRange.LastIndex; ++i)
                {
                    if (!selectedIndexes.Contains(i))
                    {
                        selectedIndexes.Add(i);
                    }
                }
            }
        }

        public bool IsSelected(int index)
        {
            lock (selectionLock)
            {
                return selectedIndexes.Contains(index);
            }
        }

        public static IReadOnlyList<ItemIndexRange> GetRangesFromIntList(IReadOnlyCollection<int> items)
        {
            var l = new List<ItemIndexRange>();
            ItemIndexRange currentRange = null;
            foreach (var i in items)
            {
                if (currentRange == null)
                {
                    currentRange = new ItemIndexRange(i, 1);
                }
                else
                {
                    if ((currentRange.FirstIndex + currentRange.Length) == i)
                    {
                        currentRange = new ItemIndexRange(currentRange.FirstIndex, currentRange.Length + 1);
                    }
                    else
                    {
                        l.Add(currentRange);
                        currentRange = new ItemIndexRange(i, 1);
                    }
                }
            }
            if (currentRange != null)
            {
                l.Add(currentRange);
            }
            
            return l;
        }

        public IReadOnlyList<ItemIndexRange> GetSelectedRanges()
        {
            lock (selectionLock)
            {
                return GetRangesFromIntList(selectedIndexes);
            }
        }
    }
}
