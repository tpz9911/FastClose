## FastClose

### 【简介 Intro】

**FastClose** 用于向目标程序发送正常退出信号，快速关闭目标程序（不用在程序里面去找退出选项）。

右键弹出设置菜单。  
优先查找进程名称（程序的进程名称通常是其主程序名，不含扩展名），如果没有填写进程名称，将匹配程序标题。

It is used to send a graceful exit signal to the target program to close it quickly, without needing to find the exit option within the application.

Right-click to open the settings menu.  
It prioritizes matching by process name (which is typically the main executable name without the file extension). If no process name is specified, it matches by the program title.  

### 【软件截图 Snapshot】

![截图1](/img/Snapshot_0001.png)

### 【运行环境 Runtime】

.NET 8+

如果你的 Windows 没有安装过 .NET 8+ 运行环境，程序在第一次运行时将由 Windows 系统跳转到微软网站，引导你下载 **.NET 桌面运行时**。

如果系统没有自动跳转，你也可以手动访问微软 .NET 网站（ https://dotnet.microsoft.com/zh-cn/download/dotnet ），自行下载 **.NET 桌面运行时** 并安装。

If the .NET 8 (or later) runtime is not installed on your Windows system, Windows will prompt and redirect you to the Microsoft website on the first launch to download the **.NET Desktop Runtime**.

If you are not redirected automatically, you can manually visit the Microsoft .NET Download Page to download and install the **.NET Desktop Runtime**.
