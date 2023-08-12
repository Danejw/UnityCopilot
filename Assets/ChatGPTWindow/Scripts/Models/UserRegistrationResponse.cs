namespace UnityCopilot
{
    public static partial class APIRequest
    {
        [System.Serializable]
        public class UserRegistrationResponse
        {
            public string username;
            public string email;
            public string full_name;
            public int credits;
            public string access_token;
            public string token_type;
        }

        
    }
}

