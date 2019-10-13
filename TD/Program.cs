using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGE;
using SharpDX;
using SharpDX.DirectInput;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime;
using System.Runtime.InteropServices;
using NAudio;
using NAudio.Wave;
using System.Threading;
using Newtonsoft.Json;
using System.IO;

namespace TD
{
    struct Settings
    {
        public bool LoopAudio;
        public int Width;
        public int Height;
        public int PixelX;
        public int PixelY;
        public string GifPath;
        public string AudioPath;
    }
    class Program
    {
        public static Settings settings;
        static void Main(string[] args)
        {
            GenerateSettings();
            settings = LoadSettings();
            Game1 game = new Game1();
            game.Run(new ConsoleRenderer("Consolas", settings.Width, settings.Height, settings.PixelX, settings.PixelY));
        }

        static Settings LoadSettings()
        {
            string content = "";
            using (StreamReader reader = new StreamReader(new FileStream("Settings.json", FileMode.Open)))
            {
               content = reader.ReadToEnd();
            }
            Settings settings = JsonConvert.DeserializeObject<Settings>(content);
            return settings;
        }

        static void GenerateSettings()
        {
            if (!File.Exists("Settings.json"))
            {
                string json = JsonConvert.SerializeObject(new Settings() { LoopAudio = false, Width = 120, Height = 67, PixelX = 10, PixelY = 10, AudioPath = "Resources/TD_Song.mp3", GifPath = "Resources/TD_Full.gif" }, Formatting.Indented);
                using (StreamWriter writer = new StreamWriter(new FileStream("Settings.json", FileMode.OpenOrCreate)))
                {
                    writer.Write(json);
                }
            }
        }
    }
    class Game1 : Game
    {
        AudioManager audioManager;
        Image gif;
        Bitmap[] GetFrames(Image originalImg)
        {
            int numberOfFrames = originalImg.GetFrameCount(FrameDimension.Time);
            Bitmap[] frames = new Bitmap[numberOfFrames];

            for (int i = 0; i < numberOfFrames; i++)
            {
                originalImg.SelectActiveFrame(FrameDimension.Time, i);
                frames[i] = ((Bitmap)originalImg.Clone());
            }

            return frames;
        }
        TGE.Color[] colors;

        public override void Initialize()
        {
            audioManager = new AudioManager();
            Console.CursorVisible = false;
            Console.Title = "Gangster's paradise";
            Screen.Clear();
            Screen.Draw("Loading...", Screen.Width / 2 - 5, Screen.Height / 2, 15);
            Screen.Print();
            gif = Image.FromFile(Program.settings.GifPath);
            colors = ColorChanger.GetPalette().colors;
        }


        public override void Start()
        {
            imageCache = new ImageFrame[gif.GetFrameCount(FrameDimension.Time)];
            imageCache[index] = new ImageFrame();
            gif.SelectActiveFrame(FrameDimension.Time, index);
            imageCache[index].SetFrameData((Bitmap)gif);
            audioManager.PlayAsync();
        }
        float elapsed = 0f;
        int index = 0;
        ImageFrame[] imageCache;
        bool ImageChanged = true;
        public override void Update()
        {
            var image = imageCache[index];
            elapsed += DeltaTime;
            ImageChanged = false;
            if (elapsed >= ImageFrame.frameDelay[index])
            {
                elapsed = 0;
                index++;
                if (index >= gif.GetFrameCount(FrameDimension.Time))
                {
                    index = 0;
                    if (!Program.settings.LoopAudio)
                    {
                        audioManager.Stop();
                        audioManager.PlayAsync();
                    }
                    else
                    {
                        if (audioManager.outputDevice.PlaybackState != PlaybackState.Playing)
                        {
                            audioManager.PlayAsync();
                        }
                    }
                }
                if (imageCache[index] == null)
                {
                    imageCache[index] = new ImageFrame();
                    gif.SelectActiveFrame(FrameDimension.Time, index);
                    imageCache[index].SetFrameData((Bitmap)gif);
                }
                ImageChanged = true;
                ColorChanger.SetPalette(new Palette() { colors = imageCache[index].ColorTable });

            }
        }
        public override void Draw()
        {
            if (ImageChanged)
            {
                var image = imageCache[index];
                Screen.Clear();
                int width = image.Width;
                int height = image.Height;
                Parallel.For(0, height, y =>
                {
                    Parallel.For(0, width, x =>
                    {
                        Screen.Draw('█', x, y, image.Pixels[x, y]);
                    });
                });
                Screen.Print();
            }
        }
    }

}
