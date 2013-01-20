using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using System.Collections.Generic;
using System.Linq;
using System.Text;


public class FrameHelper
{
    public static Image getFrame(byte[] data)
    {
        MemoryStream ms = new MemoryStream(data, 0, data.Length);
        return Bitmap.FromStream(ms);
    }
    public static ImageCodecInfo GetImageEncoder(ImageFormat format)
    {
        return ImageCodecInfo.GetImageEncoders().ToList().Find(delegate(ImageCodecInfo codec)
        {
            return codec.FormatID == format.Guid;
        });
    }

    public class MyEncoder
    {
        public ImageCodecInfo myEncoder = GetImageEncoder(ImageFormat.Jpeg);
        public EncoderParameters myEncoderParameters = new EncoderParameters(1);

        public MyEncoder()
        {
            System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameter myEncoderParameter = new EncoderParameter(encoder, 15L);
            myEncoderParameters.Param[0] = myEncoderParameter;
        }
    }
    static MyEncoder jgpEncoder = new MyEncoder();
    public static byte[] getFrameBytes(Bitmap bmp)
    {
        byte[] data;
        MemoryStream ms = new MemoryStream();

        bmp.Save(ms, jgpEncoder.myEncoder, jgpEncoder.myEncoderParameters);
        int len = Convert.ToInt32(ms.Position);
        data = new byte[len];
        ms.Position = 0;
        ms.Read(data, 0, len);
        return data;
    }
}
