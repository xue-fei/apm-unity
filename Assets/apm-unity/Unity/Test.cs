using Apm.Media;
using Apm.Media.Dsp.WebRtc;
using System;
using System.Collections.Generic;
using System.IO;
using uMicrophoneWebGL;
using UnityEngine;

public class Test : MonoBehaviour
{
    public MicrophoneWebGL microphoneWebGL;
    WebRtcFilter wrf;
    bool isPlay = false;

    private void Awake()
    {
        microphoneWebGL = GetComponent<MicrophoneWebGL>();
        microphoneWebGL.dataEvent.AddListener(OnData);
        wrf = new WebRtcFilter(240, 100, new AudioFormat(16000), new AudioFormat(16000),
            true, true, true);
    }

    // Start is called before the first frame update
    void Start()
    {
        //// todo: call this when you play frame to speakers
        //wrf.RegisterFramePlayed(....);

        //// todo: call this when you get data from mic before sending to network				
        //wrf.Write(....); // frite signal recorded by microphone
        //bool moreFrames;
        //do
        //{
        //    short[] cancelBuffer = new short[frameSize]; // contains cancelled audio signal
        //    if (wrf.Read(cancelBuffer, out moreFrames))
        //    {
        //        SendToNetwork(cancelBuffer);
        //    }
        //} while (moreFrames);

        microphoneWebGL.Begin();
        isPlay = true;
    }

    const int frameCountByte = 320;
    short[] cancelBuffer = new short[frameCountByte];
    bool moreFrames;

    List<float> ogFloats = new List<float>();
    List<float> ecFloats = new List<float>();

    // Update is called once per frame
    void Update()
    {

    }

    float[] tempFar = new float[160];
    byte[] bytesFar;
    byte[] bytesNear;
    void OnData(float[] data)
    {
        ogFloats.AddRange(data);
        if (farQueue.Count >= 160)
        {
            for (int i = 0; i < tempFar.Length; i++)
            {
                tempFar[i] = farQueue.Dequeue();
            }
            bytesFar = FloatToByte16(tempFar);
            //Debug.Log("bytesFar.Length:" + bytesFar.Length);
            wrf.RegisterFramePlayed(bytesFar);
            bytesNear = FloatToByte16(data);
            wrf.Write(bytesNear); 
            cancelBuffer = new short[frameCountByte];
            if (wrf.Read(cancelBuffer, out moreFrames))
            {
                ecFloats.AddRange(ShortToFloat(cancelBuffer));
                Debug.Log("moreFrames:" + moreFrames);
            }
        }
    }

    Queue<float> farQueue = new Queue<float>();
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (isPlay)
        {
            //Debug.Log(data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                //data[i] = data[i] * 0.3f;
                farQueue.Enqueue(data[i]);
            }
        }
    }

    public byte[] FloatToByte16(float[] floatArray)
    {
        byte[] byteArray = new byte[floatArray.Length * 2];
        int byteIndex = 0;
        foreach (float sample in floatArray)
        {
            short intValue = (short)(Math.Clamp(sample, -1.0f, 1.0f) * short.MaxValue);
            byteArray[byteIndex++] = (byte)(intValue & 0xFF);
            byteArray[byteIndex++] = (byte)((intValue >> 8) & 0xFF);
        }
        return byteArray;
    }

    public float[] ByteToFloat16(byte[] byteArray)
    {
        int sampleCount = byteArray.Length / 2;
        float[] floatArray = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short intValue = (short)((byteArray[i * 2]) | (byteArray[i * 2 + 1] << 8));
            floatArray[i] = Math.Clamp(intValue / 32768f, -1.0f, 1.0f);
        }
        return floatArray;
    }

    private void SaveWav(int channels, int frequency, float[] data, string filePath)
    {
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                // 写入RIFF头部标识
                writer.Write("RIFF".ToCharArray());
                // 写入文件总长度（后续填充）
                writer.Write(0);
                writer.Write("WAVE".ToCharArray());
                // 写入fmt子块
                writer.Write("fmt ".ToCharArray());
                writer.Write(16); // PCM格式块长度
                writer.Write((short)1); // PCM编码类型
                writer.Write((short)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2); // 字节率
                writer.Write((short)(channels * 2)); // 块对齐
                writer.Write((short)16); // 位深度
                                         // 写入data子块
                writer.Write("data".ToCharArray());
                writer.Write(data.Length * 2); // 音频数据字节数
                                               // 写入PCM数据（float转为short）
                foreach (float sample in data)
                {
                    writer.Write((short)(sample * 32767));
                }
                // 返回填充文件总长度
                fileStream.Position = 4;
                writer.Write((int)(fileStream.Length - 8));
            }
        }
    }

    public float[] ReadMono16kWavToFloat(string filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            // 读取WAV文件头
            string riff = new string(reader.ReadChars(4));    // "RIFF"
            int fileSize = reader.ReadInt32();                // 文件总大小-8
            string wave = new string(reader.ReadChars(4));    // "WAVE"
            string fmt = new string(reader.ReadChars(4));     // "fmt "
            int fmtSize = reader.ReadInt32();                 // fmt块大小（至少16）

            // 读取音频格式信息
            short audioFormat = reader.ReadInt16();           // 1=PCM
            short numChannels = reader.ReadInt16();           // 通道数
            int sampleRate = reader.ReadInt32();              // 采样率
            int byteRate = reader.ReadInt32();                // 字节率
            short blockAlign = reader.ReadInt16();            // 块对齐
            short bitsPerSample = reader.ReadInt16();         // 采样深度

            // 验证文件格式
            if (riff != "RIFF" || wave != "WAVE" || fmt != "fmt ")
                throw new Exception("无效的WAV文件头");

            // 跳过fmt块的额外信息（如果有）
            if (fmtSize > 16)
                reader.ReadBytes(fmtSize - 16);

            // 查找数据块
            string dataChunkId;
            do
            {
                dataChunkId = new string(reader.ReadChars(4));
                if (dataChunkId != "data")
                    reader.ReadBytes(reader.ReadInt32()); // 跳过非数据块
            } while (dataChunkId != "data");

            int dataSize = reader.ReadInt32(); // 数据块大小（字节）

            // 验证音频参数
            if (audioFormat != 1)
                throw new Exception("仅支持PCM格式");
            if (numChannels != 1)
                throw new Exception("仅支持单声道音频");
            if (sampleRate != 16000)
                throw new Exception("仅支持16kHz采样率");
            if (bitsPerSample != 16)
                throw new Exception("仅支持16位采样深度");

            // 读取PCM数据并转换为float
            int sampleCount = dataSize / 2; // 16位 = 2字节/样本
            float[] floatData = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                // 小端序读取16位样本
                byte lowByte = reader.ReadByte();
                byte highByte = reader.ReadByte();
                short pcmValue = (short)((highByte << 8) | lowByte);

                // 将16位PCM值转换为[-1.0, 1.0]范围的float
                floatData[i] = pcmValue / 32768.0f;
            }

            return floatData;
        }
    }

    public float[] ShortToFloat(short[] pcmData)
    {
        float[] floatData = new float[pcmData.Length];

        for (int i = 0; i < pcmData.Length; i++)
        {
            floatData[i] = pcmData[i] / 32768.0f;
        }
        return floatData;
    }

    private void OnDestroy()
    {
        isPlay = false;

        SaveWav(1, 16000, ecFloats.ToArray(), Application.dataPath + "/ec.wav");
        SaveWav(1, 16000, ogFloats.ToArray(), Application.dataPath + "/og.wav");
    }
}