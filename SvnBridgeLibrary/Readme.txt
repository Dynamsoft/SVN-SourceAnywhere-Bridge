1.可以下载TortoiseSVN做测试
输入类似如下URL访问http://"bridge server name":"bridge server port"
http://192.168.1.212:808

检验用户登录时，在user输入的地方应该加上repository name，格式repository name\user name，如default\admin，只取第一个\，如果repository包含\，那么
暂时不支持，如果user name中包含\不影响
或者repository在scconfig.ini中配置

2.配置文件叫scconfig.ini(必须与exe文件放置在同一个目录), 配置文件读取在SvnBridge.Library-->RequestHandlers-->SAWSHandlers-->SAWAdapter.cs-->SAWCommon
sc是source control的缩写；B是bridge缩写
[SourceControl]
SCIP=127.0.0.1	如果是Hosted则改为 SCID=100001
SCPort=7777		如果是Hosted则使用443表示SSL，其他值表示非SSL
Repository=Default
TempPath=C:\Temp\
SCBIP=192.168.1.212
SCBPort=808

3.主要是改SvnBridge.Library-->RequestHandlers-->SAWSHandlers下的文件
如SourceAnywhere Standalone SDK访问是在SAWSAdapter.cs中 (已经实现)
SourceAnywhere for VSS SDK访问是在SAWVAdapter.cs中
SCM Anywhere Standalone SDK访问是在SCMSAdapter.cs中

使用不同的SDK需要把不同的SDK加入到SvnBridge.Library的reference中(目前Saws与Sawh不能同时add进项目)

4.Bridger server启动入口在SvnBridge.Library-->Net-->Listener.cs-->Start函数

5.Source Control连接登陆入口在SvnBridge.Library-->RequestHandlers-->SAWSHandlers-->SAWSUserInfo.cs

6.调试时需要选择不同的Solutions配置，如SAWS_Debug, SCMS_Debug, SAWV_Debug，启动项目设为SvnBridge，x86, (SCM可能支持any cpu）
