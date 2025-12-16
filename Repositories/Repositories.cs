using System.Collections.Generic;
using System.Linq;
using SeaBattle.Data;
using SeaBattle.Entities;

namespace SeaBattle.Repositories
{
    // Інтерфейс для користувачів
    public interface IUserRepository
    {
        UserEntity GetByUsername(string username);
        void Create(UserEntity user);
        void Update(UserEntity user);
    }

    // Інтерфейс для ігор
    public interface IGameRepository
    {
        void AddHistory(GameHistoryEntity game);
        List<GameHistoryEntity> GetByUserId(int userId);
    }

    // Реалізація UserRepository
    public class UserRepository : IUserRepository
    {
        private readonly SeaBattleDbContext _context;

        public UserRepository(SeaBattleDbContext context)
        {
            _context = context;
        }

        public UserEntity GetByUsername(string username)
        {
            return _context.Users.FirstOrDefault(u => u.Username == username);
        }

        public void Create(UserEntity user)
        {
            // Генеруємо ID (автоінкремент)
            int newId = _context.Users.Any() ? _context.Users.Max(u => u.Id) + 1 : 1;
            user.Id = newId;
            _context.Users.Add(user);
        }

        public void Update(UserEntity user)
        {
            // В List зміни зберігаються по посиланню, але для порядку знаходимо
            var existing = _context.Users.FirstOrDefault(u => u.Id == user.Id);
            if (existing != null)
            {
                existing.Rating = user.Rating;
            }
        }
    }

    // Реалізація GameRepository
    public class GameRepository : IGameRepository
    {
        private readonly SeaBattleDbContext _context;

        public GameRepository(SeaBattleDbContext context)
        {
            _context = context;
        }

        public void AddHistory(GameHistoryEntity game)
        {
            int newId = _context.GameHistories.Any() ? _context.GameHistories.Max(g => g.Id) + 1 : 1;
            game.Id = newId;
            _context.GameHistories.Add(game);
        }

        public List<GameHistoryEntity> GetByUserId(int userId)
        {
            return _context.GameHistories.Where(g => g.PlayerId == userId).ToList();
        }
    }
}