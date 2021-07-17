using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGameLauncher
{
    class Constants : Application
    {

        /// <summary>
        /// Core game info
        /// </summary>
        public static readonly string GAME_TITLE = "Y'ar Guard";
        public static readonly string LAUNCHER_NAME = "Game Launcher";
        public static readonly string GAME_EXE = "yar_guard";

        /// <summary>
        /// Paths & urls
        /// </summary>
        public static readonly string DESTINATION_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), GAME_TITLE);
        public static readonly string ZIP_PATH = Path.Combine(DESTINATION_PATH, GAME_TITLE + ".zip");
        public static readonly string GAME_EXECUTABLE_PATH = Path.Combine(DESTINATION_PATH, GAME_EXE + ".exe");

        public static readonly string VERSION_URL = "https://lucianorasente.com/downloads/games/yar_guard/version.txt";
        public static readonly string APPLICATION_ICON_URL = "https://lucianorasente.com/downloads/games/yar_guard/favicon.ico";            // Needs to be .ico
        public static readonly string LOGO_URL = "https://lucianorasente.com/downloads/games/yar_guard/logo_283x75.png";                    // Ideally around 283x75
        public static readonly string BACKGROUND_URL = "https://lucianorasente.com/downloads/games/yar_guard/yarguard.png";
        public static readonly string PATCH_NOTES_URL = "https://temsoft.io/temsoft_assets/updates.xml";
        public static readonly string CLIENT_DOWNLOAD_URL = "https://lucianorasente.com/downloads/games/yar_guard/game.zip";

        /// <summary>
        /// Navigation bar buttons
        /// </summary>
        public static readonly string NAVBAR_BUTTON_1_TEXT = "Game Website";
        public static readonly string NAVBAR_BUTTON_1_URL = "https://forjagames.itch.io/yar-guard";
        public static readonly string NAVBAR_BUTTON_2_TEXT = "Forja Games";
        public static readonly string NAVBAR_BUTTON_2_URL = "http://forjagames.com";
        public static readonly string NAVBAR_BUTTON_3_TEXT = "Community";
        public static readonly string NAVBAR_BUTTON_3_URL = "https://itch.io/profile/forjagames";
        public static readonly string NAVBAR_BUTTON_4_TEXT = "Github";
        public static readonly string NAVBAR_BUTTON_4_URL = "https://github.com/forjagames";
        public static readonly string NAVBAR_BUTTON_5_TEXT = "Twitter";
        public static readonly string NAVBAR_BUTTON_5_URL = "https://twitter.com/forjagames";

        // Modify this array if you're adding or removing a button
        public static readonly string[] NAVBAR_BUTTON_TEXT_ARRAY = {NAVBAR_BUTTON_1_TEXT, NAVBAR_BUTTON_2_TEXT, NAVBAR_BUTTON_3_TEXT,
                                                                    NAVBAR_BUTTON_4_TEXT, NAVBAR_BUTTON_5_TEXT };

        /// <summary>
        /// Settings
        /// </summary>
        public static bool CACHE_IMAGES = true;
        public static int PANEL_ALPHA = 225;
        public static bool SHOW_VERSION_TEXT = true;
        public static bool AUTOMATICALLY_BEGIN_UPDATING = false;
        public static bool AUTOMATICALLY_LAUNCH_GAME_AFTER_UPDATING = false;
        public static bool SHOW_ERROR_BOX_IF_PATCH_NOTES_DOWNLOAD_FAILS = true;

    }
}
