using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace TetrisDemo
{
    public static class SoundManager
    {
        private static Song gameMusic;

        public static void Initialize(ContentManager content)
        {
            try
            {
                gameMusic = content.Load<Song>("gameMusic");
            }
            catch
            {
                Debug.Write("SoundManager Initialization Failed");
            }
        }

        public static void PlayGameMusic()
        {
            MediaPlayer.Play(gameMusic);
        }

    }


}
