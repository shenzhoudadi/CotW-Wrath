# Spiritualist · D9 骑乘与重训隔离

复查基线为 `a01caca`，覆盖 [C09、C10、C17](C-spiritualist-cases.json) 的原生伙伴隔离。[D8/B8 记录](D8-B8-spiritualist.md)

- **骑乘按钮**。Wrath 的 `UnitPartPetMaster.UpdateMountAbility` 扫描合格伙伴后生成、移除或恢复按钮。原后置补丁另做了一次伙伴扫描，只有幻影时先添加再移除按钮。现在在原生扫描内让带私有标记及类型的 Phantom 使用已有的排除分支，按钮生命周期继续由原生管理。Dog 或无私有标记的同类型伙伴保持原生结果。
- **登骑**。`AbilityTargetIsSuitableMount.CanMount` 同时是原生选标和 `UnitPartRider.Mount` 普通执行的门槛。将拦截移至这一处，沿用原生主人归属、意识和变形检查。这沿用原生脚本显式强制登骑的例外。
- **重训清退**。`UnitHelper.RespecOnCommit` 先逐一卸装、解除主人和标记销毁，再关闭旧事实。还可解析的旧 Phantom 如果已脱离主人并标记销毁，私有 AddPet 不再重走清退或升级。活跃绑定的清退仍走原生 AddPet。同主人 Dog 的正常原生重训行为仍由游戏管理，未做额外保留或重建逻辑。

## 本机验证

- 主体和宿主编译 **2.754 秒**，主体无警告，宿主两条既有警告，0 错误。隔离定向 **357/357，14.157 秒**。对原生按钮扫描 IL 核对限定过滤，以原生 `CanMount` 验证合法 Dog、无私有标记的同类型伙伴、跨主人和私有幻影。以原生 `MarkForDestroy` 和宠物部件 `Clear` 验证清退前后的门控。
- 原生 Harmony 2.0.4.0 成功安装 11 组 0085 补丁，覆盖 12 个方法，**0.385 秒**。本地 TEST ZIP 23 条，打包与三个 DLL 回读 **0.685 秒**。主 DLL SHA256 `39B816CE04E812BE31552D6A92B3C1672D43A11693E76696E1D191B8FA5BF2B0`，ZIP SHA256 `CF5AF95EA8B67F2A06B2F162C54BE94049424EBFBB6E19703ADC56C0950ED2F7`。安装包上传未运行。
- 玩家重训界面、同主人 Dog 的重建及装备回收、骑乘按钮和实际登骑、完整 AddPet 链、实机复生及存读档未运行。隔离用例只调用相关原生判定及部件操作。
