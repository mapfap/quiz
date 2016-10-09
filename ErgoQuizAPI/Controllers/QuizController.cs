using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ErgoQuizAPI.Helper;
using NLog;

namespace ErgoQuizAPI.Controllers
{
    public class QuizController : ApiController
    {
        private Logger logger = LogManager.GetCurrentClassLogger();
        private Authenticator Authenticator;
        private Gaming Gaming;
        private EventLogger EventLogger;

        public readonly int CURRENT_QUESTION_SET_ID = 1;
        public readonly long QUIZ_TIME = 120 * 1000; // 120 sec.

        public QuizController()
        {
            Authenticator = new Authenticator();
            Gaming = new Gaming();
            EventLogger = new EventLogger();
        }

        [HttpGet]
        [Route("quiz/hello")]
        public string Hello()
        {
            string lanID = Authenticator.GetLanID();
            if (lanID == null)
            {
                EventLogger.Log(EventCode.LOGIN_FAIL, "/hello");
                return "{\"ResponseCode\":\"" + ResponseCode.ERROR + "\", \"Message\":\"Fail to login. Try again using Internet Explorer.\"}";
            }

            Player player = Gaming.RetrievePlayer(lanID);

            bool isNewGame = false;
            Game game = Gaming.ResumeGame(player, CURRENT_QUESTION_SET_ID, ref isNewGame);

            if (isNewGame)
            {
                EventLogger.Log(EventCode.LOGIN_FIRST_TIME, game.GameID + "");
                return "{\"ResponseCode\":\"" + ResponseCode.GREETING + "\", \"TimeLeft\":" + QUIZ_TIME + "}";
            }
            else if (game.IsGameEnded)
            {
                EventLogger.Log(EventCode.LOGIN_SCORE, game.GameID + "");
                return "{\"ResponseCode\":\"" + ResponseCode.SCOREBOARD + "\", \"TotalScore\":" + game.TotalScore + "}";
            } 
            else 
            {
                EventLogger.Log(EventCode.LOGIN_RETURNING, game.GameID + "");
                return "{\"ResponseCode\":\"" + ResponseCode.WELCOME_BACK + "\", \"TimeLeft\":" + (QUIZ_TIME - game.TotalTimeUsed) + "}";
            }
        }


        [HttpGet]
        [Route("quiz/ready")]
        public string Ready(long? timeLeft)
        {
            if (timeLeft == null)
            {
                return "{\"ResponseCode\":\"" + ResponseCode.ERROR + "\", \"Message\":\"Missing 'timeLeft.'\"}";
            }

            string lanID = Authenticator.GetLanID();
            if (lanID == null)
            {
                EventLogger.Log(EventCode.LOGIN_FAIL, "/ready");
                return "{\"ResponseCode\":\"" + ResponseCode.ERROR + "\", \"Message\":\"Fail to login. Try again using Internet Explorer.\"}";
            }

            Player player = Gaming.RetrievePlayer(lanID);

            bool isNewGame = false;
            Game game = Gaming.ResumeGame(player, CURRENT_QUESTION_SET_ID, ref isNewGame);

            if (game.IsGameEnded)
            {
                // Users are not supposed to reach this code..
                return "{\"ResponseCode\":\"" + ResponseCode.GAMEOVER + "\", \"Message\":\"TIME OUT !\"}";
            }
            else
            {
                // NOTE: Record the time-left that user reported.
                Session session = Gaming.GetCurrentSessionForReady(game, (long)timeLeft);

                // This means no more question
                if (session == null)
                {
                    EventLogger.Log(EventCode.READY_NO_QUESTION, game.GameID + "");
                    Gaming.EndTheGame(game);
                    return "{\"ResponseCode\":\"" + ResponseCode.GAMEOVER + "\", \"Message\":\"You answered all question.\"}";
                }
                
                // TODO: Total time too much? time stamp mistmatch ? 
                if (false)
                {
                    EventLogger.Log(EventCode.READY_TIME_ANOMALY, game.GameID + ",reason here");
                }

                EventLogger.Log(EventCode.READY, session.SessionID + "");


                Question question = Gaming.GetQuestion(session);

                string json = "{";
                json += "\"ResponseCode\": \"" + ResponseCode.QUESTION + "\""; 
                json += ",\"QuestionID\": " + question.QuestionID;
                json += ",\"Title\": \"" + question.TITLE + "\"";
                json += ",\"Picture\": \"" + question.Picture + "\"";
                json += ",\"Choice1\": \"" + question.Choice1 + "\"";
                json += ",\"Choice2\": \"" + question.Choice2 + "\"";
                json += ",\"Choice3\": \"" + question.Choice3 + "\"";
                json += ",\"Choice4\": \"" + question.Choice4 + "\"";
                json += "}";

                return json;
            }

        }

