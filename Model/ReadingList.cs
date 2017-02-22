using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PageProvider.Model
{
    public class ReadingList
    {
        private string token;
        public string Token
        {
            get
            {
                return token;
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

        private List<string> items;
        private object itemsMux = new object();
        public int NumItems
        {
            get
            {
                return Items.Count;                
            }
        }

        public List<string> Items
        {
            get
            {
                lock (itemsMux)
                {
                    return new List<string>(items);
                }
            }
        }

        public List<Manga> Mangas
        {
            get
            {
                var r = new List<Manga>();
                var i = Items;
                foreach (var f in i)
                {
                    Manga m = new Manga(f);
                    r.Add(m);
                }
                return r;
            }
        }

        public ReadingList(string token, string name): this(token, name, new List<string>())
        {

        }

        public ReadingList(string token, string name, List<string> items)
        {
            this.name = name;
            this.token = token;
            this.items = items;
        }
        
        public static async Task<ReadingList> FromFileAsync(IStorageFile f)
        {
            using (var ios = await f.OpenAsync(FileAccessMode.Read))
            {
                ulong size = ios.Size;
                string text = "";
                using (var inputStream = ios.GetInputStreamAt(0))
                {
                    using (var dataReader = new DataReader(inputStream))
                    {
                        uint numBytesLoaded = await dataReader.LoadAsync((uint)size);
                        text = dataReader.ReadString(numBytesLoaded);
                    }
                }

                string[] lines = text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var items = new List<string>(lines);
                var name = "Untitled";
                if (items.Count > 0)
                {
                    name = items[0];
                    items.RemoveAt(0);
                }
                return new ReadingList(f.Path, name, items);
            }
        }

        public void appendItemsAsync(List<string> itm)
        {
            lock (itemsMux)
            {
                items.AddRange(itm);
            }
        }

        public void Clear()
        {
            lock (itemsMux)
            {
                items.Clear();
            }
        }

        public void RemoveAt(int index)
        {
            lock (itemsMux)
            {
                items.RemoveAt(index);
            }
        }

        public async void saveAsync()
        {
            if (Token == null || Token == "")
            {
                throw new Exception("Token should be set");
            }
            var f = await StorageFile.GetFileFromPathAsync(Token);
            using (var ios = await f.OpenAsync(FileAccessMode.ReadWrite))
            {
                ios.Size = 0;
                using (var outputStream = ios.GetOutputStreamAt(0))
                {
                    using (var dw = new DataWriter(outputStream))
                    {
                        dw.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                        dw.WriteString(name + "\n");
                        foreach (string item in Items)
                        {
                            var b = dw.WriteString(item + "\n");
                        }
                        
                        await dw.StoreAsync();
                        await outputStream.FlushAsync();
                    }
                }
            }
        }

        public async void deleteAsync()
        {
            if (Token == null || Token == "")
            {
                throw new Exception("Token should be set");
            }
            var f = await StorageFile.GetFileFromPathAsync(Token);
            await f.DeleteAsync();
        }

        public void renameAsync(string newName)
        {
            name = newName;
            saveAsync();
        }
    }
}
