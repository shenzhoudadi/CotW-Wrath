# Spiritualist · D2 修复与 D3 复查

基线为 `e9925c6`，对应 [D2 复查](D2-spiritualist-review.md) 和 [#16](https://github.com/shenzhoudadi/CotW-Wrath/issues/16)。当前为 D3 复查和后续修复阶段。

## D2 修复

- **等级和成长**。删除六个情绪技能蓝图的冗余等级公式。Ferocious Mien 用原生上下文属性读取绑定主人的 Spiritualist 等级。Phantom 基础三项用低豁免，按情绪覆盖两项高豁免。共享成长为 DEX/CHA +1，Anger 用 STR/CHA +1 替换同一组八个等级，复用原生特性 rank 和加值组件。
- **互保、重训与骑乘**。两开关使用原生合法组号，由 0085 自己关闭对应的另一开关。付款包使用原生伤害规则和精确整数，通过付款包身份跳过付款方临时 HP，普通伤害仍处理临时 HP。Shared/Fused 的豁免标为职业 buff，来源自有 Skill Focus 用原生职业来源标记。骑乘保留原生认可的其他伙伴，并排除 Phantom、分身、骨骸冠军。
- **原生复用**。只在成功休息的原生复活队列补入被漏掉的已绑定死亡 Phantom，复活、HP 和落点走原生流程。二环 Ghoul Touch 引用保留 DLC 条件的原生 sticky-touch 入口。Kindness 优先复用已启用的 ExpandedContent Aid Another，补上动作类型和 +2。无该提供方时只向 Kindness 授予原作式的盟友进攻／防御动作。
- **Kindness 两处后续修复**。Opening Strike 使用原生近战攻击和威胁名单，依同源且未过期的机会执行一次迅捷后续攻击，Exceptional Aid 的伤害加值随该攻击应用。四次 Mercy 由主人等级界面的选项提供，主情绪 12 级或次情绪 20 级。入选的原生 Mercy 事实授给当前绑定 Phantom。

## 本机验证

- 主体和定向宿主编译 **2.639 秒**，主体无警告，宿主有两条既有警告，0 错误。前两次测试分别因夹具未读入真实豁免进度（14.182 秒）和空白替身的实体比较（14.282 秒）失败，修复后复跑。
- 定向宿主 **252/252，14.025 秒**。包含原生等级及合法组查询、真 HP 付款、两个原生休息查询中仅修改复活队列、原生豁免与 DLC 条件、Mercy 等级条目和 Opening Strike 接线。核对原生蓝图 181 条。
- 独立 Harmony 探针编译 **0.124 秒**、运行 **0.422 秒**。在原生 Harmony 2.0.4.0 上成功安装伤害、休息、骑乘两处、Aid Another 动作和既有提供方加值共六处补丁。探针只安装补丁。
- 本地 TEST ZIP 23 条，打包与三 DLL 核对 **0.674 秒**。主 DLL SHA256 `47086EA463621E8CBF690169C23ABC39DB70ED682F163CDA3BBFF2AD3F545A1D`，ZIP SHA256 `AC03463A8F521928167D8B8CDADDDFA396A1E407519EEE9368CA6EFED7B7B3C3`。上传未运行。

## D3 后续

- 原生补丁安装与当前定向结果已核。阵亡和其他移除事件仍需核对，当前死亡入口清除 Bonded，未显式关闭这对伤害转移开关。伙伴移除事件目前无处理。
- 完整 AddPet 伙伴链、原生存读档／重训界面、休息 UI、骑乘按钮、真实战斗及实际 ExpandedContent 安装组合仍未运行。玩家验收保持开放。
