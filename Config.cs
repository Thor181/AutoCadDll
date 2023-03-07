using System;

namespace ACDll
{
    public static class Config
    {     
        public static string GetLogFileName()
        {
            return $"{Environment.GetEnvironmentVariable("TEMP")}\\log.txt";
        }
    }
}
