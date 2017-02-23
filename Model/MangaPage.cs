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

        private string name;
        public string Name
        {
            get
            {
                return name;
            }
        }

        public MangaPage(int index, string name = "", SoftwareBitmap src = null)
        {
            this.index = index;
            this.src = src;
            this.name = name;
        }
    }
}
