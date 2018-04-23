using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameOfLife
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class GameOfLifeEmulator : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GameOfLifeScreen gameOfLifeScreen;

        SettingsManager settingsManager;

        Text generationsCountText;
        Text fpsCountText;
        Text framesCountText;
        Text gpsCountText;

        public int width;
        public int height;

        Camera camera;

        FrameCounter fpsCounter = new FrameCounter();
        FrameCounter gpsCounter = new FrameCounter();

        public int cellSize;

        public int targetUps;
        public float targetGps;

        public string gameFile;

        public GameOfLifeEmulator(SettingsManager currentSettingsManager)
        {
            settingsManager = currentSettingsManager;
            graphics = new GraphicsDeviceManager(this);
            UpdateSettings();
            Window.Title = "Game of Life";
            Content.RootDirectory = "Content";
            SetResolution(width, height);
        }

        public void SetResolution(int width, int height)
        {
            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = height;
            graphics.ApplyChanges();
        }

        public void UpdateSettings()
        {
            width = settingsManager.LoadedSettings.WIDTH;
            height = settingsManager.LoadedSettings.HEIGHT;
            SetResolution(width, height);
            targetUps = settingsManager.LoadedSettings.targetFps;
            targetGps = settingsManager.LoadedSettings.targetGps;
            SetUps(targetUps);
            cellSize = settingsManager.LoadedSettings.blockSize;
            gameFile = "GameOfLife\\saves\\" + settingsManager.LoadedSettings.gameSaveFile;
            camera = new Camera(Vector2.Zero, new Point(width, height), settingsManager.LoadedSettings.cameraSpeed);
            camera.SetCellPosition(settingsManager.LoadedSettings.startPositionX, settingsManager.LoadedSettings.startPositionY, cellSize);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            SetUps(targetUps);
        }

        public void SetUps(int ups)
        {
            this.IsMouseVisible = true;
            this.TargetElapsedTime = TimeSpan.FromMilliseconds((double)1000d / (double)ups);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            SpriteFont font = Content.Load<SpriteFont>("Arial");

            fpsCountText = new Text(camera, font, Color.Blue)
            {
                start = new Vector2(0)
            };
            framesCountText = new Text(camera, font, Color.Blue)
            {
                start = new Vector2(0, 20)
            };
            gpsCountText = new Text(camera, font, Color.Blue)
            {
                start = new Vector2(150, 0)
            };
            generationsCountText = new Text(camera, font, Color.Blue)
            {
                start = new Vector2(150, 20)
            };
            LoadGame();
            StartGame();
        }

        public void LoadGame()
        {
            settingsManager.LoadSettingsFile();
            UpdateSettings();
            GameOfLife game;
            switch (settingsManager.LoadedSettings.loadFormat)
            {
                case GameLoadFormat.lifegame:
                case GameLoadFormat.lifegamefile:
                    game = LoadGameFile(gameFile, settingsManager.LoadedSettings.destroySideCells);
                    break;
                case GameLoadFormat.cells:
                case GameLoadFormat.cellsfile:
                    game = LoadCellsFile(gameFile, settingsManager.LoadedSettings.cellsPadding, settingsManager.LoadedSettings.destroySideCells);
                    break;
                case GameLoadFormat.random:
                default:
                    game = LoadRandomGame(new Point(settingsManager.LoadedSettings.randomGameWidth, settingsManager.LoadedSettings.randomGameHeight), settingsManager.LoadedSettings.randomGameProbability, settingsManager.LoadedSettings.destroySideCells);
                    break;
            }
            gameOfLifeScreen = new GameOfLifeScreen(camera, graphics, game, new Vector2(cellSize, cellSize));
        }

        public GameOfLife LoadRandomGame(Point size, int oneInChance, bool destroySideCells)
        {
            GameOfLife game = new GameOfLife(size.X, size.Y);
            Random random = new Random();
            for (int x = 0; x < game.SizeX; x++) for (int y = 0; y < game.SizeY; y++)
                    if (random.Next(0, oneInChance) == 0) game.ToggleCell(x, y);
            SaveGame(game);
            return game;
        }

        public void SaveGame(GameOfLife game)
        {
            List<Point> coordsList = new List<Point>();
            for (int x = 0; x < game.SizeX; x++) for (int y = 0; y < game.SizeY; y++)
                    if (game[x, y]) coordsList.Add(new Point(x, y));

            string fileName = GetDateTimeFileName("GameOfLife\\saves\\", namePrefix: "LifeGame_", applicationExtention: ".lifegame");
            Console.WriteLine("Saving game to file '{0}'...", fileName);

            System.IO.StreamWriter file = new System.IO.StreamWriter(fileName);
            file.WriteLine("0 0");
            foreach (Point coords in coordsList) file.WriteLine(string.Format("{0} {1}", coords.X, coords.Y));
            file.Close();

            Console.WriteLine("Saved");
        }

        public string GetRandomFileName()
        {
            Random random = new Random();
            string hexString = "0123456789abcdef";
            string fileName = "F:\\Users\\Max Grimmett\\Desktop\\";
            for (int i = 0; i < 10; i++) fileName += hexString[random.Next(hexString.Length)];
            fileName += ".lifegame";
            return fileName;
        }

        public string GetDateTimeFileName(string fileLocation = "", string namePrefix = "", string dateTimeFormat = "dd-MM-yyyy_HH-mm-ss", string dateTimeAndRandomSpacer = "_", string randCharString = "0123456789abcdef", int randCharCount = 5, string applicationExtention = "")
        {
            Random random = new Random();
            string hexString = randCharString;
            string fileName = fileLocation;
            fileName += namePrefix;
            fileName += DateTime.Now.ToString(dateTimeFormat);
            fileName += dateTimeAndRandomSpacer;
            for (int i = 0; i < randCharCount; i++) fileName += hexString[random.Next(hexString.Length)];
            fileName += applicationExtention;
            return fileName;
        }

        public GameOfLife LoadGameFile(string gameFileName, bool destroySideCells)
        {
            string currentLine;
            List<int> xCoords = new List<int>();
            List<int> yCoords = new List<int>();
            List<Point> coordsList = new List<Point>();
            System.IO.StreamReader file = new System.IO.StreamReader(gameFileName);
            string[] offsetString = file.ReadLine().Split(' ');
            int offsetX = Convert.ToInt32(offsetString[0]);
            int offsetY = Convert.ToInt32(offsetString[1]);
            while ((currentLine = file.ReadLine()) != null)
            {
                string[] coordinateStrings = currentLine.Split(' ');
                int x = Convert.ToInt32(coordinateStrings[0]) + offsetX;
                int y = Convert.ToInt32(coordinateStrings[1]) + offsetY;
                xCoords.Add(x);
                yCoords.Add(y);
                coordsList.Add(new Point(x, y));
            }
            file.Close();

            GameOfLife game = new GameOfLife(xCoords.Max() + 2, yCoords.Max() + 2);
            SetUpGame(game, coordsList.ToArray());

            return game;
        }

        public GameOfLife LoadCellsFile(string cellsFileName, int cellsPadding, bool destroySideCells)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(cellsFileName);
            string nameString = file.ReadLine();
            string separatorString = file.ReadLine();
            List<int> lineLengths = new List<int>();
            List<Point> coordsList = new List<Point>();
            int y = 0;
            string currentLine;
            while ((currentLine = file.ReadLine()) != null)
            {
                lineLengths.Add(currentLine.Length);

                for (int x = 0; x < currentLine.Length; x++)
                {
                    if (currentLine[x] == 'O') coordsList.Add(new Point(cellsPadding + x, cellsPadding + y));
                }
                if (currentLine != "") y++;
            }
            file.Close();

            int xSize = lineLengths.Max() + (2 * cellsPadding);
            int ySize = y + (2 * cellsPadding);

            GameOfLife game = new GameOfLife(xSize, ySize);
            SetUpGame(game, coordsList.ToArray());

            return game;
        }

        public void SetUpGame(GameOfLife game, Point[] coordsList)
        {
            foreach (Point coords in coordsList) game.ToggleCell(coords.X, coords.Y);
        }

        public void StartGame()
        {
            gameOfLifeScreen.gameOfLife.BeginGeneration();
            gameOfLifeScreen.gameOfLife.Wait();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent() { }

        bool doGeneration = false;
        double timeCount = 0;

        bool restartGenerationSwitch = false;
        bool saveGameSwitch = false;

        int generationsPerUpdate = 1;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            MouseState mouseState = Mouse.GetState();
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Escape)) Exit();

            if (keyboardState.IsKeyDown(Keys.Space) && restartGenerationSwitch)
            {
                restartGenerationSwitch = false;
                totalFrames = 0;
                LoadGame();
                StartGame();
            }
            else if (!keyboardState.IsKeyDown(Keys.Space)) restartGenerationSwitch = true;

            if (keyboardState.IsKeyDown(Keys.LeftControl) && keyboardState.IsKeyDown(Keys.S) && saveGameSwitch)
            {
                saveGameSwitch = false;
                SaveGame(gameOfLifeScreen.gameOfLife);
            }
            else if (!keyboardState.IsKeyDown(Keys.S)) saveGameSwitch = true;

            camera.Update(keyboardState);

            timeCount += gameTime.ElapsedGameTime.TotalMilliseconds;
            doGeneration = timeCount >= 1000d / (double)targetGps;

            if (doGeneration)
            {
                if (targetUps < targetGps) generationsPerUpdate = (int)(targetGps / targetUps);
                gpsCounter.Update((float)((timeCount / (1000d * (double)generationsPerUpdate))));
                for (int i = 0; i < generationsPerUpdate; i++)
                {
                    gameOfLifeScreen.gameOfLife.Update();
                    gameOfLifeScreen.gameOfLife.Wait();
                    timeCount = 0;
                }
            }

            fpsCountText.text = string.Format("FPS: {0:0.0}", fpsCounter.AverageFramesPerSecond);
            framesCountText.text = string.Format("Frames: {0}", totalFrames);
            gpsCountText.text = string.Format("GPS: {0:0.0}", gpsCounter.AverageFramesPerSecond);
            generationsCountText.text = string.Format("Generations: {0}", gameOfLifeScreen.gameOfLife.Generation);

            gameOfLifeScreen.Update(gameTime, mouseState, keyboardState);

            base.Update(gameTime);
        }

        int totalFrames = 0;

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            fpsCounter.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            GraphicsDevice.Clear(gameOfLifeScreen.backgroundColour);

            spriteBatch.Begin();
            gameOfLifeScreen.Draw(spriteBatch);
            fpsCountText.Draw(spriteBatch);
            framesCountText.Draw(spriteBatch);
            gpsCountText.Draw(spriteBatch);
            generationsCountText.Draw(spriteBatch);
            spriteBatch.End();

            totalFrames++;
            base.Draw(gameTime);
        }
    }

    public class Camera
    {
        public Vector2 start;
        public readonly int SCREENWIDTH;
        public readonly int SCREENHEIGHT;
        public float velocity;

        public Camera(Vector2 startPosition, Point screenSize, float initialVelocity)
        {
            start = startPosition;
            velocity = initialVelocity;
            SCREENWIDTH = screenSize.X;
            SCREENHEIGHT = screenSize.Y;
        }

        public void SetPosition(Vector2 position)
        {
            start = -position;
        }
        public void SetPosition(float x, float y)
        {
            start = new Vector2(x, y);
        }
        public void SetPosition(int x, int y)
        {
            start = new Vector2(x, y);
        }
        public void SetCellPosition(Vector2 position, int cellSize)
        {
            start = position * cellSize;
        }
        public void SetCellPosition(int x, int y, int cellSize)
        {
            start = new Vector2(x * cellSize, y * cellSize);
        }
        public void SetCellPosition(float x, float y, int cellSize)
        {
            start = new Vector2(x * cellSize, y * cellSize);
        }

        public void Update(KeyboardState keyboardState)
        {
            if ((keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A)) && (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))) { }
            else if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A)) start.X -= velocity;
            else if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D)) start.X += velocity;
            if ((keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W)) && (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S))) { }
            else if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W)) start.Y -= velocity;
            else if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S)) start.Y += velocity;
        }
    }

    public class GameOfLifeScreen : Screen
    {
        public GameOfLife gameOfLife;
        public Texture2D texture;

        public GameOfLifeScreen(Camera currentCamera, GraphicsDeviceManager graphics, GameOfLife game, Vector2 blockSize) : base(currentCamera)
        {
            gameOfLife = game;
            Point gameOfLifeSize = new Point(gameOfLife.SizeX, gameOfLife.SizeY);
            texture = new Texture2D(graphics.GraphicsDevice, (int)blockSize.X, (int)blockSize.Y);
            Color[] colors = new Color[texture.Width * texture.Height];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.White;
            backgroundColour = Color.Gray;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            for (int x = 0; x < gameOfLife.SizeX; x++)
                for (int y = 0; y < gameOfLife.SizeY; y++)
                    spriteBatch.Draw(texture, new Vector2(texture.Width * x, texture.Height * y) - camera.start, gameOfLife[x, y] ? Color.Black : Color.White);
            base.Draw(spriteBatch);
        }
    }

    /// <summary>
    /// A class that contains all the core functionality for a window
    /// </summary>
    public abstract class Screen
    {
        public Camera camera;
        // Define the background colour for the screen
        public Color backgroundColour = Color.White;
        // Define the list of ScreenItem objects (Text, Buttons, Etc)
        public List<ScreenItem> screenItems = new List<ScreenItem>();
        // Define the eneitiy rendering and update boolean variables
        public bool renderEntities = false;
        public bool updateEntities = false;

        public Screen(Camera currentCamera)
        {
            camera = currentCamera;
        }

        /// <summary>
        /// Draw all the screen items to the window.
        /// </summary>
        /// <param name="spriteBatch">The active spritebatch used to draw the screen items.</param>
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            // Loop through the screen items
            foreach (ScreenItem item in screenItems)
            {
                // Draw each item
                item.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Update all the screen items.
        /// </summary>
        /// <param name="mouseState">Information about the mouse and its buttons.</param>
        /// <param name="keyboardState">Information about the keyboard and its keys.</param>
        public virtual void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {
            // Loop through the screen items
            foreach (ScreenItem item in screenItems)
            {
                // Update each item
                item.Update(gameTime, mouseState, keyboardState);
            }
        }

        public void AddItem(ScreenItem item, bool isColourable = false)
        {
            screenItems.Add(item);
        }
    }

    /// <summary>
    /// A class that contains all the core functionality of a screen item
    /// </summary>
    public abstract class ScreenItem
    {
        public Camera camera;

        public ScreenItem(Camera currentCamera, Screen screen = null)
        {
            camera = currentCamera;
            if (screen != null)
            {
                AddToScreen(screen);
            }
        }

        public void AddToScreen(Screen screen)
        {
            screen.AddItem(this);
        }

        /// <summary>
        /// Draw the screen item to the window
        /// </summary>
        /// <param name="spriteBatch">Used to draw the item(s).</param>
        public abstract void Draw(SpriteBatch spriteBatch);
        /// <summary>
        /// Update the screen item.
        /// </summary>
        /// <param name="mouseState">Provide information about the mouse, and its buttons.</param>
        /// <param name="keyboardState">Provide information about the keyboard, and its keys.</param>
        public abstract void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState);
    }

    /// <summary>
    /// A class contains all the core functionality of a text 'label', that outputs text to the window
    /// </summary>
    public class Text : ScreenItem
    {
        // Define the variable to store the text to be displayed
        public string text;
        // Define the variable to store the font to be displayed
        public SpriteFont font;
        // Define the coordinates for the text to start at
        public Vector2 start;
        // Define the size of the text
        Vector2 size;
        // Define the colour of the text
        public Color colour;

        public Vector2 Size { get { size = font.MeasureString(text); return size; } }

        public bool doDraw = true;

        public void Show()
        {
            doDraw = true;
        }

        public void Hide()
        {
            doDraw = false;
        }

        /// <summary>
        /// Construct the text item
        /// </summary>
        /// <param name="outputText">The text to be outputted to the window.</param>
        /// <param name="textFont">The font object used to render the text.</param>
        /// <param name="textStart">The starting coordinates of the text.</param>
        /// <param name="textColour">The colour of the text.</param>
        public Text(Camera currentCamera, string outputText, SpriteFont textFont, Vector2 textStart, Color textColour, Screen screen = null) : base(currentCamera, screen)
        {
            text = outputText;
            font = textFont;
            start = textStart;
            colour = textColour;
            size = font.MeasureString(text);
        }

        public Text(Camera currentCamera, string outputText, SpriteFont textFont, Color textColour, Screen screen = null) : base(currentCamera, screen)
        {
            text = outputText;
            font = textFont;
            start = Vector2.Zero;
            colour = textColour;
            size = font.MeasureString(text);
        }

        public Text(Camera currentCamera, string outputText, SpriteFont textFont, Screen screen = null) : base(currentCamera, screen)
        {
            text = outputText;
            font = textFont;
            start = Vector2.Zero;
            colour = Color.Black;
            size = font.MeasureString(text);
        }

        public Text(Camera currentCamera, SpriteFont textFont, Color textColour, Screen screen = null) : base(currentCamera, screen)
        {
            text = "";
            font = textFont;
            start = Vector2.Zero;
            colour = textColour;
            size = font.MeasureString(text);
        }

        public Text(Camera currentCamera, SpriteFont textFont, Screen screen = null) : base(currentCamera, screen)
        {
            text = "";
            font = textFont;
            start = Vector2.Zero;
            colour = Color.Black;
            size = font.MeasureString(text);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Draw the text to the window
            if (doDraw) spriteBatch.DrawString(font, text, start + camera.start, colour);
        }

        public override void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState) { }
    }

    /// <summary>
    /// This class contains all the core attributes to create useful, renderable rectangles
    /// that can be drawn to the screen.
    /// </summary>
    public class ColouredRectangle : ScreenItem
    {
        // Define the texture for the rectangle
        Texture2D rect;
        // Define the start coordinates of the rectangle
        public Vector2 coordinates;
        // Define the size of the rectangle
        public Vector2 size;
        // Define the colour of the rectangle
        public Color colour;

        public bool Visible { get { return camera.start.X < X && camera.start.Y < Y && X < camera.start.X + camera.SCREENWIDTH && Y < camera.start.Y + camera.SCREENHEIGHT; } }

        /// <summary>
        /// Construct the coloured rectangle.
        /// </summary>
        /// <param name="startCoords">The start coordinates of the rectangle.</param>
        /// <param name="rectSize">The size of the rectangle.</param>
        /// <param name="rectColour">The colour of the rectangle.</param>
        public ColouredRectangle(Camera currentCamera, Texture2D texture, Vector2 startCoords, Color rectColour) : base(currentCamera)
        {
            // Construct the above variables
            coordinates = startCoords;
            size = new Vector2(texture.Width, texture.Height);
            rect = texture;
            colour = rectColour;

            // Create an array of colour objects (representing each pixel)
            Color[] data = new Color[(int)(size.X * size.Y)];
            // For each pixel fill it with the defined colour
            for (int i = 0; i < data.Length; ++i) data[i] = colour;
            // Set the texture to the colour data
            rect.SetData(data);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Draw the rectangle
            if (Visible) spriteBatch.Draw(rect, coordinates - camera.start, colour);
        }

        public override void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {
            //MouseIntersecing = (Left < mouseState.X && mouseState.X < Right && Top < mouseState.Y && mouseState.Y < Bottom);
        }

        public float X { get { return coordinates.X; } set { coordinates.X = value; } }
        public float Y { get { return coordinates.Y; } set { coordinates.Y = value; } }
        public float Width { get { return size.X; } }
        public float Height { get { return size.Y; } }

        // Define variaous properties which are calculated on request
        public float Left { get { return X; } set { X = value; } }
        public float Right { get { return X + Width; } set { X = value - Width; } }
        public float Top { get { return Y; } set { Y = value; } }
        public float Bottom { get { return Y + size.Y; } set { Y = value - Height; } }
        public float CentreX { get { return X + (Width / 2); } set { X = value - (Width / 2); } }
        public float CentreY { get { return Y + (Height / 2); } set { Y = value - (Height / 2); } }
        public Vector2 Centre { get { return new Vector2(CentreX, CentreY); } set { CentreX = value.X; CentreY = value.Y; } }
    }
}
