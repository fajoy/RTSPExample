RTSPExample
=====================

描述
=
> 本程式乃為**交大大學 2012第一學期 [計算機網路]期末作業**，
> 本程式引用Emgu CV授權為 GPL v3 。
> 

程式需求
-
- Microsoft Windows7 x64
- Microsoft Visual Studio 2008
- Emgu CV x64

本範例專案包含以下C#程式範例
-
- Emgu CV (影格擷取)
- Jpge 壓縮
- Async TCP 
- Async UDP

專案說明
-
- Emgu.CV (Emgu CV原始碼)
- Emgu.Util (Emgu CV原始碼)
- HelloWorld (Emgu CV原始碼 可測試Emgu CV是否正常運作)
- VideoTest (可測試Emgu CV是否可正常擷取影格)
- VideoServer (RTSP伺服器)
- VideoClient (RTSP客戶端)

物件說明
-
**共用物件**

- RTPModel 
 - RTPModel class <-> rtp packet byte array 轉換 
- FrameHelper 
 - Bitmap class <-> jpge byte array 轉換 
- SockHelper
 - Async TCP 與 Async UDP 物件


**伺服器端**

- VideoCache
 - 將VideoFile 轉成 RTP封包 byte存放
- RTPPlayer
 - 負責發送RTP封包與儲存播放狀態
- RSTPServer
 - 負責處理Client TCP的RTSP Rquest 並控制RTPPlayer

**客戶端**

- ClientPlayer
 - 負責處理接收到的UDP封包並轉成Image存放進Queue
- RSTPClient
 - 負責儲存RTSP狀態、RTSP Rquest發送與RTSP Response處理


參考文獻
=====================
- [RTSP協定] 
- [RTP協定]
- [Emgu CV]

[RTSP協定]: http://en.wikipedia.org/wiki/Real_Time_Streaming_Protocol
[RTP協定]: http://en.wikipedia.org/wiki/Real-time_Transport_Protocol
[Emgu CV]: http://sourceforge.net/projects/emgucv/
