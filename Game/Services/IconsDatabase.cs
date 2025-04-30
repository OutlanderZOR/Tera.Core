using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Packaging;
using System.Net.Cache;
using System.Windows.Media.Imaging;

namespace Tera.Game
{
    public class IconsDatabase
    {
        private readonly Dictionary<string, byte[]> _bitmaps = new();
        private readonly Package _icons;
        private readonly Dictionary<string, BitmapImage> _images = new Dictionary<string, BitmapImage>();
        private readonly BitmapImage _emptyBitmap;
        private readonly byte[] _emptyBitmapArray;
        private readonly object _lock = new object();

        public IconsDatabase(string resourceDirectory)
        {
//            IconsDirectory = Path.Combine(resourceDirectory, "icons/");
            _icons = Package.Open(resourceDirectory + "icons.zip", FileMode.Open, FileAccess.Read, FileShare.Read);
            using (MemoryStream memory = new MemoryStream())
            {
                (new Bitmap(1,1)).Save(memory,ImageFormat.Bmp);
                memory.Position = 0;
                _emptyBitmap = new BitmapImage();
                _emptyBitmap.BeginInit();
                _emptyBitmap.StreamSource = memory;
                _emptyBitmap.CacheOption=BitmapCacheOption.OnLoad;
                _emptyBitmap.EndInit();
                _emptyBitmap.Freeze();
                _emptyBitmapArray = memory.ToArray();
            }
        }

        public BitmapImage GetImage(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return _emptyBitmap;
            BitmapImage image;
            if (_images.TryGetValue(iconName, out image))
            {
                return image;
            }
            var ur = new Uri("/" + iconName + ".png", UriKind.Relative);
            lock (_lock) {
                if (_icons.PartExists(ur)) 
                    using (var stream = _icons.GetPart(ur).GetStream()) { 
                        MemoryStream mem=new MemoryStream();
                        stream.CopyTo(mem);
                        image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = mem;
                        image.EndInit();
                        image.Freeze();
                    }
                else { image = _emptyBitmap; }
            }

            //var filename = IconsDirectory + iconName + ".png";
            //image = new BitmapImage(new Uri(filename));
            _images[iconName] = image;
            return image;
        }

        public byte[] GetBitmap(string iconName)
        {
            byte[] image;
            if (_bitmaps.TryGetValue(iconName, out image) || string.IsNullOrEmpty(iconName))
            {
                return image;
            }
            var ur = new Uri("/" + iconName + ".png", UriKind.Relative);
            lock (_lock) {
                if (_icons.PartExists(ur))
                    using (var stream = _icons.GetPart(ur).GetStream())
                    {
                        image = new byte[stream.Length];
                        stream.ReadExactly(image, 0, image.Length);
                    }
                else
                {
                    image = _emptyBitmapArray;
                }
            }
            _bitmaps[iconName] = image;
            return image;
        }
    }
}