using Apm.Media;
using Apm.Media.Dsp.WebRtc;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //var enhancer = new WebRtcFilter(240, 100, new AudioFormat(16000), new AudioFormat(16000),
        //    true, true, true);

        //// todo: call this when you play frame to speakers
        //enhancer.RegisterFramePlayed(....);


        //// todo: call this when you get data from mic before sending to network				
        //enhancer.Write(....); // frite signal recorded by microphone
        //bool moreFrames;
        //do
        //{
        //    short[] cancelBuffer = new short[frameSize]; // contains cancelled audio signal
        //    if (enhancer.Read(cancelBuffer, out moreFrames))
        //    {
        //        SendToNetwork(cancelBuffer);
        //    }
        //} while (moreFrames);
    }

    // Update is called once per frame
    void Update()
    {

    }
}