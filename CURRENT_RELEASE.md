# 当前开发与出包流程

0.14.66已发布。入口为仓库根 `build.ps1`；本地原工程位于 `outputs/CallOfTheWild.Wrath.Prototype`。

1. 只处理本轮明确改动。实际改行为时，选择一次必要定向检查；已有同内容结果不因文档、打包、上传重复执行。
2. 默认执行 `./build.ps1 -WrathPath '<本地游戏目录>'`：编译主体和两个内置模块，打包，核对必要文件、三个DLL与资源。默认不运行测试。
3. 从本次staging的 `验证记录.json` 读取实际结果和 `timings`。失败按log修复；未执行检查写未运行，不追补全历史或旧证书。
4. 用户要求发布时，生成当前源码包与简短说明，提交并上传GitHub/原Drive；仅对本次文件下载核对一次。保留旧正式附件，更新HANDOFF，清理本轮无用产物。

`-CheckStartup`、`-CheckRules`、`-Focus amnesiac066` 都是按具体问题显式选择的诊断，不是默认门禁。启动检查需要本地游戏和模块引用；Amnesiac旧诊断另依赖未公开的原工作夹具，独立clone不保证可直接运行，缺失时不恢复整套历史环境来阻塞普通出包。

当前脚本沿用源码上两级的 `work` 布局；原开发目录按 `outputs/CallOfTheWild.Wrath.Prototype` 放置。独立clone先核对实际输出目录和模块本地引用，只补当前需要的依赖，不下载整个旧work。

## 耗时

2026-11-03同机热构建实测：编译11.953秒，打包核对1.611秒，总计13.612秒。本次无功能增量，定向检查未运行；不能写成“零秒通过”。未重复运行已发布Amnesiac专项。

每次记录 `timings.compileSeconds`、`packageChecksSeconds`、`incrementalChecksSeconds`、`checks` 和 `totalSeconds`；没有定向检查时 `incrementalChecksSeconds=null`。发布另记实际上传用时。旧专项未记录进程总时长，不反推或补造旧秒数。

规划估计：普通小修的必要定向检查加出包约1–5分钟；无行为变化时同机编译打包约15–30秒。相较旧全历史流程，纯验证/出包时间可能减少80%–95%；这是估计，不含编码、排错和上传，用后续实测校正。
