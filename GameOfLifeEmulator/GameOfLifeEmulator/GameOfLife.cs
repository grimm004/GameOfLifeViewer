using System;
using System.Threading.Tasks;

namespace GameOfLife
{
    public class GameOfLife
    {
        private bool[,] world;
        private bool[,] nextGeneration;
        private Task processTask;
        private bool destroyExteriorCells;

        public GameOfLife(int size, bool destroySideCells = true)
        {
            destroyExteriorCells = destroySideCells;
            if (size <= 0) throw new ArgumentOutOfRangeException("Size must be greater than zero");
            this.SizeX = this.SizeY = size;
            world = new bool[size, size];
            nextGeneration = new bool[size, size];
        }

        public GameOfLife(int xSize, int ySize, bool destroySideCells = true)
        {
            destroyExteriorCells = destroySideCells;
            if (xSize <= 0 || ySize <= 0) throw new ArgumentOutOfRangeException("Size must be greater than zero");
            this.SizeX = xSize;
            this.SizeY = ySize;
            world = new bool[xSize, ySize];
            nextGeneration = new bool[SizeX, SizeY];
        }

        public int SizeX { get; private set; }
        public int SizeY { get; private set; }
        public int Generation { get; private set; }

        public Action<bool[,]> NextGenerationCompleted;

        public bool this[int x, int y]
        {
            get { return this.world[x, y]; }
            set { this.world[x, y] = value; }
        }

        public bool ToggleCell(int x, int y)
        {
            bool currentValue = this.world[x, y];
            return this.world[x, y] = !currentValue;
        }

        public void Update()
        {
            if (this.processTask != null && this.processTask.IsCompleted)
            {
                // when a generation has completed
                // now flip the back buffer so we can start processing on the next generation
                var flip = this.nextGeneration;
                this.nextGeneration = this.world;
                this.world = flip;
                Generation++;

                // begin the next generation's processing asynchronously
                this.processTask = this.ProcessGeneration();

                NextGenerationCompleted?.Invoke(this.world);
            }
        }

        public void BeginGeneration()
        {
            if (this.processTask == null || (this.processTask != null && this.processTask.IsCompleted))
            {
                // only begin the generation if the previous process was completed
                this.processTask = this.ProcessGeneration();
            }
        }

        public void Wait()
        {
            if (this.processTask != null)
            {
                this.processTask.Wait();
            }
        }

        private Task ProcessGeneration()
        {
            return Task.Factory.StartNew(() =>
            {
                Parallel.For(0, SizeX, x =>
                {
                    Parallel.For(0, SizeY, y =>
                    {
                        int numberOfNeighbors =
                              IsNeighborAlive(world, SizeX, SizeY, x, y, -1, 0)
                            + IsNeighborAlive(world, SizeX, SizeY, x, y, -1, 1)
                            + IsNeighborAlive(world, SizeX, SizeY, x, y, 0, 1)
                            + IsNeighborAlive(world, SizeX, SizeY, x, y, 1, 1)
                            + IsNeighborAlive(world, SizeX, SizeY, x, y, 1, 0)
                            + IsNeighborAlive(world, SizeX, SizeY, x, y, 1, -1)
                            + IsNeighborAlive(world, SizeX, SizeY, x, y, 0, -1)
                            + IsNeighborAlive(world, SizeX, SizeY, x, y, -1, -1);

                        bool shouldLive = false;
                        bool isAlive = world[x, y];

                        if (isAlive && (numberOfNeighbors == 2 || numberOfNeighbors == 3)) shouldLive = true;
                        else if (!isAlive && numberOfNeighbors == 3) shouldLive = true;
                        if (destroyExteriorCells && isAlive && (x <= 0 || y <= 0 || x >= SizeX - 1 || y >= SizeY - 1)) shouldLive = false;
                        nextGeneration[x, y] = shouldLive;
                    });
                });
            });
        }

        private static int IsNeighborAlive(bool[,] world, int xSize, int ySize, int x, int y, int offsetx, int offsety)
        {
            int result = 0;

            int proposedOffsetX = x + offsetx;
            int proposedOffsetY = y + offsety;
            bool outOfBounds = proposedOffsetX < 0 || proposedOffsetX >= xSize | proposedOffsetY < 0 || proposedOffsetY >= ySize;
            if (!outOfBounds)
                result = world[x + offsetx, y + offsety] ? 1 : 0;
            return result;
        }
    }

    public class GameOfLifeSnapshot
    {
        private bool[,] world;

        public GameOfLifeSnapshot(bool[,] currentWorld)
        {
            world = currentWorld;
        }

        public bool[,] World { get { return world; } }

        public bool this[int x, int y]
        {
            get { return this.world[x, y]; }
            set { this.world[x, y] = value; }
        }
    }
}