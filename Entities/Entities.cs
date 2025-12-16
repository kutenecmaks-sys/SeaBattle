using System;

namespace SeaBattle.Entities
{
    // Таблиця користувачів
    public class UserEntity
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Rating { get; set; }
    }

    // Таблиця історії ігор
    public class GameHistoryEntity
    {
        public int Id { get; set; }
        public int PlayerId { get; set; } 
        public DateTime Date { get; set; }
        public bool IsWin { get; set; }
        public string Log { get; set; } 
        public int RatingChange { get; set; }
    }
}