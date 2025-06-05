# Deployment and Auto-start Configuration

## English Instructions

### 1. Upload the Program to Raspberry Pi Home Directory

Use FileZilla or other tools to upload the program to the Raspberry Pi's home directory.
Example directory: `/home/pi/ApWifi`

### 2. Create Auto-start Configuration Script

```bash
sudo nano /etc/systemd/system/apwifi-app.service
```

Copy the contents of the `apwifi-app.service` file from this directory to the script you're creating. Make sure to configure your own program execution name and path.

### 3. Check Service Status and Start the Auto-start Script

```bash
# If the service script has been modified, you can use this command
sudo systemctl daemon-reload

# Check service status
sudo systemctl status apwifi-app.service

# Start service
sudo systemctl start apwifi-app.service

# Enable service to start on boot
sudo systemctl enable apwifi-app.service
```

---

## 中文说明

### 1. 使用FileZilla等工具将程序上传到树莓派的家目录

建议复制到目录：`/home/pi/ApWifi`

### 2. 创建自启动配置脚本

```bash
sudo nano /etc/systemd/system/apwifi-app.service
```

将当前目录的 `apwifi-app.service` 文件内容复制到要创建的脚本里，注意配置自己的程序执行名称和路径。

### 3. 查看服务状态并启动服务自启脚本

```bash
# 如果服务脚本有修改可以使用此命令重载
sudo systemctl daemon-reload

# 查看服务状态
sudo systemctl status apwifi-app.service

# 启动服务
sudo systemctl start apwifi-app.service

# 设置服务开机自启动
sudo systemctl enable apwifi-app.service
```

## Reference Documentation

[.NET IoT Auto-start Configuration Reference](https://github.com/dotnet/iot/blob/main/Documentation/How-to-start-your-app-automatically-on-boot-using-systemd.md)