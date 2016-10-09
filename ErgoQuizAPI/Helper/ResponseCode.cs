using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ErgoQuizAPI.Helper
{
    public class ResponseCode
    {
        public static readonly string GREETING = "GREETING";
        public static readonly string WELCOME_BACK = "WELCOME_BACK";
        public static readonly string SCOREBOARD = "SCOREBOARD";
        public static readonly string QUESTION = "QUESTION";
        public static readonly string GAMEOVER = "GAMEOVER";
        public static readonly string CORRECT = "CORRECT";
        public static readonly string INCORRECT = "INCORRECT";
        public static readonly string ERROR = "ERROR";
    }
}