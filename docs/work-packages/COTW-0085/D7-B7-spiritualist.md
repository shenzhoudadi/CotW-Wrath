# Spiritualist · D7 光环与伤害转移复查

复查基线为 `eecc664`，覆盖 [C13–C15](C-spiritualist-cases.json)。[D6/B6 记录](D6-B6-spiritualist.md)

- **C15 光环激活**。Wrath 的 `AddAreaEffect` 从 `Buff.Context` 生成跟随光环，原先直接装到 Phantom 的 Feature 上。两个 Inference 特性现用仓库 Iroran Paladin 已有的原生 `AuraFeatureComponent → BlueprintBuff → AddAreaEffect` 接法，并与情绪光环共用载体构建。既有 Feature、Area、受益 Buff 的 GUID 保留。
- **C15 多来源隔离**。删除按施法者手工增删 Buff 的 Enter/Exit 动作，沿用情绪光环的原生 `AbilityAreaEffectBuff`。游戏写入 `SourceAreaEffectId`，退出旧实例时只移除旧实例的效果。受益资格仍由同一个私有谓词在光环内重查，并在 AC 和豁免计算时即时重查。旧开发包留下的无区域来源 Buff 不再给加值。原生护盾和情境描述符均按非叠加处理。
- **C13/C14 转移门控**。原生 `RuleDealDamage` 在目标不可选或处于绿名保护时提前结束。转移现在先检查相同的原生门控再扣减原始伤害，避免原伤害已经扣减、转移对象实际没有受到伤害。既有精确整数和临时 HP 处理走原生伤害事件。

## 本机验证

- 最终主体和宿主编译 **2.642 秒**，主体无警告，宿主两条既有警告，0 错误。隔离定向 **350/350，14.277 秒**。新增测试走原生不可选目标门控，检查两组光环的载体和条件，并让原生退出动作处理三个来源。退出测试以最小集合夹具承接 Buff.Remove。前两次定向分别在夹具的组件列表与单位属主绑定处中止，耗时 14.189 秒和 14.282 秒，对应编译 2.781 秒和 1.832 秒。补齐夹具后，中间版本 348/348（14.478 秒，编译 1.775 秒）。随后按新查到的载体错误修改生产接线并完成最终验证。
- 原生 Harmony 2.0.4.0 安装全部 11 组 0085 补丁，覆盖 12 个原生方法，耗时 **0.451 秒**。本地 TEST ZIP 23 条，打包与三个 DLL 回读 **0.674 秒**。主 DLL SHA256 `7435FA56B12E0E1E1443F0148D4AABEA4B826278878FCB988056CA7E7B00EE82`，ZIP SHA256 `A694F4B28B8B6C45F7AC1CE45DAE71555597CC58983F665A4C84C2E7DA5D3D64`。安装包上传未运行。
- 玩家场景、完整 AddPet、真实战斗扣血与事件间状态变化、藏匿与位置、存读档、重训、休息 UI 和骑乘按钮未运行。光环场景生成也仍待玩家 TEST。
