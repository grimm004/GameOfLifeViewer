using System;
using System.Xml;
using System.Xml.Serialization;

namespace GameOfLife
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string configFile = "GameOfLife\\config.xml";

            SettingsManager settingsManager = new SettingsManager(configFile);
            settingsManager.LoadSettingsFile();

            using (var game = new GameOfLifeEmulator(settingsManager))
                game.Run();
            Environment.Exit(0);
        }
    }
#endif

    /// <summary>
    /// A class that contains some core constants for the game's mechanics
    /// </summary>
    public class Constants
    {
        // Updates per second to try and change timings for
        public const int MAXFPS = 60;
        public const int MAXGPS = 2;
    }

    public class SettingsManager
    {
        private static Settings settings;
        private readonly string configFile;

        public SettingsManager(string fileName, Settings initialSettings = null)
        {
            configFile = fileName;
            if (initialSettings != null) settings = initialSettings;
        }

        public Settings LoadedSettings { get { return settings; } set { settings = value; } }

        public void LoadDefaultSettings()
        {
            settings = new Settings()
            {
                WIDTH = 1600,
                HEIGHT = 900,
                startPositionX = 0,
                startPositionY = 0,
                targetFps = 60,
                targetGps = 30,
                blockSize = 10,
                cameraSpeed = 5,
                gameSaveFile = "centinal.cells",
                loadFormat = GameLoadFormat.random,
                randomGameWidth = 240,
                randomGameHeight = 135,
                randomGameProbability = 5,
                cellsPadding = 5,
                destroySideCells = true
            };
        }

        public void CreateDefaultSettingsFile()
        {
            LoadDefaultSettings();
            CreateSettingsFile();
        }

        public void CreateSettingsFile()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(settings.GetType());
            XmlWriterSettings xmlSettings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "\t"
            };
            using (var writer = XmlWriter.Create(configFile, xmlSettings))
                xmlSerializer.Serialize(writer, settings);
        }

        public void LoadSettingsFile()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));

            using (var reader = XmlReader.Create(configFile))
                settings = (Settings)xmlSerializer.Deserialize(reader);
        }
    }

    public class Settings
    {
        public int targetFps;
        public int targetGps;
        public string gameSaveFile;
        public int blockSize;
        public int WIDTH;
        public int HEIGHT;
        public int startPositionX;
        public int startPositionY;
        public int cameraSpeed;
        public GameLoadFormat loadFormat;
        public int randomGameWidth;
        public int randomGameHeight;
        public int randomGameProbability;
        public int cellsPadding;
        public bool destroySideCells;
    }

    public enum GameLoadFormat
    {
        lifegame,
        lifegamefile,
        cells,
        cellsfile,
        random
    }
}
