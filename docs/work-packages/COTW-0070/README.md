# COTW-0070 Cryptid Scholar 交接

Homebrew-Archetypes Investigator 的职业变体。基线 `1c0f3b027d263e195d6e1ca2946d4a4a3f68aad6` / 0.14.72 TEST，冻结合同 `C-0070-20261112-r1`。A/C/B 已完成，D 待独立复查。[施工 issue #13](https://github.com/shenzhoudadi/CotW-Wrath/issues/13)

- **A 原作与依赖**。固定原作、31 个 HA 蓝图、14 个原生天敌供体和 10 个原生入口。已定位实际的灵感扣费、识怪检定、AC、豁免和精准伤害路径。[来源核对](A-source-and-compat.md) · [身份与指纹](A-evidence.json)
- **C 行为冻结**。完整职业替换、识怪凭据、怪物分类、24 小时与付费重用、团队 Advice、一次性 Strike、天赋与存档边界。[合同](C-frozen-contract.md) · [定向预期](C-cases.json)
- **B 实现**。已完成（含 D 退回修复轮）：职业接入、识怪与 Advice、Strike 与清退、生产注册与共享入口；D-F01…F04 已修复。隔离定向 236 条断言 PASS（原 214 + D 验收 22）。[B prompt](B-prompt.txt) · [B 证据摘要](B-summary.md)
- **D 复查**。另一会话独立核对提交、正反例、共享机制复用和未测边界。缺陷退原 B。[D prompt](D-prompt.txt)

## 实施顺序

1. **B1 提供方与职业入口**。复用 HA 识别、变体注册和 Investigator 天赋来源检查。先对照 HA 真实进展移除全部受替换授予，再接 Intuitive Lore。风险是重复 Studied Combat、误伤普通 Investigator 或 Psychic Detective。
2. **B2 识怪与 Opportune Advice**。按实际类型凭据、施放者、游戏时间和真实灵感资源完成整条 Move 能力链。先验证拒绝与失败无副作用，再验证团队加值。它为 B3 提供 Advice 的实际来源和批次。
3. **B3 Knowledgeable Strike 与清退**。消费 B2 的同源 Advice，接原生实际命中与精准伤害。验证跨施放者、多个伤害包、隐蔽／距离／免疫和重训边界。
4. **D 复查及 TEST 交付**。先看受影响路径，复用有效的同内容证据。TEST 交付完成后更新状态并关闭 #13。

建议新生产文件 `src/CryptidScholar0070.cs` 与 `src/CryptidScholarRuntime0070.cs`，注册接入 `BlueprintRegistration.cs` 和 `ModuleCatalog.cs`。按需调用现有来源检查；新测试集中在一个本项定向入口。A/C 阶段只交文档和证据，编译、功能检查、打包和实机未运行。
