using Windows.Graphics.Imaging;

namespace PageProvider.Model
{
    public class MangaPage
    {
        private int index;
        public int Index
        {
            get
            {
                return index;
            }
        }

        private SoftwareBitmap src;
        public SoftwareBitmap Source
        {
            get
            {
                return src;
            }
        }

        public MangaPage(int index, SoftwareBitmap src = null)
        {
            this.index = index;
            this.src = src;
        }
    }
}
