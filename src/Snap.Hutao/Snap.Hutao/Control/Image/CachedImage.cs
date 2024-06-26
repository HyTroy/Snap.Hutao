﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Control.Extension;
using Snap.Hutao.Core.Caching;
using Snap.Hutao.Core.ExceptionService;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Control.Image;

/// <summary>
/// 缓存图像
/// </summary>
[HighQuality]
internal sealed class CachedImage : Implementation.ImageEx
{
    /// <summary>
    /// 构造一个新的缓存图像
    /// </summary>
    public CachedImage()
    {
        DefaultStyleKey = typeof(CachedImage);
        DefaultStyleResourceUri = "ms-appx:///Control/Image/CachedImage.xaml".ToUri();
    }

    /// <inheritdoc/>
    protected override async Task<Uri?> ProvideCachedResourceAsync(Uri imageUri, CancellationToken token)
    {
        IImageCache imageCache = this.ServiceProvider().GetRequiredService<IImageCache>();

        try
        {
            HutaoException.ThrowIf(string.IsNullOrEmpty(imageUri.Host), SH.ControlImageCachedImageInvalidResourceUri);
            string file = await imageCache.GetFileFromCacheAsync(imageUri).ConfigureAwait(true); // BitmapImage need to be created by main thread.
            token.ThrowIfCancellationRequested(); // check token state to determine whether the operation should be canceled.
            return file.ToUri();
        }
        catch (COMException)
        {
            // The image is corrupted, remove it.
            imageCache.Remove(imageUri);
            return default;
        }
    }
}
