# VerifyIRAC — IRtek SDK × IRAC 图片数据验证工具

用 `Amur.IRtekFileSDK`（热像）与 `Amur.IRtekACFileSDK`（声像）批量解析 `IRAC/` 目录中的
FOTRIC 热像仪 / 声像仪 JPG 文件，验证 SDK 的解析能力。

## 验证内容（对每个文件）

- 文件类型识别（热像图片 / 声像图片 / 其他）
- 相机型号、序列号、镜头、拍摄时间
- 测温参数（发射率、距离、反射温度、湿度）、温宽
- 全幅温度场最高/最低温（640×480 = 307200 点）、中心点温度、原始 AD 矩阵
- 声像：频带（Hz）、声宽范围、点/椭圆区域声压级（dB）、热点定位
- 文件内置测量标记数量
- 内部图像导出：热像图 / 可见光图 / 声像融合图 → PNG

## 构建与运行

依赖 6 个 DLL（已在 `bin/` 内）：托管库 `Amur.IRtekFileSDK.dll`、
`Amur.ThermalImageFile.dll`、`Amur.IRtekACFileSDK.dll`（x86），原生库
`IRtekFileSDK.dll`、`IRtekACFileSDK.dll`、`AcousticUtils.dll`（x86）。
**SDK 原生库为 32 位，必须以 x86 进程运行。**

```bash
# 重新编译（Windows 自带 csc，无需 Visual Studio）
cd SDK/VerifyIRAC/bin
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe -nologo -target:exe -platform:x86 ^
  -out:VerifyIRAC.exe -r:Amur.IRtekFileSDK.dll -r:Amur.ThermalImageFile.dll ^
  -r:Amur.IRtekACFileSDK.dll -r:System.Drawing.dll ..\VerifyIRAC.cs

# 运行
VerifyIRAC.exe "<IRAC目录>" "<PNG导出目录>"
```

## 结论（2026-09-11，18 个样本）

- 热像识别：10 个 | 声像识别：8 个 | 无法识别：0 个
- 全部元数据、温度场、声压级数据解析成功，详见 `验证结果.txt`
- 解析出的图像样例见 `IRAC/_parsed/`

## ProbeIRAC — 深度数据探测（2026-09-14）

在基础验证之上，把两个 SDK 的全部公开读取接口逐一调用，输出完整数据清单：

- 热像新增：镜头视场角、完整测温参数（大气温度/外部光学）、测温范围、调色板编号与自定义色数、
  修改时间、GPS/罗盘、备注、用户数据键值对（FAVORITE/TAG/GRIDNOTE/GROUPFILES）、
  **内置测量标记详情**（类型/名称/几何/最高最低平均温/热点冷点坐标/面积/独立测温参数）、
  温差 Delta、帧时间戳、参考温度、音频
- 声像新增：**传感器信息**（序列号/162 通道/采样率 200kHz/固件版本）、检测模式（PD/泄漏）、
  聚焦、目标距离、标准声压级修正、交流电频率、GPS/罗盘方向、备注、用户数据（**PD 局放识别结果
  JSON**、LEAK 泄漏评估等）、收藏/标签、调色板透明度、**内置测量标记详情**（点位/区域/声压级/热点）、
  **帧级数据**：320×240 声压级 SPL 网格（逐点 dB）、原始/滤波/降采样声音波形、1000 点频谱 FFT、
  PRPD 散点图（约 2000~3400 点）、可见光 JPEG
- 图像导出增加：封面图、调色板色带（1×256）

```bash
cd SDK/VerifyIRAC/bin
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe -nologo -target:exe -platform:x86 ^
  -out:ProbeIRAC.exe -r:Amur.IRtekFileSDK.dll -r:Amur.ThermalImageFile.dll ^
  -r:Amur.IRtekACFileSDK.dll -r:System.Drawing.dll ..\ProbeIRAC.cs

ProbeIRAC.exe "<IRAC目录>" "<PNG导出目录>" "<报告txt路径>"
```

结果：18/18 全部成功（热像 10 + 声像 8），完整输出见 `探测结果.txt`。只读不写，不修改原文件。
