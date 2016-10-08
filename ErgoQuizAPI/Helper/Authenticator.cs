using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ErgoQuizAPI.Helper
{
    public class Authenticator
    {
        public String GetLanID()
        { 
            return "ap\\swongt2".ToLower();
        }
    }
}