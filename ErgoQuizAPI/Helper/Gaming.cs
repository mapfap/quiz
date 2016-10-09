using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ErgoQuizAPI.Helper
{
    public class Gaming
    {
        // Get Player from DB. Create new if not exists.
        public Player RetrievePlayer(string playerLanID)
        {
            Player player;
            using (var db = new ErgoQuizEntities())
            {
                player = db.Player.FirstOrDefault(p => p.LanId == playerLanID);
                if (player == null)
                {
                    player = new Player()
                    {
                        LanId = playerLanID
                    };
                    db.Player.Add(player);
                    db.SaveChanges();
                }
            }
            return player;
        }

        public Game ResumeGame(Player player, int questionSetID, ref bool isNewGame)
        {
            Game game;
            using (var db = new ErgoQuizEntities())
            {
                game = db.Game.FirstOrDefault(g => g.PlayerID == player.PlayerID && g.QuestionSetID == questionSetID);
                if (game == null)
                {
                    string sequence = GenerateGameSequence(questionSetID);
                    int firstQuestionID = Int32.Parse(sequence.Split(',')[0]);

                    game = new Game()
                    {
                        PlayerID = player.PlayerID,
                        QuestionSetID = questionSetID,
                        IsGameEnded = false,
                        TotalTimeUsed = 0,
                        ActualTimeUsed = 0,
                        GameSequence = sequence,
                        MostRecentEndedQuestionID = 0,
                        TotalScore = 0,
                        MaxConsecutiveScore = 0
                    };
                    db.Game.Add(game);
                    db.SaveChanges();
                    isNewGame = true;
                }
            }
            return game;
        }

        private string GenerateGameSequence(int questionSetID)
        {
            using(var db = new ErgoQuizEntities())
            {
                List<Int32> allQuestion = new List<Int32>();
                foreach(var question in db.Question.Where(q => q.QuestionSetID == questionSetID))
                {
                    allQuestion.Add(question.QuestionID);
                }
                
                var rnd = new Random();
                var sequence = allQuestion.OrderBy(item => rnd.Next());
                var output = String.Join(",", sequence);
                return output;
            }
        }

        private int determineNextQuestionID(Game game)
        {
            int currentQuestionID = (int)game.MostRecentEndedQuestionID;
            string[] sequence = game.GameSequence.Split(',');

            // Current is 0, This is the new game, return first question
            if (currentQuestionID == 0)
            {
                return Int32.Parse(sequence[0]);
            }

            for (int i = 0; i < sequence.Length; i++)
            {
                int questionID = Int32.Parse(sequence[i]);
                if (questionID == currentQuestionID)
                {
                    if (i + 1 >= sequence.Length)
                    {
                        // No more question.
                        return 0;
                    }
                    else
                    {
                        return Int32.Parse(sequence[i + 1]);
                    }
                }
            }

            // TODO: Code must not be reached !! how come MostRecent be a question not exist in sequence
            // just end the game
            return 0;
        }

        public Question GetQuestion(Session session)
        {
            using (var db = new ErgoQuizEntities())
            {
                return db.Question.Find(session.QuestionID);
            }
        }

        public void EndTheGame(Game game)
        {
            using (var db = new ErgoQuizEntities())
            {
                game.IsGameEnded = true;
                db.Game.Attach(game);
                db.Entry(game).Property(g => g.IsGameEnded).IsModified = true;
                db.SaveChanges();
            }
        }

        // return null if no more question to play.
        public Session GetCurrentSessionForReady(Game game, long timeLeftBeforeAsk)
        {
            // If there's open sesssion = skip and determine nextID using last ended to determine
            // Else create new session with redetermining one

            Session session;
            using (var db = new ErgoQuizEntities())
            {
                // Find an open session.
                session = db.Session.FirstOrDefault(s => s.GameID == game.GameID && s.IsAnsered == false && s.IsSkipped == false);

                // User try to retrieved question without answered the old one.
                // Skip the old one first
                if (session != null)
                {
                    session.IsSkipped = true;
                    session.SkippedAt = DateTime.UtcNow;
                    db.Session.Attach(session);
                    db.Entry(session).Property(s => s.IsSkipped).IsModified = true;
                    db.Entry(session).Property(s => s.SkippedAt).IsModified = true;

                    game.MostRecentEndedQuestionID = session.QuestionID;
                    db.Game.Attach(game);
                    db.Entry(game).Property(g => g.MostRecentEndedQuestionID).IsModified = true;
                    db.SaveChanges();

                }
            }
            session = null;

            // This method should not be called, when game is already ended.
            if (game.IsGameEnded)
            {
                return null;
            }

            int nextQuestionID = determineNextQuestionID(game);

            if (nextQuestionID == 0)
            {
                // For end the game.
                return null;
            }

            Session newCreatedSession = new Session()
            {
                GameID = game.GameID,
                QuestionID = nextQuestionID,
                TimeLeftBeforeAsk = timeLeftBeforeAsk,
                AskAt = DateTime.UtcNow,

                Answer = 0,
                IsAnsered = false,
                isCorrect = false,
                //TimeLeftAfterAnswer = "N/A",
                //AnswerAt = "N/A",
                // GameSequenceAt = 1, /not used anymore/
                TotalTimeUsed = 0,
                ActualTimeUsed = 0,

                IsSkipped = false
                //SkippedAt = "N/A"

            };

            using (var db = new ErgoQuizEntities())
            {
                db.Session.Add(newCreatedSession);
                db.SaveChanges();
            }

            return newCreatedSession;
        }

        public bool Evaluate(Session session, int answer, long timeLeft)
        {
            DateTime answerAt = DateTime.UtcNow;

            // Server time diff for waiting answer
            TimeSpan actualTimeUsed = (answerAt - (DateTime)session.AskAt);
            long actualMsUsed = (long)actualTimeUsed.TotalMilliseconds;

            // Time reported by user
            long totalMsUsed = (long)session.TimeLeftBeforeAsk - timeLeft;

            int key = (int)session.Question.Answer;
            bool isCorrect = key == answer;

            using (var db = new ErgoQuizEntities())
            {
                session.Answer = answer;
                session.IsAnsered = true;
                session.isCorrect = isCorrect;
                session.TimeLeftAfterAnswer = timeLeft;
                session.AnswerAt = answerAt;
                session.TotalTimeUsed = totalMsUsed;
                session.ActualTimeUsed = actualMsUsed;
                db.Session.Attach(session);
                db.Entry(session).Property(s => s.Answer).IsModified = true;
                db.Entry(session).Property(s => s.IsAnsered).IsModified = true;
                db.Entry(session).Property(s => s.isCorrect).IsModified = true;
                db.Entry(session).Property(s => s.TimeLeftAfterAnswer).IsModified = true;
                db.Entry(session).Property(s => s.AnswerAt).IsModified = true;
                db.Entry(session).Property(s => s.TotalTimeUsed).IsModified = true;
                db.Entry(session).Property(s => s.ActualTimeUsed).IsModified = true;
                db.SaveChanges();
            }

            using (var db = new ErgoQuizEntities())
            {
                Game game = db.Game.FirstOrDefault(g => g.GameID == session.GameID);
                game.MostRecentEndedQuestionID = session.QuestionID;
                game.TotalTimeUsed += totalMsUsed;
                game.ActualTimeUsed += actualMsUsed;
                if (isCorrect)
                {
                    game.TotalScore += 1;
                    game.MaxConsecutiveScore += 1;
                }
                else
                {
                    game.TotalScore += 0;
                    game.MaxConsecutiveScore = 0;
                }

                db.Game.Attach(game);

                db.Entry(game).Property(g => g.MostRecentEndedQuestionID).IsModified = true;
                db.Entry(game).Property(g => g.TotalTimeUsed).IsModified = true;
                db.Entry(game).Property(g => g.ActualTimeUsed).IsModified = true;
                db.Entry(game).Property(g => g.TotalScore).IsModified = true;
                db.Entry(game).Property(g => g.MaxConsecutiveScore).IsModified = true;

                db.SaveChanges();
            }

            return isCorrect;
        }

        public Session GetCurrentSessionForAnswer(Game game)
        {
            Session session;
            using (var db = new ErgoQuizEntities())
            {
                // Find an open session.
                session = db.Session.FirstOrDefault(s => s.GameID == game.GameID && s.IsAnsered == false && s.IsSkipped == false);

            }

            return session;
        }
    }
}