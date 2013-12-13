using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace TetrisDemo
{
    public enum PlaceStates
    {
        CAN_PLACE,
        BLOCKED,
        OFFSCREEN
    }

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        const int BoardWidth = 10;  // Board width in blocks
        const int BoardHeight = 20; // Board height in blocks
        const int BlockSize = 20;   // Block size in pixels

        enum GameStates { TitleScreen, Playing, GameOver };
        GameStates gameState = GameStates.TitleScreen;
        Texture2D titleScreen;
        Texture2D spriteSheet;
        Score score;
        SpriteFont pericles14;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        List<int[,]> pieces;

        int[,] Board;
        int[,] SpawnedPiece;
        Vector2 SpawnedPieceLocation;
        Vector2 BoardLocation;
        private Vector2 scoreLocation = new Vector2(100, 110);



        int StepTime = 300;  // Time step between updates in ms
        int ElapsedTime = 0;  // Total elapsed time since the last update
        int KeyBoardElapsedTime = 0;  // Total elapsed time since handling the last keypress
        private float titleScreenTimer = 0f;
        private float titleScreenDelayTime = 1f;

        Random rand = new Random(System.Environment.TickCount);  // Seed a new random number generator

        Color[] TetronimoColors = {
                                    Color.Transparent,  /* 0 */
                                    Color.Orange,       /* 1 */
                                    Color.Blue,         /* 2 */
                                    Color.Red,          /* 3 */
                                    Color.LightSkyBlue, /* 4 */
                                    Color.Yellow,       /* 5 */
                                    Color.Magenta,      /* 6 */
                                    Color.LimeGreen     /* 7 */
                                  };



        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            Board = new int[BoardWidth, BoardHeight];
            ElapsedTime = 0;

            score = new Score();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            pieces = new List<int[,]>();

            /* I Piece */
            pieces.Add(new int[4, 4] { 
                                         {0, 0, 0, 0},
                                         {1, 1, 1, 1},
                                         {0, 0, 0, 0},
                                         {0, 0, 0, 0}
                                     });

            /* J Piece */
            pieces.Add(new int[3, 3] { 
                                         {0, 0, 1},
                                         {1, 1, 1},
                                         {0, 0, 0}
                                     });

            /* O Piece */
            pieces.Add(new int[2, 2] { 
                                         {1, 1},
                                         {1, 1}
                                     });

            /* S Piece */
            pieces.Add(new int[3, 3] { 
                                         {0, 1, 1},
                                         {1, 1, 0},
                                         {0, 0, 0}
                                     });


            /* T Piece */
            pieces.Add(new int[3, 3] { 
                                         {0, 1, 0},
                                         {1, 1, 1},
                                         {0, 0, 0}
                                     });

            /* Z Piece */
            pieces.Add(new int[3, 3] { 
                                         {1, 1, 0},
                                         {0, 1, 1},
                                         {0, 0, 0}
                                     });


            // Reset the board
            InitializeBoard(Board);
            SpawnPiece();


            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);


            // TODO: use this.Content to load your game content here
            spriteSheet = Content.Load<Texture2D>(@"TetrisSprites");
            titleScreen = Content.Load<Texture2D>(@"tetristitle11");
            pericles14 = Content.Load<SpriteFont>("Pericles14");



            SoundManager.Initialize(Content);
            SoundManager.PlayGameMusic();


        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        public void InitializeBoard(int[,] board)
        {
            BoardLocation = new Vector2(20, 20);

            // Reset all board grid locations to empty
            for (int x = 0; x < BoardWidth; x++)
                for (int y = 0; y < BoardHeight; y++)
                {
                    board[x, y] = 0;
                }
        }

        // Create a new piece
        public void SpawnPiece()
        {
            int colr = rand.Next(0, pieces.Count);

            SpawnedPiece = (int[,])pieces[colr].Clone();

            int dim = SpawnedPiece.GetLength(0);

            for (int x = 0; x < dim; x++)
                for (int y = 0; y < dim; y++)
                    SpawnedPiece[x, y] *= (colr + 1);

            SpawnedPieceLocation = Vector2.Zero; // Temporary
        }


        // Checks to see if piece can be placed at location x,y on the board
        // Returns PlaceStates.CAN_PLACE if it can exist there, otherwise reports a reason why it cannot
        public PlaceStates CanPlace(int[,] board, int[,] piece, int x, int y)
        {
            // First we'll need to know the dimensions of the piece
            // Since they are square it is sufficient to just get the dimension of the first row
            int dim = piece.GetLength(0);

            // All pieces are square, so let's use a nested loop to iterate through all the cells of the piece
            for (int px = 0; px < dim; px++)
                for (int py = 0; py < dim; py++)
                {
                    // Calculate where on the game board this segment should be placed
                    int coordx = x + px;
                    int coordy = y + py;

                    // Is this space empty?
                    if (piece[px, py] != 0)
                    {
                        // If the board location would be too far to the left or right then
                        // we are hitting a wall
                        if (coordx < 0 || coordx >= BoardWidth)
                            return PlaceStates.OFFSCREEN;

                        // If even one segment can't be placed because it is being blocked then
                        // we need to return the BLOCKED state
                        if (coordy >= BoardHeight || board[coordx, coordy] != 0)
                        {
                            return PlaceStates.BLOCKED;
                        }
                    }
                }

            // If we get this far we can place the piece!
            return PlaceStates.CAN_PLACE;

        }

        // Permanently write piece to the game board 
        // Note that this method assumes that the piece can actually be placed already and does not recheck
        // to make sure the piece can be placed
        public void Place(int[,] board, int[,] piece, int x, int y)
        {
            int dim = piece.GetLength(0);

            for (int px = 0; px < dim; px++)
                for (int py = 0; py < dim; py++)
                {
                    int coordx = x + px;
                    int coordy = y + py;

                    if (piece[px, py] != 0)
                    {
                        board[coordx, coordy] = piece[px, py];
                        score.PlayerScore += 0;
                    }

                }

            RemoveCompleteLines(board);

        }

        // Rotate the piece (this style of rotation for Tetris is called a super rotation as it doesn't follow the conventional
        // style of piece rotations)
        public int[,] Rotate(int[,] piece, bool left)
        {
            int dim = piece.GetLength(0);
            int[,] npiece = new int[dim, dim];

            for (int i = 0; i < dim; i++)
                for (int j = 0; j < dim; j++)
                {
                    if (left)
                        npiece[j, i] = piece[i, dim - 1 - j];
                    else
                        npiece[j, i] = piece[dim - 1 - i, j];
                }

            return npiece;
        }

        // Check to see if there are any already completed lines on the board, if yes remove them
        public void RemoveCompleteLines(int[,] board)
        {
            for (int y = BoardHeight - 1; y >= 0; y--)
            {
                bool isComplete = true;
                for (int x = 0; x < BoardWidth; x++)
                {
                    if (board[x, y] == 0)
                    {
                        isComplete = false;
                    }
                }
                if (isComplete)
                {
                    // Row y needs to go bye bye
                    // Copy row y-1 to row y
                    for (int yc = y; yc > 0; yc--)
                    {
                        for (int x = 0; x < 10; x++)
                        {
                            board[x, yc] = board[x, yc - 1];
                        }
                    }

                    // Recheck this row
                    y++;

                    // Score += 100;
                }
            }
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here
            ElapsedTime += gameTime.ElapsedGameTime.Milliseconds;
            KeyBoardElapsedTime += gameTime.ElapsedGameTime.Milliseconds;

            switch (gameState)
            {
                case GameStates.TitleScreen:
                    titleScreenTimer +=
                        (float)gameTime.ElapsedGameTime.TotalSeconds;

                    if (titleScreenTimer >= titleScreenDelayTime)
                    {
                        if ((Keyboard.GetState().IsKeyDown(Keys.Space)))
                        {
                            gameState = GameStates.Playing;
                        }
                    }
                    break;
            }

            KeyboardState ks = Keyboard.GetState();

            if (KeyBoardElapsedTime > 200)
            {
                if (ks.IsKeyDown(Keys.Left) || ks.IsKeyDown(Keys.Right))
                {
                    // Create a new location that contains where we WANT to move the piece
                    Vector2 NewSpawnedPieceLocation = SpawnedPieceLocation + new Vector2(ks.IsKeyDown(Keys.Left) ? -1 : 1, 0);

                    // Next, check to see if we can actually place the piece there
                    PlaceStates ps = CanPlace(Board, SpawnedPiece, (int)NewSpawnedPieceLocation.X, (int)NewSpawnedPieceLocation.Y);
                    if (ps == PlaceStates.CAN_PLACE)
                    {
                        SpawnedPieceLocation = NewSpawnedPieceLocation;
                    }

                    KeyBoardElapsedTime = 0;
                }

                if (ks.IsKeyDown(Keys.Up))
                {
                    int[,] newSpawnedPiece = Rotate(SpawnedPiece, true);

                    PlaceStates ps = CanPlace(Board, newSpawnedPiece, (int)SpawnedPieceLocation.X, (int)SpawnedPieceLocation.Y);
                    if (ps == PlaceStates.CAN_PLACE)
                    {
                        SpawnedPiece = newSpawnedPiece;
                    }

                    KeyBoardElapsedTime = 0;
                }


                if (ks.IsKeyDown(Keys.Down))
                {
                    ElapsedTime = StepTime + 1;
                    KeyBoardElapsedTime = 175;
                }
            }

            // If the accumulated time over the last couple Update() method calls exceeds our StepTime variable
            if (ElapsedTime > StepTime)
            {
                // Create a new location for this spawned piece to go to on the next update
                Vector2 NewSpawnedPieceLocation = SpawnedPieceLocation + new Vector2(0, 1);

                // Now check to see if we can place the piece at that new location
                PlaceStates ps = CanPlace(Board, SpawnedPiece, (int)NewSpawnedPieceLocation.X, (int)NewSpawnedPieceLocation.Y);
                if (ps != PlaceStates.CAN_PLACE)
                {
                    // We can't move down any further, so place the piece where it is currently
                    Place(Board, SpawnedPiece, (int)SpawnedPieceLocation.X, (int)SpawnedPieceLocation.Y);
                    SpawnPiece();

                    // This is just a check to see if the newly spawned piece is already blocked, in which case the 
                    // game is over
                    ps = CanPlace(Board, SpawnedPiece, (int)SpawnedPieceLocation.X, (int)SpawnedPieceLocation.Y);
                    if (ps == PlaceStates.BLOCKED)
                    {
                        // Game over.. normally we would change a game state variable but for this tutorial we're just
                        // going to exit the app
                        this.Exit();
                    }
                }
                else
                {
                    // We can move our piece into the new location, so update the existing piece location
                    SpawnedPieceLocation = NewSpawnedPieceLocation;
                }

                ElapsedTime = 0;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            spriteBatch.Draw(titleScreen, new Rectangle(0, 0, this.Window.ClientBounds.Width, this.Window.ClientBounds.Height), Color.White);

            // Draw the board first
            for (int y = 0; y < BoardHeight; y++)
                for (int x = 0; x < BoardWidth; x++)
                {
                    Color tintColor = TetronimoColors[Board[x, y]];

                    // Since for the board itself background colors are transparent, we'll go ahead and give this one
                    // a custom color.  This can be omitted if you draw a background image underneath your board
                    if (Board[x, y] == 0)
                        tintColor = Color.FromNonPremultiplied(50, 50, 50, 50);

                    spriteBatch.Draw(spriteSheet, new Rectangle((int)BoardLocation.X + x * BlockSize, (int)BoardLocation.Y + y * BlockSize, BlockSize, BlockSize), new Rectangle(0, 0, 32, 32), tintColor);
                }

            // Next draw the spawned piece
            int dim = SpawnedPiece.GetLength(0);

            for (int y = 0; y < dim; y++)
                for (int x = 0; x < dim; x++)
                {
                    if (SpawnedPiece[x, y] != 0)
                    {
                        Color tintColor = TetronimoColors[SpawnedPiece[x, y]];

                        spriteBatch.Draw(spriteSheet, new Rectangle((int)BoardLocation.X + ((int)SpawnedPieceLocation.X + x) * BlockSize, (int)BoardLocation.Y + ((int)SpawnedPieceLocation.Y + y) * BlockSize, BlockSize, BlockSize), new Rectangle(0, 0, 32, 32), tintColor);
                    }
                }
            if (score.PlayerScore >= 100)
            {
                spriteBatch.DrawString(
                    pericles14,
                    "Score: " + score.PlayerScore.ToString(),
                    scoreLocation,
                    Color.White);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
