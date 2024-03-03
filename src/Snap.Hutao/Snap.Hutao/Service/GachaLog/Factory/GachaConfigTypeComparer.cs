﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Web.Hoyolab.Hk4e.Event.GachaInfo;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Service.GachaLog.Factory;

/// <summary>
/// 祈愿配置类型比较器
/// </summary>
internal sealed class GachaConfigTypeComparer : IComparer<GachaType>
{
    private static readonly Lazy<GachaConfigTypeComparer> LazyShared = new(() => new());
    private static readonly FrozenDictionary<GachaConfigType, int> OrderMap = FrozenDictionary.ToFrozenDictionary(
    [
        KeyValuePair.Create(GachaConfigType.AvatarEventWish, 0),
        KeyValuePair.Create(GachaConfigType.AvatarEventWish2, 1),
        KeyValuePair.Create(GachaConfigType.WeaponEventWish, 2),
        KeyValuePair.Create(GachaConfigType.StandardWish, 3),
        KeyValuePair.Create(GachaConfigType.NoviceWish, 4),
    ]);

    /// <summary>
    /// 共享的比较器
    /// </summary>
    public static GachaConfigTypeComparer Shared { get => LazyShared.Value; }

    /// <inheritdoc/>
    public int Compare(GachaType x, GachaType y)
    {
        return OrderOf(x) - OrderOf(y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int OrderOf(GachaType type)
    {
        return OrderMap.GetValueOrDefault(type, 0);
    }
}