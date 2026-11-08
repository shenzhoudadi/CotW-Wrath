# Esoteric Starseeker — 0.14.68 TEST交付总结

完整F-0081已完成开发、D-F01修复与独立D复查；本项隔离托管宿主536条断言通过，48.36秒。不是玩家游戏验收，也不代表完整存档/洗点/命令队列/Unity渲染通过。源码和TEST包随0.14.68预发布交付。

## 内容

- 25个私有蓝图；实际十星座为LanternBearer、Newlyweds、Daughter、Rider、Patriarch、Wagon、Pack、Mother、Stranger、Follower；90个法术条目，73原生、12本项目、5外部能力。
- 每个当前可施法环级按激活星座临时授予；11级可维持双星座；第三次有合法次数时FIFO替换最旧。移除/替换不退款，休息清理并恢复每日次数；永久与多来源已知保留。
- 只对本变体将Major延至15级，普通Psychic仍11级；修复低于15级的独立MajorId残留，原始Major/Mimic前置有回归断言。
- 90项全部就绪才原子注册，缺一项不发布半个变体；外部能力通过既有提供方身份合同，晚到可重试。

## 必需提供方与人工检查

5项外部能力：ExpandedContent的HypnoticPatternAbility和ParticulateFormAbility；TomeOfTheFirebird的MassFlySpell；CharacterOptionsPlus的InvisibilityPurge；PrestigePlus的NewSpell.UseAkashicForm。游戏/本地提供方不随仓库分发。缺依赖时变体不出现属于预期，不以缺项星座替代。

图标引用已检查，但具体美术/实机渲染未验；完整存档/洗点UI、真实命令取消、实际升级SpellsKnown槽位等交人工TEST。保留旧包和测试档，不启动游戏或UMM作为自动流程。

## 源码和证据边界

本轮B源差异11文件，加版本戳csproj/manifest；旧README/Medium维护说明及仅行尾的build.ps1不回灌。定向检查原候选DLL为129aa56c5e1a4dbf95ee3ed519fcfd8ca14f7c38599dcecbd0144c229149efb7；版本更新不改玩法源码，不因出包重跑同内容专项。当前包主DLL见release的SHA256SUMS/说明。

tests/Starseeker0081Tests.cs保留当前有用用例；tests/Run-Starseeker0081.py仅为原本地合法隔离宿主的调用样例，绝对路径和本地夹具需核对，独立clone不是完整测试环境。未上传游戏数据、host二进制、私人日志或22次失败尝试原件；它们在本机证据目录保留，不重建全历史。

同目录合同部分星座名和接口标注不准确：以本页真实十星座/生产Catalog、实际IOwnerGainLevelHandler及D复查为准。D确认这一适配，没有据错文档替换实际能力。D-F01红绿、原始先决、升级事件/OnPostLoad均已核对；直接OnActivate不是完整fact restart/存档实测。

0.14.67基线Definition重载在本轮两个执行站点改为显式参数绑定；其余未执行站点不宣称已检查，不扩大为全量门禁。下一项施工认领见根WORK_STATUS.md。
