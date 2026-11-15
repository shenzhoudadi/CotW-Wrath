# COTW-0085 Spiritualist · 基础职业

状态为 **A/C/B 已完成，待 D 独立复查**。B 交付见 [B-summary](../COTW-0085/B-summary.md)，分支 `feat/cotw-0085-spiritualist`。基线 `939e04d543875ff28314b808dcc996d16227acc3` / 0.14.74 TEST。B 工作分支建议 `feat/cotw-0085-spiritualist`，先做 Anger 原生伙伴链。范围是 Wisdom 六环自发念力施法、Phantom、六种原作基础情绪，以及主人完整职业进度。COTW-0086–0091 变体沿用原状态。原作 `createPhantoms` 实际接通 Anger、Despair、Hatred、Fear、Zeal、Kindness，Whimsy 沿用原作未接通状态。 [施工认领 #16](https://github.com/shenzhoudadi/CotW-Wrath/issues/16)

- A 已交接。[原作与 Wrath 入口](../COTW-0085/A-spiritualist-map.md) · [蓝图、情绪与私人法表](../COTW-0085/A-spiritualist-assets.json)
- C 已冻结 `C-0085-20261115-r1`。[生命周期合同](../COTW-0085/C-spiritualist-contract.md) · [18 项待执行用例](../COTW-0085/C-spiritualist-cases.json)
- 本轮完成静态盘点与合同。原生伙伴链的实际运行由 B 执行，场景、实机存读档、重训界面仍待验证。Ghoul Touch 暂无确认等价根，Lesser Animate Dead 接线需先核对 PR #14。

每个角色开始先读本页与 `AGENTS.md`。阶段原件写入本机 `C:/Users/xuenc/agent-tools/spiritualist-next/`，保留来源路径、GUID 与失败原件。A 和 C 可读原作、当前仓库及本机合法游戏元数据，不提交第三方程序集或原生蓝图。B 不启动游戏或 UMM，不跑整套测试。D 以新增编译、离线注册和本职业定向检查为准，实机存读档／重训界面等未测边界如实列明。

## A · Phantom 供体、法表与原生调用

**Gemini 3.8 Flash。** 读原作 `Classes/Spiritualist.cs`、`Classes/Phantoms/Phantom.cs`、引用的 Companion Mechanics，以及主线 `Modules/MediumClass/Spiritualist/Phantom.cs`、`Modules/MediumClass/Main.cs`、`src/CompanionSpellSharing.cs`、`PsychicClass.cs`、`PsychicSharedSpellCatalog.cs`。输出 `A-spiritualist-map.md` 与 `A-spiritualist-assets.json`。列清六种情绪的等级、技能、自然攻击、能力与供体，核对 AddPet/UnitPartPet、召回、死亡与原作谱系的实际引用。对视觉供体、跨区、读档或只在场景可验的路径单独标记。Medium 中的 Phantom 代码供核对，现有模块入口未发布这个职业。 [原作职业](https://github.com/Holic75/KingmakerRebalance/tree/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Classes/Spiritualist.cs) · [原作 Phantom](https://github.com/Holic75/KingmakerRebalance/tree/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Classes/Phantoms/Phantom.cs)

## C · 冻结 Phantom 生命周期

**旗舰档。** 输出 `C-spiritualist-contract.md` 与 `C-spiritualist-cases.json`。定义主人与唯一伙伴身份、等级跟随、显现／收回／召回、死亡、恢复、区域切换、重训／移除和重复事件。指定可直接复用的 Wrath 原生入口和局部适配边界。冻结 Shared/Fused Consciousness、Bonded Manifestation、Dual Bond、Etheric Tether/Spiritual Bond 与伙伴状态的组合。写清真实调用与离线宿主能力，准备玩家 TEST 的场景脚本。验收包括一名主人只保留一只 Phantom、反复切换不重复授予、不误伤动物伙伴等其他 PetType、休息和重训后按合法状态恢复。

## B · 分包实现

- **S-B01 Phantom 运行时**。旗舰档实现主人和伙伴状态桥。先用一种原作基础情绪打通一次原生主人选项→伙伴建立→显现／收回→主人等级同步→移除的实链。该切片用于开发，作为六种情绪和显现分支共同依赖。预计在独立 `src/Spiritualist*0085.cs`、`src/Phantom*0085.cs`。
- **S-B02 基础职业与情绪目录**。一名日常执行档按合同接本职私有法表、职业进度和六种完整情绪，可按整种情绪在隔离目录并行。由旗舰负责 Etheric Tether、Phantom Recall、共享意识、Bonded Manifestation/Dual Bond 等与状态桥相连的执行代码。验收检查每种情绪的实际授予与成长，私有法表只读复用已有根，伙伴不存在或死亡时按合同阻止不合法操作，现有 Medium 和其他伙伴保留原入口。

## D · 独立验收

**更换一名旗舰档。** 输出 `D-spiritualist-review.md`。走新增编译、0085 离线注册及显现／收回／移除方案，检查两角色同场及动物伙伴负例。记录定向检查命令、DLL/依赖 SHA256、可重现的失败和仅能由玩家 TEST 覆盖的场景。完整六种情绪和主人进度齐备后再提整合。

### 可用模型分档

- **A 资料档**：Gemini 3.8 Flash，负责原文、供体、映射、清单，也可按冻结合同整理机械数据。
- **B 执行档**：Composer 2.5、Kimi K2.8，负责独立文件内的明确接线、必要用例与打包步骤。
- **C／难点 B／D 攻坚档**：Grok 4.7、Gemini 4 Argon、Gemini 3.1 Pro、GLM 5.3、Kimi K3.1 Pro 或 Sol。可任选，每项保持单一实现负责人，D 换人。共享机制、Harmony、生命周期和最终冲突处理留给这一档。
