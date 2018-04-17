namespace SubSync
{
    public struct AuthCredentials
    {
        public readonly string Username;
        public readonly string Password;

        public AuthCredentials(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Username) && string.IsNullOrEmpty(Password);
    }
}