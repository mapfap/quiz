using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ErgoQuizAPI.Helper
{
    public class EventCode
    {
        public static readonly int LOGIN_FAIL = 1;
        public static readonly int LOGIN_FIRST_TIME = 2;
        public static readonly int LOGIN_RETURNING = 3;
        public static readonly int READY = 4;
        public static readonly int READY_TIME_ANOMALY = 5;
        public static readonly int READY_SKIP = 6;
        public static readonly int READY_NO_QUESTION = 7;
        public static readonly int ANSWER_NO_SESSION = 8;
        public static readonly int ANSWER_QID_MISMATCH = 9;
        public static readonly int ANSWER_TIMEOUT = 10;
        public static readonly int ANSWER_TIME_ANOMALY = 11;
        public static readonly int ANSWER_CORRECT = 12;
        public static readonly int ANSWER_INCORRECT = 13;
        public static readonly int LOGIN_SCORE = 14;
    }
}