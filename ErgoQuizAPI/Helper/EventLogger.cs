using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ErgoQuizAPI.Helper
{
    public class EventLogger
    {
        public void Log(int eventID, string value)
        {
            using (var db = new ErgoQuizEntities())
            {
                db.Event.Add(new Event()
                {
                    EventTypeID = eventID,
                    Value = value,
                    At = DateTime.UtcNow
                });
            }
        }
    }
}