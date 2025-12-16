using System;
using System.Collections.Generic;
using System.Linq;
using SeaBattle.Entities;
using SeaBattle.Repositories;

namespace SeaBattle.Services
{
    // --- DTOs (Views) ---
    public class UserView
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public int Rating { get; set; }
    }

    public class GameHistoryView
    {
        public string Date { get; set; }
        public string Result { get; set; }
        public int RatingChange { get; set; }
    }

    // --- MAPPERS ---
    public static class UserMapper
    {
        public static UserView ToView(UserEntity entity)
        {
            return new UserView
            {
                Id = entity.Id,
                Username = entity.Username,
                Rating = entity.Rating
            };
        }
    }

    // --- SERVICES ---

    public class AuthService
    {
        private readonly IUserRepository _userRepo;

        public AuthService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public UserView Register(string username, string password)
        {
            if (_userRepo.GetByUsername(username) != null)
                throw new Exception("Користувач вже існує.");

            var newUser = new UserEntity { Username = username, Password = password, Rating = 1000 };
            _userRepo.Create(newUser);
            
            return UserMapper.ToView(newUser);
        }

        public UserView Login(string username, string password)
        {
            var user = _userRepo.GetByUsername(username);
            if (user == null || user.Password != password)
                throw new Exception("Невірний логін або пароль.");
            
            return UserMapper.ToView(user);
        }
    }

    public class GameService
    {
        private readonly IUserRepository _userRepo;
        private readonly IGameRepository _gameRepo;

        public GameService(IUserRepository userRepo, IGameRepository gameRepo)
        {
            _userRepo = userRepo;
            _gameRepo = gameRepo;
        }

        public List<GameHistoryView> GetHistory(int userId)
        {
            var history = _gameRepo.GetByUserId(userId);
            return history.Select(h => new GameHistoryView 
            { 
                Date = h.Date.ToShortDateString(),
                Result = h.IsWin ? "Перемога" : "Поразка",
                RatingChange = h.RatingChange
            }).ToList();
        }

        // --- ГОЛОВНА ЛОГІКА ГРИ ---
        // Додано параметр: Action<string> turnResultCallback
        public void PlayGameSession(
            UserView currentUserView, 
            Func<Point> inputProvider, 
            Action<Player, Player> drawCallback,
            Action<string> turnResultCallback) 
        {
            var userEntity = _userRepo.GetByUsername(currentUserView.Username);

            Player human = new HumanPlayer(userEntity.Username, inputProvider);
            Player bot = new BotPlayer();

            bool isWin = false;

            while (true)
            {
                // 1. Малюємо поле
                drawCallback(human, bot);

                // 2. Хід Людини
                Point shot;
                bool isValid = false;
                do
                {
                    shot = human.MakeMove(); 
                    if (bot.IsCellAlreadyShot(shot))
                        Console.WriteLine(">> Ви вже стріляли сюди! Спробуйте ще раз.");
                    else isValid = true;
                } while (!isValid);

                bool humanHit = bot.ReceiveShot(shot);
                
                // Перевірка перемоги людини
                if (!bot.HasShipsLeft()) { isWin = true; break; }

                // 3. Хід Бота
                Point botShot;
                do { botShot = bot.MakeMove(); } while (human.IsCellAlreadyShot(botShot));
                
                bool botHit = human.ReceiveShot(botShot);
                
                string logMessage = $"Ви стріляли в {shot.X},{shot.Y} -> {(humanHit ? "Влучив" : "Промах")}\n";
                logMessage += $"Бот стріляв в {botShot.X},{botShot.Y} -> {(botHit ? "Влучив" : "Промах")}";

                // Викликаємо callback, щоб Program.cs вивів це і почекав Enter
                turnResultCallback(logMessage);

                // Перевірка поразки людини
                if (!human.HasShipsLeft()) { isWin = false; break; }
            }

            // Кінець гри
            int points = isWin ? 25 : -15;
            userEntity.Rating += points;
            _userRepo.Update(userEntity);

            var history = new GameHistoryEntity
            {
                PlayerId = userEntity.Id,
                Date = DateTime.Now,
                IsWin = isWin,
                RatingChange = points,
                Log = $"Game vs Bot. {(isWin ? "Win" : "Loss")}"
            };
            _gameRepo.AddHistory(history);

            Console.Clear();
            Console.WriteLine(isWin ? "!!! ПЕРЕМОГА !!!" : "... ПОРАЗКА ...");
            Console.WriteLine($"Рейтинг змінено: {points}. Поточний: {userEntity.Rating}");
            Console.WriteLine("Натисніть Enter для виходу в меню.");
            Console.ReadLine();
        }
    }

    // --- ВНУТРІШНІ КЛАСИ ---
    public enum CellState { Empty, Ship, Miss, Hit }
    public struct Point { public int X; public int Y; public Point(int x, int y) { X = x; Y = y; } }

    public abstract class Player
    {
        public string Name { get; protected set; }
        public CellState[,] Board { get; protected set; } = new CellState[10, 10];

        public Player(string name) { Name = name; PlaceShipsStandard(); }
        public abstract Point MakeMove();
        
        public bool IsCellAlreadyShot(Point p) => 
            (p.X < 0 || p.X > 9 || p.Y < 0 || p.Y > 9) || Board[p.X, p.Y] == CellState.Hit || Board[p.X, p.Y] == CellState.Miss;

        public bool ReceiveShot(Point p)
        {
            if (Board[p.X, p.Y] == CellState.Ship) { Board[p.X, p.Y] = CellState.Hit; return true; }
            if (Board[p.X, p.Y] == CellState.Empty) { Board[p.X, p.Y] = CellState.Miss; return false; }
            return false;
        }

        public bool HasShipsLeft()
        {
            foreach (var c in Board) if (c == CellState.Ship) return true;
            return false;
        }

        private void PlaceShipsStandard()
        {
            int[] ships = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
            Random rnd = new Random();
            foreach (var size in ships) {
                bool placed = false;
                while (!placed) {
                    int x = rnd.Next(0, 10), y = rnd.Next(0, 10);
                    bool v = rnd.Next(0, 2) == 0;
                    if (CanPlace(x, y, size, v)) { Place(x, y, size, v); placed = true; }
                }
            }
        }
        
        private bool CanPlace(int x, int y, int size, bool v) {
            if (v && y + size > 10) return false;
            if (!v && x + size > 10) return false;
            int sx = Math.Max(0, x - 1), sy = Math.Max(0, y - 1);
            int ex = Math.Min(9, v ? x + 1 : x + size), ey = Math.Min(9, v ? y + size : y + 1);
            for (int i = sx; i <= ex; i++) for (int j = sy; j <= ey; j++) if (Board[i, j] != CellState.Empty) return false;
            return true;
        }
        private void Place(int x, int y, int size, bool v) {
            for (int k = 0; k < size; k++) if (v) Board[x, y + k] = CellState.Ship; else Board[x + k, y] = CellState.Ship;
        }
    }

    public class BotPlayer : Player 
    { 
        public BotPlayer() : base("Bot") { } 
        public override Point MakeMove() => new Point(new Random().Next(0, 10), new Random().Next(0, 10)); 
    }
    
    public class HumanPlayer : Player 
    { 
        private Func<Point> _in; 
        public HumanPlayer(string n, Func<Point> i) : base(n) { _in = i; } 
        public override Point MakeMove() => _in(); 
    }
}