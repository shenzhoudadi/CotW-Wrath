# A — COTW-0060 Spirit Guide 源码范围与映射

日期：2026-11-10；操作者：本会话Sol。只读资料与元数据，未实现、未编译、未执行游戏／UMM或运行用例。

## 来源

基线main `91825bcd021a0e7439a5d8541c3a631f0c9a6d5d`，已交付0.14.69 Combined TEST。原作固定 `Holic75/KingmakerRebalance@1332fb0db844b7863f484ca978bea2349fe49769`。已把本地Oracle.cs、Common.cs、ShamanHexes.cs及十族源码共13份逐一与GitHub固定commit的blob核对一致；指纹见A-source-index.json，机械采集耗时23.756秒。

核心位置：Oracle.cs:141–198的Spirit构造器、671–744的变体／选择器，Common.cs:239–285的顺序环级表构建。十族各自的createOracleSpirit才是本包入口，不能用createSpiritWhispererSpirit代替。

## 原作真实范围

- 3级固定选择一个Bonded Spirit，并从该族选一个巫术。4级起得到该族9项奖励法术，7级基础能力，15级greater。没有true或manifestation层，**Lore的15级是Arcane Enlightenment，不是Perfect Knowledge**。
- 变体移除1级mystery skills、3/7/15级revelation；保留原有Mystery、curse、mystery spells、1/11/19级revelation与20级Mystery终阶。原作没有每日换魂／换巫术能力；“as wandering spirit”是描述，不是已实现的休息切换UI。
- 十族字段都把primary_stat、secondary_stat赋成hex_engine.hex_stat，故魂域基础／高阶能力的主次均为Charisma。HexEngine本身构造为(Charisma, Wisdom)，**巫术里读取hex_secondary_stat的分支仍为Wisdom**。
- 10族×9环=90项法术表条目；77条Wrath原生身份、8条本项目已注册根、4条ExpandedContent条目、1条CharacterOptionsPlus条目。外部共4个唯一根（Wall of Fire被两族引用）。共39个巫术池条目；不是39种互不重复的巫术。
- Waves在Oracle入口使用hexes.Skip(1)：排除Beckoning Chill，保留Crashing Waves、Mist's Shroud、Fluid Magic。与0117的列表不同。Fluid Magic在这个Oracle入口为语义已满足的空marker：正常Oracle已用普通槽施放全部魂域已知法术；Shaman.cs中对Shaman对象补充的特殊槽逻辑不自动作用于这个独立Oracle对象。

## Oracle宿主身份（当前本机蓝图归档静态读取）

- OracleClass：`20ce9bf8af32bee4c8557a045ab499b1`。
- OracleProgression：`5dd9b4982c9ed0b4090d5a35c459b729`。
- OracleSpellbook：`6c03364712b415941a98f74522a81273`，主法表`8443ce803d2d31347897a3d85cc32f53`，Charisma施法；Wizard法表`ba0401fdeb4062f40a7aa95b6f07fe89`。
- OracleRevelationSelection：`60008a10ad7ad6543b1f63016741a5d2`，进展确为1/3/7/11/15/19。
- OracleMysterySelection：`5531b975dcdf0e24c98f1ff7e017e741`。
- Wrath Oracle基础技能已有Persuasion和全部四项Knowledge/Lore；mystery额外技能直接放在各Mystery Feature的AddClassSkill组件中，不存在可照搬原作的通用mystery_skills进展项。示例Battle Mystery `6b8e676943cb91648b21b7ca72fbb833`还给Perception；Flame `3b68909df737cd4458509d7f3a9c3706`给Mobility/Athletics。只加LoreNature而不处理这些来源不等于完成替换。
- 实际游戏程序集元数据：AddClassSkill.OnTurnOn调用ModifiableValueSkill.ClassSkillRetain；OnTurnOff调用ClassSkillRelease，没有来源参数。因此不能盲目减计数或跳过TurnOff造成下溢，必须成对处理。
- AddSpellKnownTemporary有m_CharacterClass、m_Spell、Level、OnlySpontaneous；UnitPartTemporarySpellsKnown有AddEntry(EntityFact, AddSpellKnownTemporary)、RemoveEntry同签名和按Entry删除、OnPostLoad。现有TemporaryKnowledge01464已对它做来源保护与同步；本包不得另写全局RemoveSpell兜底。

## 提供方与复用

- ExpandedContent固定源码`9c2534bafa17942938fac22d6a8fd0bb735da013`：WallOfFireAbility `80189142f7c640f39195defdc9777b27`；HypnoticPatternAbility `56b8f0304a704a67b3c35cbe8c774854`；IncendiaryCloudAbility `99bad8fca7534acdb86866940eef62da`。Config/Blueprints.json固定blob `927e471ecb80f367189a4bd870e83403eb4428ee`已与本地核对。
- CharacterOptionsPlus固定源码`0b5679348c94ccbeaa5b1f3e57460c2342ec6eb8`：Frostbite根 `67b29b99f13e40a48a1a95fbd909a298`，不是FrostbiteEffect。Util/Guids.cs固定blob `ee913bea8fd297ab09c8344326ae014672550ca7`已核对；配置入口明确受Settings.IsEnabled控制，存在GUID不等于运行可用。
- 本项目ControlUndead、FireSeeds、FluidForm、OverlandFlight、RiverOfWind、ScouringWinds、SleetStorm、TimeStopSpell01438为已有根，A-spirit-map.json保存实际注册源码位置及确定性GUID；不新建这些法术。Ice Body／Meteor Swarm／Winds of Vengeance直接复用当前Wrath原生法术身份，排除了同名武器附魔。
- 复用PsychicAssets.CopyData／现有缓存修复／原生临时已知来源账本。0117的目录与已修复效果可作对照，不能复用其Wizard身份／资源对象、Lore/manifestation或当前Heaven's Leap已知选标缺陷。0060只补Oracle profile，不能复制第二套完整SpiritsEngine。
- 已额外从当前游戏归档核对38个原生tier／hex候选身份；A-map的nativeCandidateMetadata和每族hexMapping列出全部39个Oracle池字段的候选或明确私有builder边界。这是身份和可复用范围证据，不是原生同名效果等价／运行PASS。

## 查重

已检查本库所有提交入口／全部open及closed PR／4分支，以及唯一fork sxl1216的3分支、fork独有提交和issue/PR入口。未见0060实现或认领，当前原生归档也无SpiritGuide路径。选定本地ExpandedContent、CharacterOptionsPlus源码范围未找到Spirit Guide入口；不是宣称整个生态不存在。

fork main当前`c7ce6050193d9a077fc0454839615894e016dc68`另有雾效展示、Sacred Servant领域修复，不在本包接管范围。Speaker issue #6保持原负责人修复。0060已登记issue #10，只有本项A/C开工。
