using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using EtsyServicer.DomainObjects;
using EtsyServicer.DomainObjects.Enums;
using EtsyServices.DomainObjects;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace EtsyServices
{
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
                {
                    if (readLine != null) verifier = 
                            readLine.Trim();
                }
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

            var createdListing = AddListing(listing);
            listing.ID = createdListing.ID;

            AddListingImage(listing.ID, listing.Images);
            AddListingFile(listing.ID, listing.DigitalFiles);
            listing.State = ListingStatus.Active;
            createdListing = UpdateListing(listing);
            return createdListing;
        }

        public Listing UpdateListing(Listing listing)
        {
            RestRequest request = new RestRequest($"listings/{listing.ID}", Method.PUT);

            _restClient.Authenticator = OAuth1Authenticator.ForProtectedResource(_token.ApiKey, _token.SharedSecret,
                _token.Key,
                _token.AuthTokenSecret);

            request.AddHeader("Accept", "application/json");
            request.Parameters.Clear();
            //request.AddParameter("application/json", JsonConvert.SerializeObject(listing), ParameterType.RequestBody);
            request.AddParameter("state", "active");
            var etsyResponse = _restClient.Execute(request);
            if (etsyResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(
                    $"Create Listing failed.  Please check your parameters and try again. Error: {etsyResponse.Content}");
            }
            var listingResponse = JsonConvert.DeserializeObject<ListingResponse>(etsyResponse.Content);

            return listingResponse.Listing[0];
        }

        public bool AddListingImage(string listingId, ListingImage[] images)
        {
            RestRequest request = new RestRequest($"listings/{listingId}/images", Method.POST);

            _restClient.Authenticator = OAuth1Authenticator.ForProtectedResource(_token.ApiKey, _token.SharedSecret, _token.Key,
                _token.AuthTokenSecret);

            request.AddHeader("Accept", "application/json");
            request.Parameters.Clear();
            foreach (var image in images)
            {
                request.AddFile("image", image.ImagePath);
                request.AddParameter("application/json", JsonConvert.SerializeObject(images), ParameterType.RequestBody);
                var etsyResponse = _restClient.Execute(request);
                if (etsyResponse.StatusCode != HttpStatusCode.Created)
                {
                    throw new Exception(
                        $"Create Listing Image failed.  Please check your parameters and try again. Error: {etsyResponse.Content}");
                }
            }
            return true;
        }

        public bool AddListingFile(string listingId, DigitalFile[] files)
        {
            RestRequest request = new RestRequest($"listings/{listingId}/files", Method.POST);

            _restClient.Authenticator = OAuth1Authenticator.ForProtectedResource(_token.ApiKey, _token.SharedSecret, _token.Key,
                _token.AuthTokenSecret);

            request.AddHeader("Accept", "application/json");
            request.Parameters.Clear();
            foreach (var file in files)
            {
                request.AddFile("file", file.Path);
                request.AddParameter("name", file.Name);
                request.AddParameter("rank", file.Rank);
                //request.AddParameter("application/json", JsonConvert.SerializeObject(file), ParameterType.RequestBody);
                var etsyResponse = _restClient.Execute(request);
                if (etsyResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(
                        $"Create Listing File failed.  Please check your parameters and try again. Error: {etsyResponse.Content}");
                }
            }
            return true;
        }

        private Listing AddListing(Listing listing)
        {
            RestRequest request = new RestRequest("listings", Method.POST);

            _restClient.Authenticator = OAuth1Authenticator.ForProtectedResource(_token.ApiKey, _token.SharedSecret, _token.Key,
                _token.AuthTokenSecret);

            request.AddHeader("Accept", "application/json");
            request.Parameters.Clear();
            request.AddParameter("application/json", JsonConvert.SerializeObject(listing), ParameterType.RequestBody);

            var etsyResponse = _restClient.Execute(request);
            if (etsyResponse.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception(
                    $"Create Listing failed.  Please check your parameters and try again. Error: {etsyResponse.Content}");
            }
            var listingResponse = JsonConvert.DeserializeObject<ListingResponse>(etsyResponse.Content);
            
            return listingResponse.Listing[0];
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

        public Listing GetListingByID(int listingId)
        {
            RestRequest restRequest = new RestRequest("listings/" + listingId, Method.GET);
            restRequest.AddParameter("api_key", _token.ApiKey);
            IRestResponse irestResponse = _restClient.Execute(restRequest);

            var temp = irestResponse.Content;
            //todo: convert to listing
            return new Listing();
        }
        #endregion
    }
}