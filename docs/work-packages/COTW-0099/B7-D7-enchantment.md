# COTW-0099 B7 / D7 Enchantment 与职业选择

Enchantment 的基础力 Cloud Mind、共鸣 Glorious Presence，以及 Binding Pattern、Inspired Assault、Obey 已接入隔离职业图。Mental Focus 沿用同一套学校投资、锁定与支付入口。Wrath 原生职业进度承载七次法器选择、1–19 奇数级的十次 Focus Power 选择，以及 2 级起的 Magic Item Skill。

## 复用

- Cloud Mind 通过原生 Hit Dice 条件分流，复用 DazeBuff 和 Staggered。Binding Pattern 同时施加 Staggered 和 HoldMonsterBuff，后者原生每轮重投 Will；成功时仅撤掉麻痹。两项能力沿用原作执行图的 Medium 射程。
- Inspired Assault 取 Heroism 图标，用原生属性修正分别更新命中、伤害与三项豁免。Obey 的三项私有效果沿用 Command 的 Halt、Fall、Flee 原生动作和 buff，通过已有的条件 DC 入口应用同族 +2。原生 Command 的四个分支和父引用保持原样。
- Glorious Presence 与 Court Bard 共用实际写入 Charisma 检定的组件，指定 Competence。Magic Item Skill 使用 Wrath 的属性修正和升级刷新，2→4 级时实际从 +1 变为 +2。
- 术表沿用 Enchantment 的 Wrath 根和 CotW Mass Daze。ExpandedContent 的两种 Inflict Pain 供体保留在共享目录，实际程序集未加载。

## D7 修复与定向验证

原生法器撤销会一起撤掉基础能力、额外能力、共鸣和依赖法术。重新取得法器后，原选择的额外能力和 Extra Spells 槽恢复，已撤销的术法须重新学习。Inspired Assault 在有效等级 24 时仍封顶 +4，持续 24 分钟。隔离登记要求麻痹供体带有每轮 Will 重掷的原生组件，同 GUID 的空壳会被拒绝。

最终编译 0 警告、0 错误，用时 1.171 秒。原生定向 290 条断言、0 失败、0 宿主异常，用时 3.915 秒，登记 100 个私有身份。检查使用托管的真实选择、施法与效果路径，无画面的宿主提供特效边界和豁免结果，检查保留原生 DC、动作、持续时间和来源账本。魅力检定检查直接调用事实组件并读出原生规则修正，精通上限检查写入有效的账本状态。独立 D7 按此范围复查通过。

`SlicePublished=false`。Abjuration 的其余能力、另外六个学派、四个 Panoply、剩余职业特性、正式精通选择及普通启动登记仍待实现。实际提供方、完整升级和重训界面、磁盘存读档、游戏/UMM、模型补充、历史测试、打包和上传未运行。 [机器可读记录](B7-D7-evidence.json) · [认领 #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15)
