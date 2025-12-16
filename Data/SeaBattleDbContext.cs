using System.Collections.Generic;
using SeaBattle.Entities;

namespace SeaBattle.Data
{
    public class SeaBattleDbContext
    {
        // "Таблиці"
        public List<UserEntity> Users { get; set; }
        public List<GameHistoryEntity> GameHistories { get; set; }

        public SeaBattleDbContext()
        {
            Users = new List<UserEntity>();
            GameHistories = new List<GameHistoryEntity>();
            SeedData(); // Наповнення початковими даними
        }

        private void SeedData()
        {
            Users.Add(new UserEntity 
            { 
                Id = 1, 
                Username = "admin", 
                Password = "admin", 
                Rating = 9999 
            });
            
            Users.Add(new UserEntity 
            { 
                Id = 2, 
                Username = "player1", 
                Password = "123", 
                Rating = 1000 
            });
        }
    }
}