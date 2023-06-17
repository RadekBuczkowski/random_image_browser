namespace Random_Image.Classes.Cache;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Media.Imaging;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Utilities;
using Random_Image.Resources;

/// <summary>
/// Implements a class for caching and loading image bitmaps and for handling image file paths. The class
/// is thread-safe and uses separate threads to process each image for maximum performance and seamless experience.
/// </summary>
public class ImageCache
{
    /// <summary>
    /// Image file paths available to load (but not necessarily present in the bitmap cache), grouped and randomized.
    /// </summary>
    public ImageCacheFiles Files { get; }

    /// <summary>
    /// Thread-safe bitmap cache. The content of the dictionary is also thread-safe.
    /// </summary>
    private ConcurrentDictionary<string, CacheItem> Cache { get; } = new();

    /// <summary>
    /// Thread-safe incremental identity specifying how old an image in the cache is.
    /// </summary>
    private long _concurrentIdentity = 0;

    /// <summary>
    /// Thread-safe counter of images received from the cache. Used to trigger automatic restarts of the application.
    /// </summary>
    private int _cacheReceivedCount = 0;

    /// <summary>
    /// Thread-safe counter of images removed from the cache. Used to trigger the garbage collector run.
    /// </summary>
    private int _garbageCollectCount = 0;

    /// <summary>
    /// Creates a new cache with the given collection of image file paths. Use the <see cref="ImageCacheFiles.Scan"/>
    /// method to create the collection.
    /// </summary>
    public ImageCache(IEnumerable<string> files, bool groupImages)
    {
        Files = new ImageCacheFiles(files, groupImages);
    }

    /// <summary>
    /// Restores the image cache from a serialized temporary JSON file.
    /// </summary>
    public ImageCache(string tempFileName)
    {
        Files = ImageCacheFiles.DeSerializeFromTempFile(tempFileName);
    }

    /// <summary>
    /// Loads the bitmap of the specified image file either from the memory cache or from the file storage.
    /// The call is blocking, i.e. the current thread is suspended until the result is ready.
    /// </summary>
    public ImageCacheBitmap LoadImage(string filePath)
    {
        CacheItem result = LoadFromCache(filePath);
        result.Ready.WaitOne();  // Block until the image is ready.
        return result.GetImageCacheBitmap();
    }

    /// <summary>
    /// Loads the bitmap of the specified image file either from the memory cache or from the file storage.
    /// The specified <paramref name="receive"/> action gets invoked with the bitmap when ready.
    /// The call is non-blocking and the function returns immediately.
    /// </summary>
    /// <returns><see langword="true"/> if the bitmap is in the cache and was returned synchronously
    /// (the action got already invoked), or <see langword="false"/> if the bitmap is missing and will be returned
    /// asynchronously.</returns>
    public bool LoadImage(string filePath, Action<ImageCacheBitmap> receive)
    {
        CacheItem result = LoadFromCache(filePath, receive);
        return result.Ready.WaitOne(0);  // A non-blocking check if the image is already available.
    }

    /// <summary>
    /// Represents one image bitmap in the cache. The class is thread-safe.
    /// </summary>
    private class CacheItem
    {
        private long _identity;
        private ManualResetEvent _ready;
        private BitmapSource _bitmap;
        private int _width;
        private int _height;
        private long _size;
        private long _time;
        private bool _isAnimated;
        private Exception _exception;

        /// <summary>
        /// Incremental identity specifying how old the image in the cache is. Getting an image from the cache again
        /// will always update the identity.
        /// </summary>
        public long Identity
        {
            get => Volatile.Read(ref _identity);
            set => Volatile.Write(ref _identity, value);
        }

        /// <summary>
        /// Synchronization event set when the Bitmap property is ready. Can be used to suspend the current thread
        /// until the image is loaded.
        /// </summary>
        public ManualResetEvent Ready
        {
            get => Volatile.Read(ref _ready);
            init => Volatile.Write(ref _ready, value);
        }

