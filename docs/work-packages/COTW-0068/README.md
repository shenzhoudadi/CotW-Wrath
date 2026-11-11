# COTW-0068 Psychic Detective 交接

本项是 Homebrew-Archetypes Investigator 的完整灵能侦探变体。基线 `56ff43852259e2a72a46a7d21d74893d2a904e48` / 0.14.71 TEST，合同 `C-0068-20261111-r1`。A/C 已完成；B 候选交付后独立 D 复查给出 NEEDS_FIX 四项阻断，B 已在同一隔离分支完成修复（305 条定向断言 PASS），待 D 对修复提交再次独立复查。[施工 issue #12](https://github.com/shenzhoudadi/CotW-Wrath/issues/12)

- **A 已完成**。核对固定原作、HA 的实际职业与进展、原生换书和选项调用，以及现有 Psychic、Mindblade、Extra Investigator Talent 的可复用机制。[依赖与复用](A-source-and-compat.md) · [证据索引](A-evidence.json) · [180 行法表核对](A-spell-map-audit.json)
- **C 已完成**。冻结提供方门槛、完整进展、六环法书、11 种基础增幅、天赋边界和多职业来源隔离。[行为合同](C-frozen-contract.md) · [定向预期](C-cases.json)
- **B 候选已交付，D 四项阻断已修复**。书本／资源绑定与完整变体由单一负责人整合，只跑本项编译与最小定向检查；D-F01 一级原生 Detect Magic、D-F02 两档费用/强度/效果、D-F03 8/15 级延迟授予、D-F04 单豁免真实事件均已闭合，305 条断言含四项受控反证。[来源摘要](B-source-summary.md) · [直接接手 prompt](B-prompt.txt)
- **D 负责独立复查**。换一会话的旗舰档核对真实调用、负例和未测边界，具体缺陷退原 B；本轮 NEEDS_FIX 已退回并修复，待对修复提交复查。[复查 prompt](D-prompt.txt)

旗舰档可用 Sol、Grok、Gemini Pro／Argon、GLM 或 Kimi Pro。轻量档可用 Flash、Composer 或轻量 Kimi。依据实际能力分配，书本身份、支付、天赋资格、Harmony 作用域和最终结论由旗舰负责人处理。

## 实施顺序

1. **S-PSYCHIC-0068 绑定扩展**。在现有增幅运行时里补齐书本、职业和资源的显式绑定。旧组件缺省字段走原 Psychic 路径。先用正常 Psychic 与侦探的正反例证明资格、协调、支付及菜单作用域。
2. **F-0068 完整变体**。接 HA 的真实 Investigator，按原生 ReplaceSpellbook 替换炼金术书。加入完整成长、Phrenic Dabbler 与天赋接线，使用现有法表、增幅效果、临时知识和注册工具。
3. **V-0068 局部检查**。编译与本项最小定向入口，优先验证实际换书、资源支付、嵌套选项和多职业隔离。结构断言与运行时效果分别标明。
4. **D-0068 独立复查及统一 TEST 交付**。复查通过后，按用户出包授权统一提交、预发布和下载核验，再关闭 #12。

共享绑定扩展先完成，F-0068 依赖它。A/C 文档整理、图标供体核对可以独立执行。生产 BlueprintRegistration、ModuleCatalog、增幅运行时与菜单由 B 的一位负责人串行整合。

本轮 A/C 完成规划与冻结，B 完成候选实现及本项最小定向检查。游戏 / UMM、整套历史专项、真实第三方插件加载、打包与玩家实机均未运行，正式 TEST 交付在独立 D 复查通过后按用户授权进行。
