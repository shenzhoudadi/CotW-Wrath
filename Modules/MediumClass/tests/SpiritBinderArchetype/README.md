# 缚灵者变体：安装产物结构审计

```text
dotnet run --project tests/SpiritBinderArchetype/SpiritBinderArchetype.csproj -- ^
  artifacts/Release/MediumClass/MediumClass.dll ^
  "D:/steam/steamapps/common/PathFinder Second Adventure/Wrath_Data/Managed/Assembly-CSharp.dll" ^
  Strings/Feats.json
```

第一个参数是 ILRepack 之后的发布 DLL，第二个参数是已安装游戏的程序集，第三个参数可选，默认 `Strings/Feats.json`。
本套件链接生产 `Utilities/Guids.cs`，因此 GUID 断言读取的是真正的常量而非副本。

40 项检查分四组，全部用 Mono.Cecil 静态读取 IL，不启动 Unity、不加载存档、不执行游戏逻辑：

- **原生契约**（读游戏程序集）：`BlueprintFeatureSelection` 继承 `BlueprintFeature`，`m_AllFeatures` 是 `Kingmaker.Blueprints.BlueprintFeatureReference[]`；`BlueprintArchetype.AddFeatures/RemoveFeatures` 是 `LevelEntry[]`；`LevelEntry.m_Features` 是 `List<BlueprintFeatureBaseReference>`。这是"两个原生专长池嵌套进一个选择"在类型上成立的前提。
- **BlueprintCore 契约**（读发布 DLL 中已合并的 BlueprintCore）：`ArchetypeConfigurator.New` 保留父职阶，`OnConfigureCompleted` 调用 `AddToArchetypes` 把变体发布到通灵者职阶上。
- **安装产物**：四个 GUID 字面量；四个本地化 key 同时出现在 IL 与 `Strings/Feats.json` 中；`FeatureConfigurator.New` / `FeatureSelectionConfigurator.New` / `ArchetypeConfigurator.New` 各一次；`SetAddFeatures` 一次、`SetRemoveFeatures` 零次；两个原生池各引用一次、`Cast` 两次、`AddToAllFeatures` 一次；`AddEntry` 的等级序列恰为 1,2,3,5,7,9,11。
- **规则接线**：`Cap` 与 `PenaltyApplies` 各读取一次 `SpiritBinder.HasFeature`；`RefreshPenalty` 走 `PenaltyApplies`；`PenaltyApplies` 仍要求 `HasSpirit` 且仍对普通通灵者检查 `Amount` 阈值；`ContextActionApplySpirit.RunAction` 两个分支都刷新惩罚（共鸣扣费发生在动作之前，首次降灵必须当场生效）；`UnitPartMedium` 不再残留第二份阈值副本 `IsInfluencePenalty`；`Main.BlueprintCacheInitHandler.ConfigureClasses` 中通灵者职阶先于变体创建。

等级常量由局部函数 `LevelAt` 从 `AddEntry` 调用点回溯：先找参数数组的 `newarr`，再向前取第一个整型常量（`Ldc_I4_0..8` / `Ldc_I4_S` / `Ldc_I4`）。这样断言的是 IL 中真实的等级，而不是数调用次数。

`SetRemoveFeatures` 为零是有意的：缚灵者在桌面规则中取代的通灵者能力本模组并未实现，没有东西可移除。

嵌套而非拷贝的依据是核对过的原生数据：游戏目录下 `blueprints.zip` 的 252,498 个蓝图转储中，416 个选择蓝图里有 52 个嵌套了其它选择，其中 `LoremasterSecretSelection` 一次嵌套 4 个。嵌套的好处是其它 mod 追加进 `FeatureSelectionRefs.FighterFeatSelection`（155 项）或 `WizardFeatSelection`（26 项）的专长会自动出现在缚灵者的合并池里。

行为层面的验证在 `tests/InfluenceScenarios`（上限 +1、常驻惩罚、与坚毅灵魂/神话上限叠加、反向对照普通通灵者）。本套件只回答"发布 DLL 里确实是这么装的"。
