using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Media;
using STak.TakEngine;

namespace STak.WinTak
{
    public static class AudioPlayer
    {
        private static string MediaPlayer => UIAppConfig.Appearance.AudioPlayer;


        public static void PlaySound(string soundFile)
        {
            PlaySound(soundFile, Player.None);
        }


        public static void PlaySound(string soundFile, int playerId)
        {
            if (MainWindow.Instance.AudioEnabled)
            {
                string filePath = soundFile;

                if (playerId != Player.None)
                {
                    // Append "_P1" or "_P2" to the base file name.
                    var dirName  = Path.GetDirectoryName(soundFile);
                    var baseName = Path.GetFileNameWithoutExtension(soundFile);
                    var fileName = $"{baseName}_P{playerId+1}{Path.GetExtension(soundFile)}";
                    filePath = Path.Combine(dirName, fileName);
                }

                filePath = App.GetSoundPathName(filePath);

                if (File.Exists(filePath))
                {
                    if (MediaPlayer?.ToLower() == "mediaplayer")
                    {
                        MediaPlayer player = new MediaPlayer();
                        var uri = App.ConvertPathNameToUri(filePath);
                        player.Open(new Uri(uri, UriKind.Absolute));
                        player.Play();
                    }
                    else if (MediaPlayer?.ToLower() == "soundplayer" && OperatingSystem.IsWindows())
                    {
                        SoundPlayer player = new SoundPlayer(filePath);
                        player.Play();
                    }
                }
            }
        }
    }
}
