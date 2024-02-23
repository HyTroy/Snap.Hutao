﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Control.Collection.AdvancedCollectionView;
using Snap.Hutao.Factory.ContentDialog;
using Snap.Hutao.Model.Calculable;
using Snap.Hutao.Model.Entity.Primitive;
using Snap.Hutao.Model.Intrinsic;
using Snap.Hutao.Model.Intrinsic.Frozen;
using Snap.Hutao.Model.Metadata;
using Snap.Hutao.Model.Metadata.Avatar;
using Snap.Hutao.Model.Metadata.Item;
using Snap.Hutao.Model.Primitive;
using Snap.Hutao.Service.Cultivation;
using Snap.Hutao.Service.Hutao;
using Snap.Hutao.Service.Metadata;
using Snap.Hutao.Service.Notification;
using Snap.Hutao.Service.User;
using Snap.Hutao.View.Dialog;
using Snap.Hutao.Web.Response;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CalculateAvatarPromotionDelta = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.AvatarPromotionDelta;
using CalculateClient = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.CalculateClient;
using CalculateConsumption = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.Consumption;
using CalculateItem = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.Item;
using CalculateItemHelper = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.ItemHelper;

namespace Snap.Hutao.ViewModel.Wiki;

/// <summary>
/// 角色资料视图模型
/// </summary>
[HighQuality]
[ConstructorGenerated]
[Injection(InjectAs.Scoped)]
internal sealed partial class WikiAvatarViewModel : Abstraction.ViewModel, IWikiViewModelInitialization
{
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly ICultivationService cultivationService;
    private readonly IMetadataService metadataService;
    private readonly ITaskContext taskContext;
    private readonly IHutaoSpiralAbyssStatisticsCache hutaoCache;
    private readonly IInfoBarService infoBarService;
    private readonly CalculateClient calculateClient;
    private readonly IUserService userService;

    private AdvancedCollectionView<Avatar>? avatars;
    private Avatar? selected;
    private ObservableCollection<string>? filterTokens;
    private string? filterToken;
    private BaseValueInfo? baseValueInfo;
    private Dictionary<Level, Dictionary<GrowCurveType, float>>? levelAvatarCurveMap;
    private List<Promote>? promotes;
    private FrozenSet<string> availableQueries;

    /// <summary>
    /// 角色列表
    /// </summary>
    public AdvancedCollectionView<Avatar>? Avatars { get => avatars; set => SetProperty(ref avatars, value); }

    /// <summary>
    /// 选中的角色
    /// </summary>
    public Avatar? Selected
    {
        get => selected; set
        {
            if (SetProperty(ref selected, value))
            {
                UpdateBaseValueInfo(value);
            }
        }
    }

    /// <summary>
    /// 基础数值信息
    /// </summary>
    public BaseValueInfo? BaseValueInfo { get => baseValueInfo; set => SetProperty(ref baseValueInfo, value); }

    /// <summary>
    /// 保存的筛选标志
    /// </summary>
    public ObservableCollection<string>? FilterTokens { get => filterTokens; set => SetProperty(ref filterTokens, value); }

    public string? FilterToken { get => filterToken; set => SetProperty(ref filterToken, value); }

    public FrozenSet<string>? AvailableQueries { get => availableQueries; }

    public void Initialize(ITokenizingTextBoxAccessor accessor)
    {
        accessor.TokenizingTextBox.TextChanged += OnFilterSuggestionRequested;
        accessor.TokenizingTextBox.QuerySubmitted += OnQuerySubmitted;
        accessor.TokenizingTextBox.TokenItemAdded += OnTokenItemModified;
        accessor.TokenizingTextBox.TokenItemRemoved += OnTokenItemModified;
    }

    protected override async ValueTask<bool> InitializeUIAsync()
    {
        if (!await metadataService.InitializeAsync().ConfigureAwait(false))
        {
            return false;
        }

        levelAvatarCurveMap = await metadataService.GetLevelToAvatarCurveMapAsync().ConfigureAwait(false);
        promotes = await metadataService.GetAvatarPromoteListAsync().ConfigureAwait(false);

        Dictionary<MaterialId, Material> idMaterialMap = await metadataService.GetIdToMaterialMapAsync().ConfigureAwait(false);
        List<Avatar> avatars = await metadataService.GetAvatarListAsync().ConfigureAwait(false);
        IOrderedEnumerable<Avatar> sorted = avatars
            .OrderByDescending(avatar => avatar.BeginTime)
            .ThenByDescending(avatar => avatar.Sort);
        List<Avatar> list = [.. sorted];

        await CombineComplexDataAsync(list, idMaterialMap).ConfigureAwait(false);

        await taskContext.SwitchToMainThreadAsync();
        Avatars = new(list, true);
        Selected = Avatars.View.ElementAtOrDefault(0);
        FilterTokens = [];

        availableQueries = FrozenSet.ToFrozenSet<string>(
            [
                .. avatars.Select(a => a.Name),
                .. IntrinsicFrozen.AssociationTypes,
                .. IntrinsicFrozen.BodyTypes,
                .. IntrinsicFrozen.ElementNames,
                .. IntrinsicFrozen.ItemQualities,
                .. IntrinsicFrozen.WeaponTypes,
            ]);

        return true;
    }

