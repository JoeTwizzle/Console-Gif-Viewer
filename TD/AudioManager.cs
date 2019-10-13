using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TD
{
    public class AudioManager
    {
        public WaveOutEvent outputDevice;
        public void PlayAsync()
        {
            if (outputDevice == null)
            {
                outputDevice = new WaveOutEvent();
            }
            Task.Run(() =>
            {
                using (Mp3FileReader audioFile = new Mp3FileReader(Program.settings.AudioPath))
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(1000);
                    }
                }
            });
        }

        public void Stop()
        {
            outputDevice.Stop();
        }
    }
}
