# Spiritualist · D11 情绪技能与原生等级提交

复查基线为 `1a6b004`，覆盖 [C03、C17](C-spiritualist-cases.json)。[D10/B10](D10-B10-spiritualist.md)

- **普通技能分配**。原作从 AddClassLevels 的常规分配表排除两项由情绪自动成长的技能，并用 Druid 的技能点减一。Wrath 原生 Druid 为 3，Phantom 普通点数现为 2。六种情绪按各自技能对筛选原生分配列表，原生 AddClassSkill 仍负责职业技能标记。[原作固定提交](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Classes/Phantoms/Phantom.cs#L146)
- **情绪技能跟级**。原生 `UnitProgressionData.AddClassLevel` 增加等级后调用 `UnitDescriptor.OnGainClassLevel`，由后者发布按单位的 `IOwnerGainLevelHandler`。幻影自己的情绪事实在 TurnOn 和这个事件上补足本职业技能等级，移除对全局 AddClassLevels 方法的手工后置补丁。
- **实际托管推进**。在已绑定且物理阶段预置的 Anger 上，从原生 AddClassLevel 实走 1 至 2 级，并检查两项情绪技能各为 2，普通 Athletics 保持 1。另一只幻影的原生事件及同单位事件重放不额外加秩。保留 D10 对等价职业等级请求和同主人 Dog 的核对。测试使用安装档的原生 XP 表，通过夹具提供当前单位、场景身份和派对状态。

## 本机验证

- 最终主体与宿主编译 **1.775 秒**，主体无警告，宿主两条既有警告，0 错误。隔离定向 **375/375，14.513 秒**。较窄的技能事件版本先通过 **374/374，14.329 秒**，编译 **2.739 秒**。补入原生等级提交后的三次尝试分别在夹具缺少事实 ID、缺少 TimeController、未初始化原生派对列表的位置终止，分别耗时 **14.378、14.348、14.249 秒**，三次编译 **2.807、1.773、1.785 秒**。第二个尝试也检出旧共享夹具的 Druid 缺字段，最终按已审计原生记录回填。
- 原生 Harmony 2.0.4.0 核对当前全部 **10 组补丁、11 个原生方法，0.384 秒**。本地 TEST 包 23 条，打包和三个 DLL 回读 **0.677 秒**。主 DLL SHA256 `9FABA11249B93FB8E57D6AC407E9E3F1B7E38E8DC0D004C5BEC1187FA679BEF5`，ZIP SHA256 `095264DA8F50DA218E0E38956214E029171A660F214D610756756B1BBA051163`。安装包上传未运行。
- Unity 生成、完整 AddClassLevels 控制器与交互式加点、实际场景位置与战斗、实机存读档、休息和重训界面未运行。