    private async ValueTask CombineComplexDataAsync(List<Avatar> avatars, Dictionary<MaterialId, Material> idMaterialMap)
    {
        if (!await hutaoCache.InitializeForWikiAvatarViewAsync().ConfigureAwait(false))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(hutaoCache.AvatarCollocations);

        foreach (Avatar avatar in avatars)
        {
            avatar.Collocation = hutaoCache.AvatarCollocations.GetValueOrDefault(avatar.Id);
            avatar.CookBonusView ??= CookBonusView.Create(avatar.FetterInfo.CookBonus, idMaterialMap);
            avatar.CultivationItemsView ??= avatar.CultivationItems.SelectList(i => idMaterialMap.GetValueOrDefault(i, Material.Default));
        }
    }

    [Command("CultivateCommand")]
    private async Task CultivateAsync(Avatar? avatar)
    {
        if (avatar is null)
        {
            return;
        }

        if (userService.Current is null)
        {
            infoBarService.Warning(SH.MustSelectUserAndUid);
            return;
        }

        CalculableOptions options = new(avatar.ToCalculable(), null);
        CultivatePromotionDeltaDialog dialog = await contentDialogFactory.CreateInstanceAsync<CultivatePromotionDeltaDialog>(options).ConfigureAwait(false);
        (bool isOk, CalculateAvatarPromotionDelta delta) = await dialog.GetPromotionDeltaAsync().ConfigureAwait(false);

        if (!isOk)
        {
            return;
        }

        Response<CalculateConsumption> consumptionResponse = await calculateClient
            .ComputeAsync(userService.Current.Entity, delta)
            .ConfigureAwait(false);

        if (!consumptionResponse.IsOk())
        {
            return;
        }

        CalculateConsumption consumption = consumptionResponse.Data;
        LevelInformation levelInformation = LevelInformation.From(delta);
        List<CalculateItem> items = CalculateItemHelper.Merge(consumption.AvatarConsume, consumption.AvatarSkillConsume);
        try
        {
            bool saved = await cultivationService
                .SaveConsumptionAsync(CultivateType.AvatarAndSkill, avatar.Id, items, levelInformation)
                .ConfigureAwait(false);

            if (saved)
            {
                infoBarService.Success(SH.ViewModelCultivationEntryAddSuccess);
            }
            else
            {
                infoBarService.Warning(SH.ViewModelCultivationEntryAddWarning);
            }
        }
        catch (Core.ExceptionService.UserdataCorruptedException ex)
        {
            infoBarService.Error(ex, SH.ViewModelCultivationAddWarning);
        }
    }

    private void UpdateBaseValueInfo(Avatar? avatar)
    {
        if (avatar is null)
        {
            BaseValueInfo = null;
            return;
        }

        ArgumentNullException.ThrowIfNull(promotes);
        Dictionary<PromoteLevel, Promote> avatarPromoteMap = promotes.Where(p => p.Id == avatar.PromoteId).ToDictionary(p => p.Level);
        Dictionary<FightProperty, GrowCurveType> avatarGrowCurve = avatar.GrowCurves.ToDictionary(g => g.Type, g => g.Value);
        FightProperty promoteProperty = avatarPromoteMap[0].AddProperties.Last().Type;

        List<PropertyCurveValue> propertyCurveValues =
        [
            new(FightProperty.FIGHT_PROP_BASE_HP, avatarGrowCurve, avatar.BaseValue),
            new(FightProperty.FIGHT_PROP_BASE_ATTACK, avatarGrowCurve, avatar.BaseValue),
            new(FightProperty.FIGHT_PROP_BASE_DEFENSE, avatarGrowCurve, avatar.BaseValue),
            new(promoteProperty, avatarGrowCurve, avatar.BaseValue),
        ];

        ArgumentNullException.ThrowIfNull(levelAvatarCurveMap);
        BaseValueInfo = new(avatar.MaxLevel, propertyCurveValues, levelAvatarCurveMap, avatarPromoteMap);
    }

    private void OnFilterSuggestionRequested(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (Avatars is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(FilterToken))
        {
            return;
        }

        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            sender.ItemsSource = availableQueries.Where(q => q.Contains(FilterToken, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is not null)
        {
            return;
        }

        ApplyFilter();
    }

    private void OnTokenItemModified(TokenizingTextBox sender, object args)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (Avatars is null)
        {
            return;
        }

        if (FilterTokens.IsNullOrEmpty())
        {
            Avatars.Filter = default!;
            return;
        }

        Avatars.Filter = AvatarFilter.Compile(string.Join(' ', FilterTokens));

        if (Selected is not null && Avatars.Contains(Selected))
        {
            return;
        }

        try
        {
            Avatars.MoveCurrentToFirst();
        }
        catch (COMException)
        {
        }
    }
}