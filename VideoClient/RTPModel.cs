using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public struct RTPModel
{
    static int HEADER_SIZE = 12;
    public int Version;
    public int Padding;
    public int Extension;
    public int CC;
    public int Marker;
    public int PayloadType;
    public int SequenceNumber;
    public int TimeStamp;
    public int Ssrc;


    public byte[] header;

   
    public byte[] payload;


    public RTPModel(int PType, int Framenb, int Time, byte[] data)
    {
        Version = 2;
        Padding = 0;
        Extension = 0;
        CC = 0;
        Marker = 0;
        Ssrc = 0;

        SequenceNumber = Framenb;
        TimeStamp = Time;
        PayloadType = PType;

        
        header = new byte[HEADER_SIZE];
        updateSeq(header, SequenceNumber);
        updateTimeStamp(header,  TimeStamp);
  
        payload = data;
    }
    public static void updateSeq(byte[] header, int SequenceNumber)
    {
        header[2] = (byte)(SequenceNumber);
        header[3] = (byte)(SequenceNumber >> 8);
    }

    public static void updateTimeStamp(byte[] header, int TimeStamp)
    {
        header[4] = (byte)TimeStamp;
        header[5] = (byte)(TimeStamp >> (8));
        header[6] = (byte)(TimeStamp >> (16));
        header[7] = (byte)(TimeStamp >> (24));
    }
    public RTPModel(byte[] parsedata) {
        MemoryStream ms = new MemoryStream(parsedata);
        header=new byte[HEADER_SIZE];
        ms.Read(header, 0, 12);
        Version = 2;
        Padding = 0;
        Extension = 0;
        CC = 0;
        Marker = 0;
        Ssrc = 0;
        PayloadType = header[1]&(0xf0);
        SequenceNumber = 0;
        SequenceNumber=header[2];
        SequenceNumber |= header[3]<<8;
        TimeStamp = header[4];
        TimeStamp |= header[5] << 8;
        TimeStamp |= header[6] << 16;
        TimeStamp |= header[7] << 24;
        Ssrc = 0;
        
        payload = new byte[parsedata.Length - HEADER_SIZE];
        ms.Read(payload, 0, parsedata.Length - HEADER_SIZE);
    }

    public byte[] toBytes() { 
        MemoryStream ms=new MemoryStream();
        ms.Write(this.header,0,this.header.Length);
        ms.Write(this.payload,0,this.payload.Length);
        return ms.ToArray();
    }

}




