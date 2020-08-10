using System;
using System.Collections.Concurrent;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClipboardMonitor;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace ScreenshotCompressor
{
    internal class Program
    {
        private const int EightMb = 8388608;

        private static DateTime _lastEvent;

        [STAThread]
        private static void Main(string[] args)
        {
            ClipboardNotifications.ClipboardUpdate += Clipboard_ClipboardChanged;
            Console.WriteLine("Running");
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static void Clipboard_ClipboardChanged(object sender, EventArgs eventArgs)
        {
            if (DateTime.UtcNow.Subtract(_lastEvent).TotalSeconds < 1)
            {
                return;
            }

            if (Clipboard.ContainsImage())
            {
                Console.WriteLine("Clipboard image event occurred, capturing and rewriting...");
                var img = Clipboard.GetImage();
                if (img == null)
                {
                    return;
                }

                using var mem = new MemoryStream();
                img.Save(mem, ImageFormat.Bmp);
                mem.Seek(0, SeekOrigin.Begin);
                var image = Image.Load(mem);

                var streams = CompressImage(image);

                while (streams.Count == 0)
                {
                    Console.WriteLine("Too large - reducing resolution");
                    image.Mutate(context => context.Resize((int)(image.Width * 2 / 3.0), 0));
                    streams = CompressImage(image);
                }

                var memStream = streams.OrderByDescending(stream => stream.Length).First();

                Clipboard.SetImage(System.Drawing.Image.FromStream(memStream));
                Parallel.ForEach(streams, stream => stream.Dispose());
                Console.WriteLine("done!");
                _lastEvent = DateTime.UtcNow;
            }
        }

        private static ConcurrentBag<MemoryStream> CompressImage(Image image)
        {
            var streams = new ConcurrentBag<MemoryStream>();

            Parallel.For(0, 4, new ParallelOptions {MaxDegreeOfParallelism = 5}, compression =>
            {
                Console.WriteLine($"Running at compression level {compression}");
                var stream = new MemoryStream();
                image.SaveAsPng(stream, new PngEncoder {CompressionLevel = (PngCompressionLevel)compression});
                Console.WriteLine($"Compression level {compression} produced size {stream.Length}");
                if (stream.Length < EightMb)
                {
                    streams.Add(stream);
                }
                else
                {
                    stream.Dispose();
                }
            });

            return streams;
        }
    }
}
