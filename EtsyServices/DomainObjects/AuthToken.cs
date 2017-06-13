namespace EtsyServicer.DomainObjects
{
    public class AuthToken
    {
        public string ApiKey { get; set; }
        public string SharedSecret { get; set; }
        public string AuthTokenSecret { get; set; }
        public string Key { get; set; }

        public AuthToken(string apiKey, string sharedSecret, string authKey, string authSecret)
        {
            ApiKey = apiKey;
            SharedSecret = sharedSecret;
            Key = authKey;
            AuthTokenSecret = authSecret;
        }
    }
}