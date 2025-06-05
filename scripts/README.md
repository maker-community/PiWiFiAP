## 树莓派配置程序自启动方法

### 1. 使用FileZilla等工具将程序上传到树莓派的家目录

本人复制的目录为/home/gil/ApWifi

### 2. 创建配置自启动配置脚本

```
sudo nano /etc/systemd/system/apwifi-app.service

```

将当前目录的apwifi-app.service文件内容复制到要创建的脚本里，注意配置自己的程序执行名称和路径。

### 3.  查看服务状态启动服务自启脚本

```
# 如果服务脚本有修改可以使用它操作
sudo systemctl daemon-reload


# 查看服务状态
sudo systemctl status apwifi-app.service

# 启动服务

sudo systemctl start apwifi-app.service


# 设置服务启动
sudo systemctl enable apwifi-app.service
```

[.NET IOT 服务自启动配置参考文档](https://github.com/dotnet/iot/blob/main/Documentation/How-to-start-your-app-automatically-on-boot-using-systemd.md)