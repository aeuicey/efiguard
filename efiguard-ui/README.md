# EfiGuard UI

一个基于 Apple 设计风格的 Windows 系统安全功能管理工具，用于可视化监控系统虚拟化与安全状态、关闭 VBS (Virtualization-based Security) 以及管理 EfiGuard 启动项。

## 功能

- **实时监控仪表盘**：
  - VBS (Virtualization-based Security)
  - 内存完整性 / HVCI (Hypervisor-enforced Code Integrity)
  - Credential Guard
  - Hyper-V 平台状态
  - CPU 虚拟化支持 (VT-x / AMD-V)
  - SLAT (二级地址转换)
  - Secure Boot
  - TPM 状态
  - Hypervisor 启动类型
  - EfiGuard 启动项检测

- **一键关闭 VBS**：复用华为 `tool.bat` 逻辑，自动执行 DISM 禁用 Hyper-V 功能并配置 BCD 启动项

- **EfiGuard 管理**：从 GitHub 自动下载最新版 EfiGuard，部署到 ESP 分区并创建 BCD 启动项

## 运行要求

- Windows 10/11 x64
- **必须以管理员权限运行**（否则所有操作按钮将被禁用）

## 使用方法

### 方式一：使用管理员启动器（推荐）

双击 `RunAsAdmin.bat`，脚本会自动请求 UAC 提升并以管理员权限启动 EfiGuard UI。

### 方式二：手动以管理员身份运行

右键 `EfiGuard UI.exe` → **以管理员身份运行**。

### 开发模式

```bash
cd efiguard-ui
npm install
npm start
```

## 打包构建

```bash
npm run dist:dir    # 生成 unpacked 目录
npm run dist        # 生成便携版 exe（需要网络下载 NSIS，可能失败）
```

> 注意：在当前网络环境下，便携版打包所需的 NSIS 工具可能无法从 GitHub 下载。可直接分发 `build/win-unpacked/` 文件夹。

## 技术栈

- Electron 35
- HTML5 / CSS3 / Vanilla JS
- PowerShell / WMI / BCDEdit / DISM

## 警告

- 关闭 VBS 和安装 EfiGuard 属于**系统级修改**，可能导致系统安全性降低
- EfiGuard **无法绕过 HVCI / 内存完整性**，若 HVCI 处于启用状态，EfiGuard 的 DSE 补丁将无效
- 所有修改操作执行后**需要重启系统**才能生效
- 请在明确了解风险的情况下使用本工具

## License

GPLv3（与 EfiGuard 保持一致）
