# COTW-0046 Speaker for the Past · TEST 验证

合同 C01-SPEAKER-0046-20261108-r1。0.14.74 TEST 包含 16 项启示及 18 个过去秘仪法术。注册复用已有法术根、私有启示及原生 Ancestors 供体。TabletopTweaks-Base 的 Sands of Time 缺失时延后注册。

- D4 隐藏准备槽：升降容量后，失去来源的 Speaker 准备不再重新出现。覆盖超魔占用更高环和更低环。
- D4 同环临时来源：有有效的共享来源时保留准备。最后一个临时来源结束后清理相应超魔条目，原生 CanSpend 返回 false。
- D4 独立精魂：移除 Speaker 普通来源仍保留原生 Battle Spirit 五环 Righteous Might 及其精魂槽准备，两种取得顺序均覆盖。
- 211 条本项定向断言通过。常规构建与打包后，从实际 ZIP 取出的 0.14.74.0 主 DLL 也通过同一 211 条。主 DLL SHA256 71B50F550A5EEC01EDA2C204EC54A73D44DACF413A8FA5D18F018BDDEA48C00D。安装 ZIP SHA256 419D0233667494E2303446013260696F6397B49F991176D1CB7E95EF52218C5C。
- 构建 24.825 秒，打包与包内检查 2.470 秒。测试范围为 Speaker 定向托管宿主。实机、完整重训控制器、跨会话存读档、真实 TTT-Base 共存未测。

[安装与测试说明](../../releases/0.14.74-TEST.md)
