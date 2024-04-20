
namespace Core
{
    public static class GameContent
    {
        public const string TEXTURES_PATH = "Sprites/";
        public const string SOUNDS_PATH   = "Sounds/";
        public const string FONTS_PATH    = "Fonts/";

        public static string TexturePath(string name)
        { 
            return TEXTURES_PATH + name; 
        }

        public static string SoundPath(string name)
        {
            return SOUNDS_PATH + name;
        }

        public static string FontPath(string name)
        {
            return FONTS_PATH + name;
        }
    }
}
