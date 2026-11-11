# COTW-0068 复查与 TEST 交付核查

复查候选 `2b5eb9149b76ddd0417f92bee2ff7c7b90e8e1fd`，合同 `C-0068-20261111-r1`。D2 与 B-fix 来自同一会话。D2 报告四项修复闭合，定向记录为 305 条断言／15.38 秒。详见 [issue #12 的 D2 结论](https://github.com/shenzhoudadi/CotW-Wrath/issues/12#issuecomment-6790671036)。

- D-F01 确认一级授予原生 Detect Magic 及私有施法特性。
- D-F02 确认 Defensive Prognostication 的两档费用为 1／2 点，效果为 +2／+4。
- D-F03 确认 Overpowering Mind 的 8／15 级档使用 HA Investigator 等级授予。高级后选和 PostLoad 尚未执行。
- D-F04 确认 Psychic Meddler 对本次豁免应用临时修正，上限 +6。豁免替换机制的交互仍需实机观察。

D2 报告正文列出的 DLL 与宿主 SHA256 和证据 JSON 不一致。留存的 JSON 与现有文件、日志的哈希相符。公开记录保留此差异，本次发布以发布候选的独立产物和定向日志为准。原始 D2 报告及日志仍在本机交接目录。

发布核查确认初次注册缺少 HA 时，旧 RegisterOptional 条件会跳过稍后就绪的变体。已让该入口调用现有注册函数处理首次发布和后续刷新。C01 在尚无侦探根时走真实 RegisterOptional，验证提供方就绪后发布。复跑 306 条断言通过，14.778 秒，0 失败和 0 HOST LOG。另保留 D2 的未测边界，包括真实 HA／TTT、C05、C08、C13、C16、C20、存读档／重训与玩家实机。
