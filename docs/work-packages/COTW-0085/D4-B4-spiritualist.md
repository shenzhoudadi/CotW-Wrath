# Spiritualist · D4 复查与 B4 修复

复查基线为 `017acea`，继续按 [C-0085-20261115-r1](C-spiritualist-contract.md) 验证。此前 253 条定向及完整伙伴链受 Unity ECall 限制的记录见 [B2/D3](B2-D3-spiritualist.md)。

- **C12 Bonded。** 原生 `TryStart` 将首轮扣减排到控制器后续 tick，同一 tick 前关闭可能跳过消耗。新接线只对本项成功启动的实例立即调用一次原生 `OnNewRound`，再延续原生 6 秒计时。关闭马上清 buff。两种触手在能力入口要求原生盾 buff，结算时又核对实际存活并收回的 Phantom。18 级从自身通过原生范围选敌，修复原先用玩家成员表找敌人。临时攻击和 Phantom 共用一套伤害骰及等级档，18 级继续用原生最佳掷骰效果。
- **C16 Zeal’s Resolve。** 实际 d20 返回给父攻击／豁免之前，使用现有判失败逻辑检查骰面。一次失败消耗一次本职资源，调用原生骰子产生第二个结果并采用它，保留掷骰历史。成功、预设结果、虚拟检定和重击确认不触发。共用 Speaker for the Past 的判定方法，原模块调用及行为相同。
- **C16 四个光环。** Aura of Fury、Aura of Despair、Hateful Aura、Determination Aura 由原生 `AbilityAreaEffectBuff` 管理来源、持续和撤销，替换零持续时间的进出范围脚本。Hateful Aura 以施法 Phantom 和原生主人为授权来源，在真正造成伤害后才反击。各来源互相独立。
- **C16 Hatred 和 Fear。** Hated Target 改为一项可持续标记，Phantom 在 7 级从移动切到迅捷。Shared Hatred 附在本次标记下，Sneak Attack 的原生精确伤害只在本 Phantom 标记的目标上增加。Increase Fear 用 Phantom 的 HD／CHA 计算 DC，成功豁免的 24 小时免疫按来源保存。离开光环只撤销当前来源的恐惧，Shelter Allies 由原生光环组件撤销。

## 本机验证

- 编译 **2.659 秒**，主体无警告，宿主两条既有警告，0 错误。定向宿主 **264/264，14.237 秒**，覆盖触手门控、范围与伤害档、原生 d20 结果替换、光环来源、Hatred 标记与 Fear 退出。编译中的原生 Dice 命名解析失败耗时 1.466 秒，修正后完成上述验证。
- 独立 Harmony 2.0.4.0 探针编译 0.115 秒、运行 0.414 秒，验证改动的首轮消耗与动作类型两处原生方法可成功安装补丁。
- 本地 TEST ZIP 23 条，打包与三个 DLL 回读 **0.670 秒**。主 DLL SHA256 `7E95907601C1E4B082A91B6D2E63C7A0EEFBA17C9BFA289AF6D18EC96BA975DE`，ZIP SHA256 `830D88763F927DCCBDA2C0ADF3144D11A827DECF5CA25273AFDC56F9AF56D310`。安装包上传未运行。
- 本轮未运行完整 AddPet、实际战斗及模式切换计时、重训、休息 UI、骑乘、真实存读档和实际 ExpandedContent 组合。继续按原生与玩家证据边界复查。
