using System;
using System.Text;
using SeaBattle.Data;
using SeaBattle.Repositories;
using SeaBattle.Services;

namespace SeaBattle
{
    class Program
    {
        // Dependency Injection
        static SeaBattleDbContext dbContext = new SeaBattleDbContext();
        static IUserRepository userRepo = new UserRepository(dbContext);
        static IGameRepository gameRepo = new GameRepository(dbContext);
        
        static AuthService authService = new AuthService(userRepo);
        static GameService gameService = new GameService(userRepo, gameRepo);

        static UserView currentUser = null;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            
            while (true)
            {
                if (currentUser == null) ShowAuthMenu();
                else ShowMainMenu();
            }
        }

        static void ShowAuthMenu()
        {
            Console.Clear();
            Console.WriteLine("=== ВХІД ДО СИСТЕМИ ===");
            Console.WriteLine("1. Реєстрація");
            Console.WriteLine("2. Вхід");
            Console.WriteLine("3. Вихід з програми");
            Console.Write("Вибір: ");
            
            var choice = Console.ReadLine();
            if (choice == "3") Environment.Exit(0);

            Console.Write("Логін: "); string l = Console.ReadLine();
            Console.Write("Пароль: "); string p = Console.ReadLine();

            try {
                if (choice == "1") {
                    currentUser = authService.Register(l, p);
                    Console.WriteLine("Успішна реєстрація! Enter..."); Console.ReadLine();
                }
                else if (choice == "2") {
                    currentUser = authService.Login(l, p);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Помилка: {ex.Message}"); Console.ReadLine();
            }
        }

        static void ShowMainMenu()
        {
            Console.Clear();
            // Скріншот 2
            Console.WriteLine($"Гравець: {currentUser.Username} | Рейтинг: {currentUser.Rating}");
            Console.WriteLine("1. Грати в Морський Бій");
            Console.WriteLine("2. Історія ігор");
            Console.WriteLine("3. Вийти з акаунту");
            Console.Write("\nВибір: ");

            switch (Console.ReadLine())
            {
                case "1":
                    StartGame();
                    break;
                case "2":
                    ShowHistory();
                    break;
                case "3":
                    currentUser = null;
                    break;
            }
        }

        static void StartGame()
        {
            // Передаємо 3 методи: Input, Draw, TurnResult
            gameService.PlayGameSession(
                currentUser, 
                GetInput,   
                DrawBoard,
                ShowTurnResult // Новий метод для відображення результатів ходу
            );

            // Оновлення рейтингу в UI
            try { var u = authService.Login(currentUser.Username, "ignored"); if(u!=null) currentUser.Rating = u.Rating; } catch { }
        }
        
        static Point GetInput()
        {
            while (true)
            {
                // Тут Write, щоб ввід був у тому ж рядку
                Console.Write("Ваш постріл (формат X Y, наприклад 0 0): ");
                string input = Console.ReadLine();
                var parts = input.Split(' ');
                
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    return new Point(x, y);
                }
                Console.WriteLine("Невірний формат!");
            }
        }
        
        static void ShowTurnResult(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Натисніть Enter для наступного ходу...");
            Console.ResetColor();
            Console.ReadLine();
        }
        
        static void DrawBoard(Player human, Player bot)
        {
            Console.Clear();
            DrawSingleBoard(bot, true); // Бот зверху
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(new string('-', 35)); 
            
            DrawSingleBoard(human, false); // Гравець знизу
        }

        static void DrawSingleBoard(Player p, bool hideShips)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Поле гравця: {p.Name}");
            
            Console.Write("  ");
            for (int i = 0; i < 10; i++) Console.Write(i + " ");
            Console.WriteLine();

            for (int y = 0; y < 10; y++)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(y + " "); // Номер рядка зліва

                for (int x = 0; x < 10; x++)
                {
                    var cell = p.Board[x, y];
                    switch (cell)
                    {
                        case CellState.Empty:
                            Console.ForegroundColor = ConsoleColor.Blue; // Вода ~
                            Console.Write("~ ");
                            break;
                        case CellState.Ship:
                            if (hideShips) {
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.Write("~ ");
                            } else {
                                Console.ForegroundColor = ConsoleColor.Green; // Корабель #
                                Console.Write("# ");
                            }
                            break;
                        case CellState.Hit:
                            Console.ForegroundColor = ConsoleColor.Red; // Влучання X (або # червоним)
                            Console.Write("# ");  
                            break;
                        case CellState.Miss:
                            Console.ForegroundColor = ConsoleColor.White; // Промах *
                            Console.Write("* ");
                            break;
                    }
                }
                Console.WriteLine();
            }
            Console.ResetColor();
        }

        static void ShowHistory()
        {
            Console.Clear();
            Console.WriteLine("ІСТОРІЯ ІГОР:");
            var history = gameService.GetHistory(currentUser.Id);
            foreach (var h in history) Console.WriteLine($"{h.Date} | {h.Result} | Рейтинг: {h.RatingChange:+0;-0}");
            Console.WriteLine("\nEnter для повернення...");
            Console.ReadLine();
        }
    }
}