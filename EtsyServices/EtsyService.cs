using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using EtsyServicer.DomainObjects;
using RestSharp;
using RestSharp.Authenticators;

namespace EtsyServices
{
    /// <summary>
    /// RestSharp documentation: https://github.com/restsharp/RestSharp/wiki
    /// </summary>
    public class EtsyService : IEtsyService
    {
        #region private fields
        readonly Uri _baseUrl = new Uri("https://openapi.etsy.com/v2/");
        private AuthToken _token;
        private RestClient _restClient;
        private string[] _permissionsArray;
        #endregion
        #region  public properties
        public string Permissions => string.Join(" ", _permissionsArray);
        #endregion

        public void Configure(string[] permissions)
        {
            _restClient = new RestClient(_baseUrl);
            _permissionsArray = permissions;
            var appKey = SettingsHelper.GetAppSetting("ApiKey");
            var sharedSecret = SettingsHelper.GetAppSetting("SharedSecret");
            var authToken = SettingsHelper.GetAppSetting("AuthToken");
            var authSecret = SettingsHelper.GetAppSetting("AuthSecret");

            while (appKey.IsNullOrEmpty())
            {
                Console.Write("Add API Key: ");
                appKey = Console.ReadLine();
                SettingsHelper.SetAppSetting("ApiKey", appKey);
            }

            if (sharedSecret.IsNullOrEmpty())
            {
                Console.Write("Add Shared Secret: ");
                sharedSecret = Console.ReadLine();
                SettingsHelper.SetAppSetting("SharedSecret", sharedSecret);
            }

            _token = new AuthToken(appKey, sharedSecret, authToken, authSecret);


            if (_token.Key.IsNullOrEmpty() || _token.AuthTokenSecret.IsNullOrEmpty())
            {
                Console.WriteLine("Must set auth token.");
                SetAuthToken(appKey, sharedSecret);
            }
        }
        #region private methods
        private void ObtainTokenCredentials(string oAuthTokenTemp, string oAuthTokenSecretTemp, string oauthVerifier, out string permanentOauthToken, out string permanentOauthTokenSecret)
        {
            //consumerKey is the appKey you got when you registered your app, same for sharedSecret
            _restClient.Authenticator = OAuth1Authenticator.ForAccessToken(_token.ApiKey, _token.SharedSecret, oAuthTokenTemp, oAuthTokenSecretTemp, oauthVerifier);

            RestRequest restRequest = new RestRequest("oauth/access_token", Method.GET);
            IRestResponse irestResponse = _restClient.Execute(restRequest);

            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(irestResponse.Content);

            permanentOauthToken = queryString["oauth_token"];
            permanentOauthTokenSecret = queryString["oauth_token_secret"];

            if (permanentOauthToken.IsNullOrEmpty() || permanentOauthTokenSecret.IsNullOrEmpty())
            {
                throw new NullReferenceException("Unable to retrieve permanent auth token.  Please check your credentials and try again.");
            }
        }

        private string GetConfirmUrl(out string oAuthToken, out string oauthTokenSecret, string apiKey, string sharedSecret, string callbackUrl = null)
        {
            _restClient.Authenticator = OAuth1Authenticator.ForRequestToken(apiKey, sharedSecret, callbackUrl ?? "oob");

            RestRequest restRequest = new RestRequest("oauth/request_token", Method.POST);

            restRequest.AddParameter("scope", Permissions);

            IRestResponse response = _restClient.Execute(restRequest);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                oAuthToken = null;
                oauthTokenSecret = null;
                return null;
            }

            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(response.Content);

            oAuthToken = queryString["oauth_token"];
            oauthTokenSecret = queryString["oauth_token_secret"];

            return queryString["login_url"];
        }

        #endregion

        #region public methdods
        private void SetAuthToken(string apiKey, string sharedSecret)
        {
            string token;
            string tempSecret;
            var returnUrl = GetConfirmUrl(out token, out tempSecret, apiKey, sharedSecret);


            Process.Start(returnUrl);
            //break here to get the verifier from Etsy.
            var verifier = string.Empty;
            string authToken;
            string authSecret;

            while (verifier.IsNullOrEmpty())
            {
                Console.Write("Enter verifier from Etsy: ");
                var readLine = Console.ReadLine();
                if (!readLine.IsNullOrEmpty())
                { verifier = readLine.Trim(); }
            }

            ObtainTokenCredentials(token, tempSecret, verifier, out authToken, out authSecret);
            _token.Key = authToken;
            _token.AuthTokenSecret = authSecret;
            SettingsHelper.SetAppSetting("authToken", _token.Key);
            SettingsHelper.SetAppSetting("authSecret", _token.AuthTokenSecret);
        }

        public Listing CreateListing(Listing listing)
        {
            if (!_token.IsValid())
            {
                throw new InvalidOperationException(
                    "Auth token has not been set.  Please set the auth token before calling an authenticated service.");
            }

            RestRequest request = new RestRequest("listings", Method.POST);

            _restClient.Authenticator = OAuth1Authenticator.ForProtectedResource(_token.ApiKey, _token.SharedSecret, _token.Key,
                _token.AuthTokenSecret);

            request.AddParameter("title", "This is a test");
            request.AddParameter("description", "Test Description");
            request.AddParameter("status", "draft");
            request.AddParameter("quantity", "1");
            request.AddParameter("price", "1");
            request.AddParameter("is_supply", "false");
            request.AddParameter("category_id", "68887420");
            request.AddParameter("when_made", "2010_2017");
            request.AddParameter("who_made", "i_did");
            request.AddParameter("is_digital", true);
            request.AddParameter("shipping_template_id", 30116314577);
            var etsyResponse = _restClient.Execute(request);
            if (etsyResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(string.Format("Create Listing failed.  Please check your parameters and try again. Error: {0}", etsyResponse.Content));
            }

            return listing;
        }

        public string GetPermissionScopes()
        {
            if (!_token.IsValid())
            {
                throw new InvalidOperationException(
                    "Auth token has not been set.  Please set the auth token before calling an authenticated service.");
            }
            _restClient.Authenticator = OAuth1Authenticator.ForProtectedResource(_token.ApiKey, _token.SharedSecret, _token.Key, _token.AuthTokenSecret);

            RestRequest restRequest = new RestRequest("oauth/scopes", Method.GET);

            IRestResponse irestResponse = _restClient.Execute(restRequest);

            return irestResponse.Content;
        }
        #endregion
    }
}