# COTW-0109 Iroran Paladin 交接

原生圣武士变体。A 与 C 已由本会话亲自完成，合同 `C-0109-20261111-r1`，B 可以开工。生产基线为 `a3f534625643b94cfa644e19c8a7fbcb7d6e528a` / 0.14.70 TEST。当前阶段为实现待接手。[施工 issue #11](https://github.com/shenzhoudadi/CotW-Wrath/issues/11)

- **A 已完成**。核对原作六份固定 blob、原生进展和供体、托管调用点、既有注册工具以及上游与 fork 认领。看 [A-source-and-compat.md](A-source-and-compat.md) 和 [A-native-anchors.json](A-native-anchors.json)。
- **C 已完成**。冻结 Q01–Q07 与最小定向预期。决定采用原作四级加次数表、原作默认的 AC 等级上限、13 ft 实际光环半径，以及隔离的 Wrath 职业／气池适配。看 [C-frozen-contract.md](C-frozen-contract.md) 和 [C-cases.json](C-cases.json)。
- **B 实现**。单一负责人负责高风险机制、完整变体接线和局部验证。在最新 main 的隔离分支工作。B 可分配机械蓝图接线、图标和英文说明，生产注册由同一负责人合并。[B-prompt.txt](B-prompt.txt)
- **D 独立复查**。由另一个会话核对 diff、原生调用证据、失败原件和指纹。具体缺口交原 B 修复。已验证的同内容记录可复用。复查后按仓库流程统一 TEST 交付。[D-prompt.txt](D-prompt.txt)

## B 的实施顺序

1. **重掷原语**。在本包 `src/IroranRerolls0109.cs` 建立限定到真实活动光环和实际 D20 的原生掷骰接线，先用少量受控样本证明聚合与晚到入口、正负来源、历史记录。它只随完整变体发布。风险为晚到双扣骰和影响已有 Psychic／Azata 等来源。
2. **完整变体**。使用 `src/IroranPaladin0109.cs`、`src/IroranCombat0109.cs` 或同等小范围文件，保持同一实现负责人。先接实际职业选择和完整等级表，再接 Personal Trial／气／徒手／AC、Divine Body、两道光环。只改必要的 `BlueprintRegistration.cs` 与 `ModuleCatalog.cs` 入口。复用原生资源、事实和拳头原语及现有 `PsychicAssets`、`ArchetypeBatch01445` 或 `MagusBatch01454` 注册工具。风险为旧圣武士能力残留、假信仰、共享气池回满、误删别人的标记或附魔。
3. **本项定向检查**。先跑本次改变的高风险正反例，保留原生路径。测试代码集中在一个 Iroran 测试入口，避免调用旧 Psychic 行为套件。任选现有本机隔离宿主中足够的最小入口。保留命令、秒数、输出、源／DLL／依赖指纹、实测范围和失败原件。最终交 D。

旗舰档可选 Sol、Grok 4.7、Gemini Pro／Argon、GLM 或 Kimi Pro。适合 B 高风险路径与 D 终审。Flash、Composer 或轻量 Kimi 可辅助已经冻结的逐级接线表、说明、日志索引和可重复命令执行。重掷语义、真实宗教选择、资源所有权、AC 叠加、Harmony 作用域、效果证明与最终结论由旗舰负责人处理。

编译、运行检查、打包、上传安装包均未运行。当前 GitHub 交付只包括本次规划、证据摘要和认领。已完成模块、其他贡献者范围及冻结的排除项沿用现状。
