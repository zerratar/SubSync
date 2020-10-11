﻿using System;
using System.IO;

namespace SubSyncLib.Logic
{
    public class FileBasedCredentialsProvider : IAuthCredentialProvider
    {
        private readonly string filename;
        private readonly ILogger logger;
        private bool synchronizedWithFile;
        private string username;
        private string password;

        public FileBasedCredentialsProvider(string filename, ILogger logger)
        {
            this.filename = filename;
            this.logger = logger;
        }

        private void ReadFile()
        {
            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Path.Combine(appPath, filename);
            if (!System.IO.File.Exists(filePath))
            {
                return;
            }
            // storing the user/pass is generally a very bad idea as you could find the values by looking in the application memory.
            // But since I highly doubt anyone is going to go that far to write a virus or app to read the memory of SubSync just to steal someones subtitle provider passwords. lol
            try
            {
                var lines = System.IO.File.ReadAllLines(filename);
                foreach (var line in lines)
                {
                    var splitPos = line.IndexOf('=');
                    var key = line.Substring(0, splitPos);
                    var value = line.Substring(splitPos+1, line.Length - splitPos - 1);
                    if (key.Equals("username", StringComparison.CurrentCultureIgnoreCase))
                    {
                        username = value;
                    }
                    else if (key.Equals("password", StringComparison.CurrentCultureIgnoreCase))
                    {
                        password = value;
                    }
                }
                synchronizedWithFile = true;
            }
            catch (Exception exc)
            {
                logger.WriteLine("@yel@Unable to read opensubtitles.auth! username and password will be left blank!");
            }

        }

        public AuthCredentials Get()
        {
            if (!synchronizedWithFile)
            {
                // in case it previously failed or file was added after the application started.
                ReadFile();
            }

            return new AuthCredentials(username, password);
        }
    }
}