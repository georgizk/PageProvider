using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace PageProvider.PageLoader
{
    public abstract class Loader
    {
        public abstract int Count { get; }
        public abstract IAsyncOperation<SoftwareBitmap> loadImage(int index, int width = 0, int height = 0);
        
        public static bool IsAllowedFiletype(string filename)
        {
            bool res = Regex.IsMatch(filename, ".*\\.(jpg|jpeg|png)$", RegexOptions.IgnoreCase);
            return res;
        }
    }
}
