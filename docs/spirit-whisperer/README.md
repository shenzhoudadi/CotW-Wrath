# Spirit Whisperer — 源码交付与边界

2026-11-09：COTW-0117／S-SPIRIT-0117 已完成 A/C/B/D 工作及最终整合复查，源码合入基线为 `48abc49ffe4839d45dc3ac224f8ce762629e89c2`。**尚未出新安装包，旧0.14.68 Starseeker TEST附件不变。**

内容：Wizard完整十魂域（Battle、Bones、Flame、Heavens、Life、Lore、Nature、Stone、Waves、Wind）、1/8/20能力、5/10/15巫术入口、仅魔宠Arcane Bond、Wizard／Intelligence与指定Charisma资源隔离、Lore的特殊层级、Life私有Extra Channel及原作保留的bonus feat选项。只使用一份本项隔离桥，不发Spirit Magic法术，不改原生Shaman或Speaker。

冻结合同和案例见[C-frozen-contract.md](C-frozen-contract.md)、[C-cases.json](C-cases.json)。独立D最近复查见[D-review.md](D-review.md)。这些文档描述开发范围，不等于玩家验收或所有场景无缺陷。

## 最后核对

- B工程基于旧main `5275b86`，先提交其7个实际变更文件，再合并最新main；唯一注册冲突保留Spirit Whisperer和Sacred Servant晚加载入口。Vindictive Bastard、Menacing、Sacred Servant、Live On、Havocker、Starseeker均保留；未把未通过D的Speaker混入本包。
- 独立D最后一轮结论为“没有新的阻断”；D-F23／24／25修复保持。没有重开旧合同或增加全库验收要求。
- 补查Friend to Animals：私有Feature保留原生Buff `05cdce24bc6c1b547a566faad6853579` → Area `c98a2fd5b994e6e4d9560964c7877a7f` → Effect Buff `1f9dd715917bad74d9156696a5bcb9d4` 的动物豁免路径，另将召唤转化改为Wizard。D只看外层未读到组件，不等同实际删除光环。本次仅静态查链，没有运行区域或豁免结算，也没有改原生效果。
- 最新隔离检查记录为 **174条，exit 0，37.59秒**，被测DLL SHA256 `E52FCD5C7325F99189A1ABF57C19DFA33B3325DEB600B99DAB56CDEC354BC91D`，本轮已重新读取DLL确认与记录一致。B-progress／B-evidence顶层仍停留旧98条轮次，**最终状态以这份最新运行记录和D末轮为准**，不把旧指纹冒充当前产物。
- 上述174条混合了结构／组件与有限真实回调检查，**不等于174条完整战斗行为**。宿主日志还包含共同前置的历史输出，本轮没有重复运行它们或将其变为新门槛。
- 合并后主工程仅重编译一次：**0警告、0错误，8.138秒**。命令为 `dotnet build CallOfTheWild.Wrath.Prototype.csproj -c Release -p:WrathPath=<合法本地游戏> -p:BaseIntermediateOutputPath=<本次独立obj> -p:BaseOutputPath=<本次独立bin> --nologo -v:q`。只改注册整合、没有改功能实现，所以未重复运行同内容174条检查。
- 没有启动游戏或UMM、没有全量／全历史检测、没有版本变更。本机未重跑支付／休息／重训／完整存档、天气、坐标移动与完整战斗扣血链；未测不能写PASS。

## 已知边界保留

沿用D的非阻断记录：Heaven's Leap若同时残留自己和队友的标记，可能优先选中自己；选标／实际移动控制器仍待TEST玩家验证。Stormwalker天气更新、Deathly Being治疗转换等已有局部回调检查不冒充完整游戏测试。原生效果共享部分没有逐项重建全场景。后续按玩家实际log定向修复，不自动启动新的HOLD深挖队列。

本轮仅按用户要求提交合并源码。issue #7标为“开发完成／源码已合入，待TEST交付”，保留占用，避免别人重做；后续正常TEST安装包交付后再关闭，不把源码合并写成已发布新版。
