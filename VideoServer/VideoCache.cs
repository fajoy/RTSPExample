using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
public class VideoCache
{

    public string path;
    public List<byte[]> frames = new List<byte[]>();
    public event Action<VideoCache, int> FrameLoaded;
    public VideoCache(string filepath)
    {
        path = filepath;
    }
    public void startLoad()
    {
        Capture c = new Capture(path);
        Image<Bgr, Byte> frame = null;

        do
        {
            frame = c.QueryFrame();
            if (frame == null)
                break;
            Bitmap bmp = frame.ToBitmap();
            byte[] payload = FrameHelper.getFrameBytes(bmp);
            RTPModel pkg = new RTPModel(0, frames.Count, frames.Count, payload);
            frames.Add(pkg.toBytes());
            FrameLoaded.Invoke(this, frames.Count - 1);
        } while (true);

    }
}