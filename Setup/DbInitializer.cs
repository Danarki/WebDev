using Microsoft.EntityFrameworkCore;
using WebDev.Models;

namespace WebDev
{
    public class DbInitializer
    {
        public static void Initialize(WebAppContext context)
        {
            context.Database.EnsureCreated();

            if (!context.Users.Any())
            {
                User user = new User();
                user.Username = "admin";
                user.Password = BCrypt.Net.BCrypt.HashPassword("admin");
                user.VerifiedAt = DateTime.UtcNow;
                user.Email = "admin@cardigo.com";

                context.Users.Add(user);

                try
                {
                    context.SaveChanges();

                }
                catch (Exception e)
                {
                    throw new Exception("Database Seeding Error!", e.InnerException);
                }
            }

            if (!context.GameTypes.Any())
            {
                GameType gameType = new GameType();
                gameType.Name = "Blackjack";
                gameType.Type = GameTypeEnum.Dealer;
                gameType.MaxPlayers = 7;

                context.GameTypes.Add(gameType);

                GameType gameType2 = new GameType();
                gameType2.Name = "Pesten";
                gameType2.Type = GameTypeEnum.GroupGame;
                gameType2.MaxPlayers = 6;

                context.GameTypes.Add(gameType2);

                try
                {
                    context.SaveChanges();

                }
                catch (Exception e)
                {
                    throw new Exception("Database Seeding Error!", e.InnerException);
                }
            }
        }
    }
}