        [HttpGet]
        [Route("quiz/answer")]
        public string Answer(int? questionID, int? answer, long? timeLeft)
        {
            if (questionID == null || answer == null ||timeLeft == null)
            {
                return "{\"ResponseCode\":\"" + ResponseCode.ERROR + "\", \"Message\":\"Missing 'timeLeft or answer or questionID.'\"}";
            }

            string lanID = Authenticator.GetLanID();
            if (lanID == null)
            {
                EventLogger.Log(EventCode.LOGIN_FAIL, "/answer");
                return "{\"ResponseCode\":\"" + ResponseCode.ERROR + "\", \"Message\":\"Fail to login. Try again using Internet Explorer.\"}";
            }

            Player player = Gaming.RetrievePlayer(lanID);

            bool isNewGame = false;
            Game game = Gaming.ResumeGame(player, CURRENT_QUESTION_SET_ID, ref isNewGame);
            
            if (game.IsGameEnded)
            {
                // Users are not supposed to reach this code..
                return "{\"ResponseCode\":\"" + ResponseCode.GAMEOVER + "\", \"Message\":\"Game is ended !\"}";
            }

            Session session = Gaming.GetCurrentSessionForAnswer(game);

            // No question to answer
            if (session == null)
            {
                // Users are not supposed to reach this code..
                EventLogger.Log(EventCode.ANSWER_NO_SESSION, game.GameID + "");
                return "{\"ResponseCode\":\"" + ResponseCode.ERROR + "\", \"Message\":\"Anomaly detected\"}";
            }

            // Answer to wrong question
            if (session.QuestionID != questionID)
            {
                EventLogger.Log(EventCode.ANSWER_QID_MISMATCH, session.SessionID + "," + questionID);
                return "{\"ResponseCode\":\"" + ResponseCode.ERROR + "\", \"Message\":\"Anomaly detected\"}";
            }

            if (timeLeft <= 0)
            {
                EventLogger.Log(EventCode.ANSWER_TIMEOUT, session.SessionID + "");
                Gaming.EndTheGame(game);
                // Redirect user go to main page again? 
                return "{\"ResponseCode\":\"" + ResponseCode.GAMEOVER + "\", \"Message\":\"TIME OUT !\"}";
            }


            // TODO: Total time too much? time stamp mistmatch ? 
            if (false)
            {
                EventLogger.Log(EventCode.ANSWER_TIME_ANOMALY, game.GameID + ",reason here");
            }

            bool isCorrect = Gaming.Evaluate(session, (int)answer, (long)timeLeft);
            if (isCorrect)
            {
                EventLogger.Log(EventCode.ANSWER_CORRECT, session.SessionID + "");
                return "{\"ResponseCode\":\"" + ResponseCode.CORRECT + "\", \"Message\":\"\"}";
            } else
            {
                EventLogger.Log(EventCode.ANSWER_INCORRECT, session.SessionID + "");
                return "{\"ResponseCode\":\"" + ResponseCode.INCORRECT + "\", \"Message\":\"\"}";
            }
            
        }
    }
}
