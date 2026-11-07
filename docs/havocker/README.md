# Havocker / S-KINETIC — 0.14.67 TEST

本次将自0.14.66正式基线以来的Havocker实现、Spellburn图标接线和Mimic展示改动提交到仓库。用户已反馈Havocker试玩总体正常；完整移植的历史边界仍保留未测说明，不把人工总体反馈扩写为逐项验收。

## 已包含

- K01-HAVOCKER-20261106-r1下的私有四元素念袭图、Witch有效等级／Int主属性、双向Kineticist入职隔离、Spellburn与原生Spend统一结算、刀刃续击及生命周期处理；私有图443节点。
- Spellburn 2—9环Buff／能力／特性引用原生Gather Power图标。离线仅证明引用复制，画面待人工确认。
- Mimic按来源名称／描述／图标评分选择展示，并在后期新提供方出现时同步已有Buff、toggle、leaf及第一选择器；保留旧tier与GUID，**单根多Rank暂缓**。复用来源：[sxl1216/CotW-Wrath@bf5a2a7](https://github.com/sxl1216/CotW-Wrath/commit/bf5a2a7bc25a53c45355396f078ab24ad4dd342d)，保留原许可；未整仓合并供方改动。

## 验证边界

K12离线BridgeTests141/141；之后图标引用1/1、Mimic展示同步1/1均有独立定向记录。后两次使用0.14.66同功能DLL，不冒充0.14.67重新执行；0.14.67仅版本及说明更新，重新编译15.514秒、通常包检查2.066秒。独立复查确认本轮测试设置及包内说明已补齐。

Chain完整场景弹射、Archer合法获得／完整交付、完整冲锋／念刃反击、部分外部Hex／Patron提供方绑定、完整存档／重训与真实VFX等仍不宣称通过。旧Psychic夹具amplifications058／foreign-meta061／062未运行。没有全量检测、游戏或UMM启动；原生隔离结果不是玩家游戏验收。

冻结合同和cases见本目录；它们是历史机制裁定，不代表文首冻结时尚未实现的状态仍是当前状态。当前可用消费者源码与宿主patch见../../tests/offline-havocker/。仅克隆模组仓库无法取得合法游戏、第三方DLL和本地夹具，不为此重建整套历史环境。

主DLL：`3aad748c77197d5b1f1de5a253b1b8349f43509d7ef8bd4f36cac69edc03ad61`；安装包：`7eae2bce5f5ab89ac129b2d8a1afbe99fc467393944e66448ed20372a57b1dc8`。
