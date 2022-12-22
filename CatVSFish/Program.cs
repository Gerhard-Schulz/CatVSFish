using System;

namespace CatVSFish
{
    public class Game
    {
        private readonly Random random;
        private readonly CatPlayer player;
        public readonly GameStatisctics statistics;

        public event MoveDelegate CatMove;
        public event PrintDelegate PrintField;

        public GameState State
        {
            get
            {
                if(player.Energy <= 0)
                {
                    return GameState.LoseHunger;
                }
                if(player.Energy > player.maxEnergy)
                {
                    return GameState.LoseObesity;
                }
                if(statistics.TotalFoodGenerated != statistics.Eats)
                {
                    return GameState.Progress;
                }
                return GameState.Win;
            }
        }
        private bool[,] Field { get; set; }

        public Game(int n)
        {
            random = new Random(Guid.NewGuid().GetHashCode());
            Field = new bool[n, n];
            statistics = new GameStatisctics();
            player = new CatPlayer(this, 0, 0, 5);
            CatMove += player.MoveCat;
            PrintField += PrintGame;
            GenarateFood();
        }

        public void GenarateFood()
        {
            int fieldLength = Field.GetLength(1);
            int foodAmount = (int)(fieldLength * 1.5);
            statistics.SetFoodAmount(foodAmount);
            for (int i = 0; i < foodAmount; i++)
            {
                int calculateX = random.Next(fieldLength);
                int calculateY = random.Next(fieldLength);
                while ((player.X == calculateX && player.Y == calculateY) || Field[calculateY, calculateX])
                {
                    calculateX = random.Next(fieldLength);
                    calculateY = random.Next(fieldLength);
                }
                Field[calculateY, calculateX] = true;
            }
        }

        public bool CheckMovement(MovementDirection direction)
        {
            switch (direction)
            {
                case MovementDirection.Up:
                    return player.Y - 1 < 0;
                case MovementDirection.Down:
                    return player.Y + 1 >= Field.GetLength(1);
                case MovementDirection.Left:
                    return player.X - 1 < 0;
                case MovementDirection.Right:
                    return player.X + 1 >= Field.GetLength(1);
                default:
                    return false;
            }
        }

        public bool TryEat()
        {
            bool oldValue = Field[player.Y, player.X];
            Field[player.Y, player.X] = false;
            return oldValue;
        }

        public void UpdateInterface()
        {
            PrintField();
            Console.Write("Введите направление движения котика (WASD): ");
            string input = Console.ReadLine();
            Console.Clear();
            ProcessInput(input);
        }

        private void PrintGame()
        {
            string[] info = GetCurrentGameInfoLines();
            int length = Field.GetLength(0);
            Console.BackgroundColor = ConsoleColor.DarkRed;
            for (int i = 0; i < length + 2; i++)
            {
                Console.Write("# ");
            }
            Console.WriteLine();
            for (int i = 0; i < length; i++)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.Write("# ");
                Console.BackgroundColor = ConsoleColor.Gray;
                for (int j = 0; j < length; j++)
                {
                    if (player.Y == i && player.X == j)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("C ");
                    }
                    else if (Field[i, j])
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write("F ");
                    }
                    else
                    {
                        Console.Write("  ");
                    }
                }
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("# ");
                Console.BackgroundColor = ConsoleColor.Black;
                if (i < info.Length)
                {
                    Console.Write($"\t{info[i]}");
                }
                Console.WriteLine();
            }
            Console.BackgroundColor = ConsoleColor.DarkRed;
            for (int i = 0; i < length + 2; i++)
            {
                Console.Write("# ");
            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine();
        }

        private string[] GetCurrentGameInfoLines()
        {
            return new string[]
            {
            $"Энергия котика: {player.Energy}",
            $"Максимальная энергия котика: {player.maxEnergy}",
            $"Позиция котика: ({player.X},{player.Y})",
            $"Рыбок осталось: {statistics.TotalFoodGenerated - statistics.Eats} из {statistics.TotalFoodGenerated}"          
            };
        }
        private void ProcessInput(string input)
        {
            input = input?.ToUpper();
            bool moveResult = true;
            switch (input)
            {
                case "W":
                case "Ц":
                    moveResult = CatMove.Invoke(MovementDirection.Up);
                    break;
                case "A":
                case "Ф":
                    moveResult = CatMove.Invoke(MovementDirection.Left);
                    break;
                case "S":
                case "Ы":
                    moveResult = CatMove.Invoke(MovementDirection.Down);
                    break;
                case "D":
                case "В":
                    moveResult = CatMove.Invoke(MovementDirection.Right);
                    break;
                default:
                    Console.WriteLine("Введённые данные не удалось распознать :(");
                    break;
            }
            if (!moveResult)
            {
                Console.WriteLine("Вы не можете ходить в эту сторону!");
            }
        }

    }

    public enum MovementDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public enum GameState
    {
        LoseObesity,
        LoseHunger,
        Progress,
        Win
    }

    public class CatPlayer
    {
        private readonly Game game;
        public readonly int maxEnergy = 10;
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Energy { get; private set; }

        public CatPlayer(Game game, int x, int y, int energy)
        {
            this.game = game;
            X = x;
            Y = y;
            Energy = energy;
        }

        private void Move(MovementDirection direction)
        {
            game.statistics.IncStep();
            switch (direction)
            {
                case MovementDirection.Up:
                    Y--;
                    break;
                case MovementDirection.Down:
                    Y++;
                    break;
                case MovementDirection.Left:
                    X--;
                    break;
                case MovementDirection.Right:
                    X++;
                    break;
            }
            Energy--;
        }

        public bool MoveCat(MovementDirection direction)
        {
            bool notAccessible = game.CheckMovement(direction);
            if (notAccessible)
            {
                return false;
            }
            Move(direction);
            bool foodEated = game.TryEat();
            if (foodEated)
            {
                game.statistics.IncEat();
                Energy += 3;
            }
            return true;
        }
    }

    public delegate bool MoveDelegate(MovementDirection direction);
    public delegate void PrintDelegate();

    public class GameStatisctics
    {
        public int TotalFoodGenerated { get; private set; }
        public int Steps { get; private set; }
        public int Eats { get; private set; }

        public void IncStep() => Steps++;
        public void IncEat() => Eats++;
        public void SetFoodAmount(int amount) => TotalFoodGenerated = amount;
        public override string ToString()
        {
            return $"\tСтатистика\nВсего рыбок съедено: {Eats}/{TotalFoodGenerated}\nВсего ходов: {Steps}";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Введите сторону квадрата поля (Должна быть > 7): ");
            string gameSquareSizeString = Console.ReadLine();
            int gameSquareSize = 8;
            while (!int.TryParse(gameSquareSizeString, out gameSquareSize) || gameSquareSize < 8)
            {
                Console.Clear();
                Console.WriteLine("Неверный формат ввода!");
                Console.Write("Введите сторону квадрата поля (Должна быть > 7): ");
                gameSquareSizeString = Console.ReadLine();
            }
            Game game = new Game(gameSquareSize);
            Console.Clear();
            while (game.State == GameState.Progress)
            {
                game.UpdateInterface();
            }
            switch (game.State)
            {
                case GameState.LoseHunger:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Котик ослаб от голода :( \n");
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case GameState.LoseObesity:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Котик переел :( \n");
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case GameState.Win:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Котик сожрал всю рыбу :) Вы победили!\n");
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            Console.Write(game.statistics);
            Console.ReadKey();
        }
    }
}