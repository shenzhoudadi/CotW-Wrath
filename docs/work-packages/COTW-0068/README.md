# COTW-0068 Psychic Detective 交接

本项是 Homebrew-Archetypes Investigator 的完整灵能侦探变体。基线 `56ff43852259e2a72a46a7d21d74893d2a904e48` / 0.14.71 TEST，合同 `C-0068-20261111-r1`。A/C 已由本会话完成，B 待实现，D 待独立复查。[施工 issue #12](https://github.com/shenzhoudadi/CotW-Wrath/issues/12)

- **A 已完成**。核对固定原作、HA 的实际职业与进展、原生换书和选项调用，以及现有 Psychic、Mindblade、Extra Investigator Talent 的可复用机制。[依赖与复用](A-source-and-compat.md) · [证据索引](A-evidence.json) · [180 行法表核对](A-spell-map-audit.json)
- **C 已完成**。冻结提供方门槛、完整进展、六环法书、11 种基础增幅、天赋边界和多职业来源隔离。[行为合同](C-frozen-contract.md) · [定向预期](C-cases.json)
- **B 负责实现**。旗舰档模型做书本／资源绑定与统一生产整合。轻量档可按冻结表接私有蓝图、说明和目录。单一负责人合并共享文件。[直接接手 prompt](B-prompt.txt)
- **D 负责独立复查**。换一会话的旗舰档核对真实调用、负例和未测边界，具体缺陷退原 B。[复查 prompt](D-prompt.txt)

旗舰档可用 Sol、Grok、Gemini Pro／Argon、GLM 或 Kimi Pro。轻量档可用 Flash、Composer 或轻量 Kimi。依据实际能力分配，书本身份、支付、天赋资格、Harmony 作用域和最终结论由旗舰负责人处理。

## 实施顺序

1. **S-PSYCHIC-0068 绑定扩展**。在现有增幅运行时里补齐书本、职业和资源的显式绑定。旧组件缺省字段走原 Psychic 路径。先用正常 Psychic 与侦探的正反例证明资格、协调、支付及菜单作用域。
2. **F-0068 完整变体**。接 HA 的真实 Investigator，按原生 ReplaceSpellbook 替换炼金术书。加入完整成长、Phrenic Dabbler 与天赋接线，使用现有法表、增幅效果、临时知识和注册工具。
3. **V-0068 局部检查**。编译与本项最小定向入口，优先验证实际换书、资源支付、嵌套选项和多职业隔离。结构断言与运行时效果分别标明。
4. **D-0068 独立复查及统一 TEST 交付**。复查通过后，按用户出包授权统一提交、预发布和下载核验，再关闭 #12。

共享绑定扩展先完成，F-0068 依赖它。A/C 文档整理、图标供体核对可以独立执行。生产 BlueprintRegistration、ModuleCatalog、增幅运行时与菜单由 B 的一位负责人串行整合。

本轮只完成 A/C 和交接。编译、功能检查、打包与玩家实机均未运行。
