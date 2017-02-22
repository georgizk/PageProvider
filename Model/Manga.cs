using System.IO;
using System.ComponentModel;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using PageProvider.DataSource;

namespace PageProvider.Model
{
    public class Manga: INotifyPropertyChanged
    {
        private const string KEY_LAST_READ_PREFIX = "manga_last_read";
        private const string KEY_FINISHED_READING_PREFIX = "manga_finished_reading";
        private string path;
        public Manga(string filePath = null)
        {
            path = filePath;
            cover = null;
            if (path != null)
            {
                name = Path.GetFileName(path);
            }
            else
            {
                name = "Unknown";
            }
            pages = null;
        }

        public void replaceWith(Manga m)
        {
            Name = m.name;
            NumPages = m.numPages;
            Cover = m.cover;
            ArchivePath = m.ArchivePath;
            Pages = m.Pages;
        }

        private void notifyPopertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string ArchivePath
        {
            get
            {
                return path;
            }
            private set
            {
                path = value;
                notifyPopertyChanged("ArchivePath");
                notifyPopertyChanged("LastReadPageIdx");
                notifyPopertyChanged("DoneReading");
                notifyPopertyChanged("StatusColor");
            }
        }

        private string name;
        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
                notifyPopertyChanged("Name");
            }
        }

        private int numPages;
        public int NumPages
        {
            get
            {
                return numPages;
            }
            set
            {
                numPages = value;
                notifyPopertyChanged("NumPages");
            }
        }

        private MangaPage cover;
        public MangaPage Cover
        {
            get
            {
                return cover;
            }

            set
            {
                cover = value;
                notifyPopertyChanged("Cover");
            }
        }

        private PageDataSource pages;
        public PageDataSource Pages
        {
            get
            {
                return pages;
            }

            set
            {
                pages = value;
                notifyPopertyChanged("Pages");
            }
        }
        
        public int LastReadPageIdx
        {
            get
            {
                var lastPage = retrieveLocalIntSetting(KEY_LAST_READ_PREFIX + ArchivePath);
                return lastPage;
            }

            set
            {
                storeLocalIntSetting(KEY_LAST_READ_PREFIX + ArchivePath, value);
                notifyPopertyChanged("LastReadPageIdx");
            }
        }

        public bool DoneReading
        {
            get
            {
                var done = retrieveLocalIntSetting(KEY_FINISHED_READING_PREFIX + ArchivePath);
                return done > 0;
            }

            set
            {
                int val = 0;
                if (value)
                {
                    val = 1;
                }
                storeLocalIntSetting(KEY_FINISHED_READING_PREFIX + ArchivePath, val);
                notifyPopertyChanged("DoneReading");
            }
        }

        public SolidColorBrush StatusColor
        {
            get
            {
                Color c;
                if (DoneReading)
                {
                    // finished reading
                    c = (Color)XamlBindingHelper.ConvertValue(typeof(Color), "#00CC6A");
                }
                else
                {
                    if (LastReadPageIdx > 0)
                    {
                        // not finished, but read before
                        c = (Color)XamlBindingHelper.ConvertValue(typeof(Color), "#E3008C");
                        
                    }
                    else
                    {
                        // new, never read before
                        c = (Color)XamlBindingHelper.ConvertValue(typeof(Color), "#0078D7");
                    }                    
                }
                var b = new SolidColorBrush(c);
                return b;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        
        private void storeLocalIntSetting(string key, int value)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[key] = value;
        }

        private int retrieveLocalIntSetting(string key)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var setting = localSettings.Values[key];
            if (setting == null)
            {
                return -1;
            }
            return (int)setting;
        }
    }
}