        /// <summary>
        /// The image bitmap or <see langword="null"/> if the <see cref="Ready"/> event is still reset.
        /// </summary>
        public BitmapSource Bitmap
        {
            get => Volatile.Read(ref _bitmap);
            set
            {
                Volatile.Write(ref _bitmap, value);
                FilePixelWidth = 0;
                FilePixelHeight = 0;
            }
        }

        /// <summary>
        /// Contains a possible exception object if bitmap loading failed.
        /// </summary>
        public Exception LoadingException
        {
            get => Volatile.Read(ref _exception);
            set => Volatile.Write(ref _exception, value);
        }

        /// <summary>
        /// Original width of the bitmap in pixels. Can be different from <see cref="Bitmap.PixelWidth"/>
        /// if the bitmap was scaled down when loading. 0 means the same as <see cref="Bitmap.PixelWidth"/>.
        /// </summary>
        public int FilePixelWidth
        {
            get => Volatile.Read(ref _width);
            set => Volatile.Write(ref _width, value);
        }

        /// <summary>
        /// Original height of the bitmap in pixels. Can be different from <see cref="Bitmap.PixelHeight"/>
        /// if the bitmap was scaled down when loading. 0 means the same as <see cref="Bitmap.PixelHeight"/>.
        /// </summary>
        public int FilePixelHeight
        {
            get => Volatile.Read(ref _height);
            set => Volatile.Write(ref _height, value);
        }

        /// <summary>
        /// The size of the image file in bytes.
        /// </summary>
        public long FileSize
        {
            get => Volatile.Read(ref _size);
            set => Volatile.Write(ref _size, value);
        }

        /// <summary>
        /// The time of the last write to the image file as <see cref="DateTime.Ticks"/>.
        /// </summary>
        public long FileTime
        {
            get => Volatile.Read(ref _time);
            set => Volatile.Write(ref _time, value);
        }

        /// <summary>
        /// <see langword="true"/> if the image is animated, i.e. it has more than one bitmap frame
        /// (only GIF images support this).
        /// </summary>
        public bool IsAnimated
        {
            get => Volatile.Read(ref _isAnimated);
            set => Volatile.Write(ref _isAnimated, value);
        }

        public CacheItem(long identity)
        {
            Identity = identity;
            Ready = new ManualResetEvent(false);
            Bitmap = null;
            LoadingException = null;
            FileSize = -1;
            FileTime = -1;
            IsAnimated = false;
        }

        public ImageCacheBitmap GetImageCacheBitmap(bool isAsynchronous = false)
        {
            return new(Bitmap, LoadingException)
            {
                FilePixelWidth = FilePixelWidth,
                FilePixelHeight = FilePixelHeight,
                FileSize = FileSize,
                FileTime = FileTime,
                IsAnimated = IsAnimated,
                IsAsynchronous = isAsynchronous
            };
        }
    }

    /// <summary>
    /// Returns a new value for the incremental identity which specifies how old a cache item is. Note: we ignore
    /// the possibility of an overflow because loading 2^63 images is unrealistic. The method is thread-safe.
    /// </summary>
    private long GetNewIdentity() => Interlocked.Increment(ref _concurrentIdentity);

    /// <summary>
    /// Returns the image bitmap of the specified file from the cache, loading it into the cache if necessary.
    /// The call is non-blocking and starts a new thread if the image is not in the cache. The method is thread-safe.
    /// </summary>
    private CacheItem LoadFromCache(string filePath, Action<ImageCacheBitmap> receive = null)
    {
        if (Cache.TryGetValue(filePath, out CacheItem result))
        {
            // Ihe image exists in the cache. Stay in the current thread and mark the image with a new identity
            // preventing it from expiring in the cache.
            result.Identity = GetNewIdentity();
            // Quick non-blocking check if the image content is also available. If not available, it is going be sent
            // to the UI thread asynchronously from another thread that is already running.
            if (receive != null && result.Ready.WaitOne(0))
                receive(result.GetImageCacheBitmap());
        }
        else
        {
            // If the image is not in the cache, load the image file in a new thread.
            result = new(GetNewIdentity());
            if (Cache.TryAdd(filePath, result))
                NewThread.Invoke(() => LoadFromFile(filePath, result, receive), LIFO: true);
            else
                Cache.TryGetValue(filePath, out result);
        }
        return result;
    }

