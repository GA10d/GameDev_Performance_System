# CLI 验证记录

2026-09-28，Unity 2022.3.62f1 / Windows / Python 3.12.7。

测试使用独立 `authoring-qa/UnityProject`，不修改正式工程中的用户演出。正式安装的 C# 与独立测试版本逐个 SHA256 比对。

- `unity-verification.txt`：53 条检查通过。覆盖五种模板和 JSON 往返、选项与超时结局、条件 yes/no、全局选择、局部资源扣除、不可用/过晚/多余选择、角色绑定与时间覆盖、悬空出口、无结束路径、不可达节点、新资产生成、拒绝覆盖/越界、中文保存、依赖导出、保护其他未保存资产、保持当前编辑会话。
- `python-verification.txt`：14 项 unittest 通过。覆盖 JSON Schema、枚举往返、未知/缺失字段、负时间、NaN、Unity float / int 溢出、布尔与整数区分、重复 JSON 键、中文编码、文件覆盖保护、JSON 错误输出与退出码。
- `transport-verification.txt`：16 条检查通过。使用实际打开的 Unity 编辑器，通过 Python CLI 连续查询素材、用中文参数生成外星人问答、校验、模拟三个分支、精确报错、生成资产、回读、导出、拒绝覆盖、生成修改版并确认原版不变。
- `cold-restart.txt`：关闭上述测试编辑器后，用 batch CLI 再次启动 Unity 回读生成资产，与原始完整 JSON 相等。

这是结构、资产、逻辑与传输验证；没有新增真实渲染截图或游戏导航验收。运行时代码未修改，CLI simulate 不渲染画面。

可重复运行 Python 测试：在 Tool/V2 下执行 `python -m unittest discover -s CLI -p test_astra.py -v`。

Unity 检查入口：`ToolV2CliQA.Run`，需要 `-cliQA`，只在独立测试工程运行。该检查会在测试工程创建 `Assets/CliQA` 版本目录和工程外的导出目录，不能对正式工作工程运行。
