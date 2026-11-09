# A交C的差异／缺口

这些是需要裁决的行为差异，不是要求恢复旧环境。C已在C-frozen-contract.md逐项裁决。

1. **Q01 固定魂域还是每日切换**：Oracle.cs仅给3级FeatureSelection，没有切换控制器；旧REMAINING描述daily/wandering不能当代码合同。
2. **Q02 技能替换**：Wrath无原作mystery_skills公共进展项，且Oracle基础已含四Knowledge/Lore。保留Mystery身份时必须仅去掉该来源额外技能；普通Oracle／其他技能来源不受影响。原生Retain/Release无来源参数，成对和读档幂等是实现风险。
3. **Q03 属性**：全部createOracleSpirit的基础／高阶能力secondary=Charisma；HexEngine.secondary=Wisdom。不能全文替换Cha或把0117的主Int／次Cha照搬。
4. **Q04 Oracle版目录**：Lore=Arcane Enlightenment；Waves=Skip(1)保留Fluid Magic；不能照抄0117的Perfect Knowledge或其Waves列表。
5. **Q05 法术来源**：4级起9项仅按当前主书可用环位逐步加入。原作LearnSpellList移除不可靠；沿用已存在原生临时已知账本，保留独立永久／临时来源，不能继承Speaker未通过复查的RemoveSpell实现。
6. **Q06 外部提供方**：90项均有身份映射；4个外部唯一根的固定源码已定位，未读取本机运行注册缓存，未验证已安装／启用。实现时需真实提供方可用性门禁和迟加载重试，不能建空壳、替换法术或删族放行。
7. **Q07 Arcane Enlightenment**：Oracle版15/17/19各选一项Wizard法术；Int和Cha均需>=10+环级，排除Oracle基础法表。辅助wandering progression在19级开始，不是本构造器返回的greater，不能两份同时授予。学到的法术仍需来源安全撤销。
8. **Q08 Fluid Magic**：Oracle使用正常自发槽，本入口的空marker对应已满足语义；不是准许其他能力以空Feature占位。不新增特殊槽、加次数或额外法表“补偿”。
9. **Q09 私有图／复用**：原生Spirit效果可能仍有Shaman职业、Cha资源、嵌套Buff/Area/Weapon边；只能共享证明确不需要改变的叶子。不要把外层字段检查当实际支付／清理／效果通过。
10. **Q10 旧已知问题**：0117Heaven's Leap的多标记优先自己不能复制进0060；Friend to Animals在本包应按HexEngine的Wisdom、最少0而非原生Buff固定Cha；少量原生图的显示名与同名资源不能代替行为适配。

**A阶段不存在阻止C定案的未定位法术身份。** 提供方实际安装／启用、运行时资格与控制器属于B/D的必要检查或诚实未测边界；本轮没有编译、测试、游戏／UMM启动，也没有任何PASS宣告。