    /// <summary>
    /// Loads the bitmap of the specified image file into the cache and returns it. The method is thread-safe.
    /// </summary>
    private void LoadFromFile(string filePath, CacheItem result, Action<ImageCacheBitmap> receive)
    {
        try
        {
            (result.Bitmap, result.FilePixelWidth, result.FilePixelHeight) =
                ResourceHelper.LoadBitmapWithLimit(filePath, ImageBrowserState.MaxImagePixelDimension);
        }
        catch
        {
            try
            {
                result.Bitmap = ResourceHelper.LoadBitmap(filePath, troubleShooting: true);
            }
            catch (Exception ex) { result.LoadingException = ex.Fix(Text.FailedToLoadImage); }
        }
        try
        {
            (result.FileSize, result.FileTime) = PathHelper.GetFileSizeAndTime(filePath);
            result.IsAnimated = ResourceHelper.IsAnimatedGif(filePath);
        }
        catch { result.IsAnimated = false; }
        result.Ready.Set();  // Signal the image data is ready in the cache.
        if (receive != null)
            MainThread.Invoke(() => receive(result.GetImageCacheBitmap(true)));  // Deliver it back to the UI thread.
        ExpireOldestItems();
    }

    /// <summary>
    /// Removes oldest items from the cache if necessary when adding new items. The method is thread-safe.
    /// </summary>
    private void ExpireOldestItems()
    {
        while (Cache.Count > ImageBrowserConstants.MaxLoadedImageBitmaps)
        {
            string firstKey = string.Empty;
            long firstIdentity = long.MaxValue;
            foreach ((string key, CacheItem value) in Cache)  // Thread-safe iteration finding the oldest item.
            {
                long identity = value.Identity;
                if (firstIdentity > identity)
                {
                    firstIdentity = identity;
                    firstKey = key;
                }
            }
            if (Cache.TryRemove(firstKey, out CacheItem item))
            {
                // If the identity has changed, it means the item has just been accessed and should not expire.
                if (firstIdentity != item.Identity)
                    Cache.TryAdd(firstKey, item);  // Undo remove and try again.
                else
                    RecycleItems(1);
            }
        }
    }

    /// <summary>
    /// Frees unused memory if the total count of removed images reaches the threshold. The method is thread-safe.
    /// </summary>
    private void RecycleItems(int count, int threshold = 1000)
    {
        int value = Interlocked.Add(ref _garbageCollectCount, count);
        if (value >= threshold && Interlocked.CompareExchange(ref _garbageCollectCount, 0, value) == value)
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
    }

    /// <summary>
    /// Marks the count of images received from the cache. Used to trigger automatic restarts of the application.
    /// </summary>
    public void AcknowledgeReceived()
    {
        if (Volatile.Read(ref _cacheReceivedCount) >= 0)
            Interlocked.Increment(ref _cacheReceivedCount);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the number of images loaded from the cache has exceeded the threshold
    /// and the application needs to restart to prevent the problem of stuttering animations
    /// (threshold equals <see cref="ImageBrowserState.AutoRestartEveryNImages"/>).
    /// </summary>
    public bool NeedsRestart(int threshold, int extend_threshold = 0)
    {
        int value = Volatile.Read(ref _cacheReceivedCount);
        if (threshold <= 0 || value <= 0 || value < threshold + extend_threshold)
            return false;
        Volatile.Write(ref _cacheReceivedCount, -1); // Prevent from restarting more than once per application run.
        return true;
    }
}