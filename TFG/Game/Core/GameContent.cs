
namespace Core
{
    public static class GameContent
    {
        public const string BACKGROUNDS_PATH = "Backgrounds/";
        public const string ANIMATIONS_PATH  = "../../../Content/Animations/";
        public const string TEXTURES_PATH    = "Sprites/";
        public const string SOUNDS_PATH      = "Sounds/";
        public const string MUSIC_PATH       = "Music/";
        public const string FONTS_PATH       = "Fonts/";

        public static string BackgroundPath(string name)
        {
            return BACKGROUNDS_PATH + name;
        }

        public static string AnimationPath(string name)
        {
            return ANIMATIONS_PATH + name;
        }

        public static string TexturePath(string name)
        { 
            return TEXTURES_PATH + name; 
        }

        public static string SoundPath(string name)
        {
            return SOUNDS_PATH + name;
        }

        public static string MusicPath(string name)
        {
            return MUSIC_PATH + name;
        }

        public static string FontPath(string name)
        {
            return FONTS_PATH + name;
        }
    }
}
