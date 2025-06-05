<div align="center">

# 🚀 Deployment & Auto-start Configuration

**Production deployment scripts for Raspberry Pi 5**

[![Systemd](https://img.shields.io/badge/Systemd-Service-blue?style=flat-square&logo=linux)](https://systemd.io/)
[![Raspberry Pi OS](https://img.shields.io/badge/Raspberry%20Pi%20OS-Compatible-C51A4A?style=flat-square&logo=raspberry-pi)](https://www.raspberrypi.org/)

*Automated service configuration for seamless ApWifi deployment*

</div>

---

## 🇺🇸 English Instructions

### 📋 Prerequisites
- Raspberry Pi 5 with Raspberry Pi OS
- ApWifi application built and ready for deployment
- SSH access to your Raspberry Pi
- Root/sudo privileges

### 🔧 Deployment Steps

<details>
<summary><b>📦 Step 1: Upload Application Files</b></summary>

Use your preferred file transfer method to upload the ApWifi application to your Raspberry Pi:

**Recommended Methods:**
- **FileZilla** (GUI) - User-friendly FTP client
- **WinSCP** (Windows) - Secure file transfer
- **SCP** (Command line) - For advanced users

**Target Directory:**
```bash
/home/pi/ApWifi
```

**Command Line Example:**
```bash
scp -r ./publish/* pi@<PI_IP_ADDRESS>:/home/pi/ApWifi/
```

</details>

<details>
<summary><b>⚙️ Step 2: Create Systemd Service</b></summary>

Create the systemd service file for automatic startup:

```bash
sudo nano /etc/systemd/system/apwifi-app.service
```

Copy the contents from [`apwifi-app.service`](apwifi-app.service) in this directory, or use this template:

```ini
[Unit]
Description=ApWifi Application
After=network.target

[Service]
Type=simple
User=root
WorkingDirectory=/home/pi/ApWifi
ExecStart=/home/pi/ApWifi/ApWifi.App
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

**⚠️ Important:** Adjust the paths according to your actual deployment directory.

</details>

<details>
<summary><b>🎯 Step 3: Enable and Start Service</b></summary>

Configure the service to start automatically on boot:

```bash
# Reload systemd configuration
sudo systemctl daemon-reload

# Enable auto-start on boot
sudo systemctl enable apwifi-app.service

# Start the service immediately
sudo systemctl start apwifi-app.service

# Verify service status
sudo systemctl status apwifi-app.service
```

**Expected Output:**
```
● apwifi-app.service - ApWifi Application
   Loaded: loaded (/etc/systemd/system/apwifi-app.service; enabled)
   Active: active (running) since...
```

</details>

### 🛠️ Service Management Commands

| Action | Command | Description |
|--------|---------|-------------|
| **Start** | `sudo systemctl start apwifi-app.service` | Start the service |
| **Stop** | `sudo systemctl stop apwifi-app.service` | Stop the service |
| **Restart** | `sudo systemctl restart apwifi-app.service` | Restart the service |
| **Status** | `sudo systemctl status apwifi-app.service` | Check service status |
| **Logs** | `sudo journalctl -u apwifi-app.service -f` | View live logs |
| **Disable** | `sudo systemctl disable apwifi-app.service` | Disable auto-start |

---

## 🇨🇳 中文说明

### 📋 前置条件
- 运行树莓派OS的树莓派5
- 已构建并准备部署的ApWifi应用程序
- SSH访问您的树莓派
- Root/sudo权限

### 🔧 部署步骤

<details>
<summary><b>📦 步骤1：上传应用程序文件</b></summary>

使用您喜欢的文件传输方法将ApWifi应用程序上传到树莓派：

**推荐方法：**
- **FileZilla**（图形界面）- 用户友好的FTP客户端
- **WinSCP**（Windows）- 安全文件传输
- **SCP**（命令行）- 适合高级用户

**目标目录：**
```bash
/home/pi/ApWifi
```

**命令行示例：**
```bash
scp -r ./publish/* pi@<树莓派IP地址>:/home/pi/ApWifi/
```

</details>

<details>
<summary><b>⚙️ 步骤2：创建Systemd服务</b></summary>

创建用于自动启动的systemd服务文件：

```bash
sudo nano /etc/systemd/system/apwifi-app.service
```

从本目录中的[`apwifi-app.service`](apwifi-app.service)复制内容，或使用此模板：

```ini
[Unit]
Description=ApWifi Application
After=network.target

[Service]
Type=simple
User=root
WorkingDirectory=/home/pi/ApWifi
ExecStart=/home/pi/ApWifi/ApWifi.App
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

**⚠️ 重要：** 根据您的实际部署目录调整路径。

</details>

<details>
<summary><b>🎯 步骤3：启用并启动服务</b></summary>

配置服务在启动时自动运行：

```bash
# 重新加载systemd配置
sudo systemctl daemon-reload

# 启用开机自启动
sudo systemctl enable apwifi-app.service

# 立即启动服务
sudo systemctl start apwifi-app.service

# 验证服务状态
sudo systemctl status apwifi-app.service
```

**预期输出：**
```
● apwifi-app.service - ApWifi Application
   Loaded: loaded (/etc/systemd/system/apwifi-app.service; enabled)
   Active: active (running) since...
```

</details>

### 🛠️ 服务管理命令

| 操作 | 命令 | 描述 |
|------|------|------|
| **启动** | `sudo systemctl start apwifi-app.service` | 启动服务 |
| **停止** | `sudo systemctl stop apwifi-app.service` | 停止服务 |
| **重启** | `sudo systemctl restart apwifi-app.service` | 重启服务 |
| **状态** | `sudo systemctl status apwifi-app.service` | 检查服务状态 |
| **日志** | `sudo journalctl -u apwifi-app.service -f` | 查看实时日志 |
| **禁用** | `sudo systemctl disable apwifi-app.service` | 禁用自启动 |

---

## 🔍 Troubleshooting / 故障排除

<details>
<summary><b>🚨 Common Issues / 常见问题</b></summary>

### Service fails to start / 服务启动失败
```bash
# Check detailed logs / 检查详细日志
sudo journalctl -u apwifi-app.service --no-pager

# Check file permissions / 检查文件权限
ls -la /home/pi/ApWifi/ApWifi.App
```

### Permission denied / 权限被拒绝
```bash
# Make executable / 设置可执行权限
sudo chmod +x /home/pi/ApWifi/ApWifi.App

# Check ownership / 检查所有权
sudo chown -R root:root /home/pi/ApWifi/
```

### Network interface issues / 网络接口问题
```bash
# Check network interfaces / 检查网络接口
ip addr show

# Restart networking / 重启网络服务
sudo systemctl restart networking
```

</details>

---

## 📚 References / 参考资料

- [📖 .NET IoT Auto-start Configuration](https://github.com/dotnet/iot/blob/main/Documentation/How-to-start-your-app-automatically-on-boot-using-systemd.md)
- [🔧 Systemd Service Configuration](https://www.freedesktop.org/software/systemd/man/systemd.service.html)
- [🥧 Raspberry Pi OS Documentation](https://www.raspberrypi.org/documentation/)

---

<div align="center">

*Ready to deploy? Follow the steps above for a seamless ApWifi installation! 🚀*

</div>