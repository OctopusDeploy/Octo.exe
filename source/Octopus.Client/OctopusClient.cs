using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Octopus.Client.Exceptions;
using Octopus.Client.Model;
using Octopus.Client.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Octopus.Client.Logging;

namespace Octopus.Client
{
    /// <summary>
    /// The Octopus Deploy RESTful HTTP API client.
    /// </summary>
    public class OctopusClient : IHttpOctopusClient
    {
        private static readonly ILog Logger = LogProvider.For<OctopusClient>();

        readonly OctopusServerEndpoint serverEndpoint;
        readonly JsonSerializerSettings defaultJsonSerializerSettings = JsonSerialization.GetDefaultSerializerSettings();
        readonly HttpClient client;
        readonly CookieContainer cookieContainer = new CookieContainer();
        readonly Uri cookieOriginUri;
        private string antiforgeryCookieName = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctopusClient" /> class.
        /// </summary>
        /// <param name="serverEndpoint">The server endpoint.</param>
        public OctopusClient(OctopusServerEndpoint serverEndpoint) :this(serverEndpoint, null)
        {
        }

        internal OctopusClient(OctopusServerEndpoint serverEndpoint, string requestingTool)
        {
            this.serverEndpoint = serverEndpoint;
            cookieOriginUri = BuildCookieUri(serverEndpoint);
            var handler = new HttpClientHandler()
            {
                CookieContainer = cookieContainer,
                Credentials = serverEndpoint.Credentials ?? CredentialCache.DefaultCredentials
            };
            if (serverEndpoint.Proxy != null)
                handler.Proxy = serverEndpoint.Proxy;

            client = new HttpClient(handler, true)
            {
                Timeout = TimeSpan.FromMilliseconds(ApiConstants.DefaultClientRequestTimeout)
            };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add(ApiConstants.ApiKeyHttpHeaderName, serverEndpoint.ApiKey);
            client.DefaultRequestHeaders.Add("User-Agent", new OctopusCustomHeaders(requestingTool).UserAgent);
            Repository = new OctopusRepository(this);
        }


        public RootResource RootDocument => Repository.LoadRootDocument();
        public IOctopusRepository Repository { get; private set; }

        /// <summary>
        /// Indicates whether a secure (SSL) connection is being used to communicate with the server.
        /// </summary>
        public bool IsUsingSecureConnection => serverEndpoint.IsUsingSecureConnection;

        public IOctopusSpaceRepository ForSpace(SpaceResource space)
        {
            ValidateSpaceId(space);
            return new OctopusRepository(this, RepositoryScope.ForSpace(space));
        }

        public IOctopusSystemRepository ForSystem()
        {
            return new OctopusRepository(this, RepositoryScope.ForSystem());
        }

        public void SignIn(LoginCommand loginCommand)
        {
            if (loginCommand.State == null)
            {
                loginCommand.State = new LoginState { UsingSecureConnection = IsUsingSecureConnection };
            }
            Post(Repository.LoadRootDocument().Links["SignIn"], loginCommand);

            antiforgeryCookieName = cookieContainer.GetCookies(cookieOriginUri)
                .Cast<Cookie>()
                .Single(c => c.Name.StartsWith(ApiConstants.AntiforgeryTokenCookiePrefix)).Name;

            Repository = new OctopusRepository(this);
        }

        public void SignOut()
        {
            Post(Repository.LoadRootDocument().Links["SignOut"]);
            antiforgeryCookieName = null;
        }

        /// <summary>
        /// Occurs when a request is about to be sent.
        /// </summary>
        public event Action<HttpRequestMessage> BeforeSendingHttpRequest;

        /// <summary>
        /// Occurs when a response has been received.
        /// </summary>
        public event Action<HttpResponseMessage> AfterReceivingHttpResponse;

        /// <summary>
        /// Occurs when a request is about to be sent.
        /// </summary>
        public event Action<OctopusRequest> SendingOctopusRequest;

        /// <summary>
        /// Occurs when a response is received from the Octopus Server.
        /// </summary>
        public event Action<OctopusResponse> ReceivedOctopusResponse;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            Repository.Link("Self");
        }

        private Uri BuildCookieUri(OctopusServerEndpoint octopusServerEndpoint)
        {
            // The CookieContainer is a bit funny - it sets the cookie without the port, but doesn't ignore the port when retreiving cookies
            // From what I can see it uses the Uri.Authority value - which contains the port number
            // We need to clear the port in order to successfully get cookies for the same origin
            var uriBuilder = new UriBuilder(octopusServerEndpoint.OctopusServer.Resolve("/")) { Port = 0 };

            return uriBuilder.Uri;
        }

        /// <summary>
        /// Fetches a single resource from the server using the HTTP GET verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path from which to fetch the resource.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>
        /// The resource from the server.
        /// </returns>
        public TResource Get<TResource>(string path, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            return DispatchRequest<TResource>(new OctopusRequest("GET", uri), true).ResponseResource;
        }

        /// <summary>
        /// Fetches a collection of resources from the server using the HTTP GET verb. The collection itself will usually be
        /// limited in size (pagination) and links to the next page of data is available in the <see cref="Resource.Links" />
        /// property.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>
        /// The collection of resources from the server.
        /// </returns>
        public ResourceCollection<TResource> List<TResource>(string path, object pathParameters = null)
        {
            return Get<ResourceCollection<TResource>>(path, pathParameters);
        }

        /// <summary>
        /// Fetches a collection of resources from the server using the HTTP GET verb. All pages will be retrieved.
        /// property.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>
        /// The collection of resources from the server.
        /// </returns>
        public IReadOnlyList<TResource> ListAll<TResource>(string path, object pathParameters = null)
        {
            var resources = new List<TResource>();
            Paginate<TResource>(path, pathParameters, r =>
            {
                resources.AddRange(r.Items);
                return true;
            });
            return resources;
        }

        /// <summary>
        /// Fetches a collection of resources from the server one page at a time using the HTTP GET verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="getNextPage">
        /// A callback invoked for each page of data found. If the callback returns <c>true</c>, the next
        /// page will also be requested.
        /// </param>
        public void Paginate<TResource>(string path, object pathParameters, Func<ResourceCollection<TResource>, bool> getNextPage)
        {
            var page = List<TResource>(path, pathParameters);

            while (getNextPage(page) && page.Items.Count > 0 && page.HasLink("Page.Next"))
            {
                page = List<TResource>(page.Link("Page.Next"));
            }
        }

        /// <summary>
        /// Fetches a collection of resources from the server one page at a time using the HTTP GET verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="getNextPage">
        /// A callback invoked for each page of data found. If the callback returns <c>true</c>, the next
        /// page will also be requested.
        /// </param>
        public void Paginate<TResource>(string path, Func<ResourceCollection<TResource>, bool> getNextPage)
        {
            Paginate(path, null, getNextPage);
        }

        /// <summary>
        /// Creates a resource at the given URI on the server using the POST verb, then performs a fresh GET request to fetch
        /// the created item.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to create.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>
        /// The latest copy of the resource from the server.
        /// </returns>
        public TResource Create<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            var response = DispatchRequest<TResource>(new OctopusRequest("POST", uri, requestResource: resource), true);
            return Get<TResource>(response.Location);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to create.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        public void Post<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            DispatchRequest<TResource>(new OctopusRequest("POST", uri, requestResource: resource), false);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to post.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        public TResponse Post<TResource, TResponse>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            return DispatchRequest<TResponse>(new OctopusRequest("POST", uri, requestResource: resource), true).ResponseResource;
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb.
        /// </summary>
        /// <param name="path">The path to the container resource.</param>
        public void Post(string path)
        {
            var uri = QualifyUri(path);

            DispatchRequest<string>(new OctopusRequest("POST", uri), false);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the PUT verb.
        /// </summary>
        /// <exception cref="OctopusSecurityException">
        /// HTTP 401 or 403: Thrown when the current user's API key was not valid, their
        /// account is disabled, or they don't have permission to perform the specified action.
        /// </exception>
        /// <exception cref="OctopusServerException">
        /// If any other error is successfully returned from the server (e.g., a 500
        /// server error).
        /// </exception>
        /// <exception cref="OctopusValidationException">HTTP 400: If there was a problem with the request provided by the user.</exception>
        /// <exception cref="OctopusResourceNotFoundException">HTTP 404: If the specified resource does not exist on the server.</exception>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to create.</param>
        public void Put<TResource>(string path, TResource resource)
        {
            var uri = QualifyUri(path);

            DispatchRequest<TResource>(new OctopusRequest("PUT", uri, requestResource: resource), false);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the PUT verb.
        /// </summary>
        /// <exception cref="OctopusSecurityException">
        /// HTTP 401 or 403: Thrown when the current user's API key was not valid, their
        /// account is disabled, or they don't have permission to perform the specified action.
        /// </exception>
        /// <exception cref="OctopusServerException">
        /// If any other error is successfully returned from the server (e.g., a 500
        /// server error).
        /// </exception>
        /// <exception cref="OctopusValidationException">HTTP 400: If there was a problem with the request provided by the user.</exception>
        /// <exception cref="OctopusResourceNotFoundException">HTTP 404: If the specified resource does not exist on the server.</exception>
        /// <param name="path">The path to the container resource.</param>
        public void Put(string path)
        {
            var uri = QualifyUri(path);

            DispatchRequest<string>(new OctopusRequest("PUT", uri), false);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the PUT verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to create.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        public void Put<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            DispatchRequest<TResource>(new OctopusRequest("PUT", uri, requestResource: resource), false);
        }

        /// <summary>
        /// Deletes the resource at the given URI from the server using a the DELETE verb.
        /// </summary>
        /// <param name="path">The path to the resource to delete.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="resource">The resource to pass as the request body, if supplied.</param>
        public void Delete(string path, object pathParameters = null, object resource = null)
        {
            var uri = QualifyUri(path, pathParameters);

            DispatchRequest<string>(new OctopusRequest("DELETE", uri, resource), true);
        }

        /// <summary>
        /// Updates the resource at the given URI on the server using the PUT verb, then performs a fresh GET request to reload
        /// the data.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path to the resource to update.</param>
        /// <param name="resource">The resource to update.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>
        /// The latest copy of the resource from the server.
        /// </returns>
        public TResource Update<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            DispatchRequest<TResource>(new OctopusRequest("PUT", uri, requestResource: resource), false);
            return DispatchRequest<TResource>(new OctopusRequest("GET", uri), true).ResponseResource;
        }

        /// <summary>
        /// Fetches raw content from the resource at the specified path, using the GET verb.
        /// </summary>
        /// <exception cref="OctopusSecurityException">
        /// HTTP 401 or 403: Thrown when the current user's API key was not valid, their
        /// account is disabled, or they don't have permission to perform the specified action.
        /// </exception>
        /// <exception cref="OctopusServerException">
        /// If any other error is successfully returned from the server (e.g., a 500
        /// server error).
        /// </exception>
        /// <exception cref="OctopusValidationException">HTTP 400: If there was a problem with the request provided by the user.</exception>
        /// <exception cref="OctopusResourceNotFoundException">HTTP 404: If the specified resource does not exist on the server.</exception>
        /// <param name="path">The path to the resource to fetch.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>A stream containing the content of the resource.</returns>
        public Stream GetContent(string path, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);
            return DispatchRequest<Stream>(new OctopusRequest("GET", uri), true).ResponseResource;
        }

        /// <summary>
        /// Creates or updates the raw content of the resource at the specified path, using the PUT verb.
        /// </summary>
        /// <param name="path">The path to the resource to create or update.</param>
        /// <param name="contentStream">A stream containing content of the resource.</param>
        public void PutContent(string path, Stream contentStream)
        {
            if (contentStream == null) throw new ArgumentNullException("contentStream");
            var uri = QualifyUri(path);
            DispatchRequest<Stream>(new OctopusRequest("PUT", uri, requestResource: contentStream), false);
        }

        public Uri QualifyUri(string path, object parameters = null)
        {
            var dictionary = parameters as IDictionary<string, object>;

            path = (dictionary == null) ? UrlTemplate.Resolve(path, parameters) : UrlTemplate.Resolve(path, dictionary);

            return serverEndpoint.OctopusServer.Resolve(path);
        }

        protected virtual OctopusResponse<TResponseResource> DispatchRequest<TResponseResource>(OctopusRequest request, bool readResponse)
        {
            using var requestMessage = new HttpRequestMessage
            {
                RequestUri = request.Uri,
                Method = new HttpMethod(request.Method)
            };

            if (request.Method == "PUT" || request.Method == "DELETE")
            {
                requestMessage.Method = HttpMethod.Post;
                requestMessage.Headers.Add("X-HTTP-Method-Override", request.Method);
            }

            if (!string.IsNullOrEmpty(antiforgeryCookieName))
            {
                var antiforgeryCookie = cookieContainer.GetCookies(cookieOriginUri)
                    .Cast<Cookie>()
                    .SingleOrDefault(c => string.Equals(c.Name, antiforgeryCookieName));
                if (antiforgeryCookie != null)
                {
                    requestMessage.Headers.Add(ApiConstants.AntiforgeryTokenHttpHeaderName, antiforgeryCookie.Value);
                }
            }

            OnSendingOctopusRequest(request);

            OnBeforeSendingHttpRequest(requestMessage);

            if (request.RequestResource != null)
                requestMessage.Content = GetHttpContent(request);

            Logger.Trace($"DispatchRequest: {requestMessage.Method} {requestMessage.RequestUri}");

            var completionOption = readResponse
                ? HttpCompletionOption.ResponseContentRead
                : HttpCompletionOption.ResponseHeadersRead;

            try
            {
                using var response = client.SendAsync(requestMessage, completionOption).GetAwaiter().GetResult();
                OnAfterReceivingHttpResponse(response);

                if (!response.IsSuccessStatusCode)
                    throw OctopusExceptionFactory.CreateException(response).GetAwaiter().GetResult();

                var resource = readResponse
                    ? ReadResponse<TResponseResource>(response)
                    : default;

                var locationHeader = response.Headers.Location?.OriginalString;
                var octopusResponse = new OctopusResponse<TResponseResource>(request, response.StatusCode, locationHeader, resource);
                OnReceivedOctopusResponse(octopusResponse);

                return octopusResponse;
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException($"Timeout getting response from {request.Uri}, client timeout is set to {client.Timeout}");
            }
        }

        private T ReadResponse<T>(HttpResponseMessage response)
        {
            var content = response.Content;

            if (typeof(T) == typeof(Stream))
            {
                var stream = new MemoryStream();
                content.CopyToAsync(stream).GetAwaiter().GetResult();
                stream.Seek(0, SeekOrigin.Begin);
                return (T)(object)stream;
            }

            if (typeof(T) == typeof(byte[]))
                return (T) (object) content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

            var str = content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (typeof(T) == typeof(string))
                return (T)(object)str;

            try
            {
                return JsonConvert.DeserializeObject<T>(str, defaultJsonSerializerSettings);
            }
            catch (Exception ex)
            {
                throw new OctopusDeserializationException((int)response.StatusCode,
                    $"Unable to process response from server: {ex.Message}. Response content: {(str.Length > 1000 ? str.Substring(0, 1000) : str)}", ex);
            }
        }

        private HttpContent GetHttpContent(OctopusRequest request)
        {
            return request.RequestResource switch
            {
                Stream requestStream => GetStreamContent(requestStream),
                FileUpload fileUpload => GetFileUploadContent(fileUpload),
                _ => GetJsonContent(request.RequestResource)
            };

            static HttpContent GetStreamContent(Stream stream)
            {
                var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                return content;
            }

            static HttpContent GetFileUploadContent(FileUpload fileUpload)
            {
                var formContent = new MultipartFormDataContent();
                var streamContent = new StreamContent(fileUpload.Contents);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                formContent.Add(streamContent, "file", fileUpload.FileName);
                return formContent;
            }

            HttpContent GetJsonContent(object resource)
            {
                var text = JsonConvert.SerializeObject(resource, defaultJsonSerializerSettings);
                return new StringContent(text, Encoding.UTF8, "application/json");
            }
        }

        protected virtual void OnBeforeSendingHttpRequest(HttpRequestMessage request) => BeforeSendingHttpRequest?.Invoke(request);

        protected virtual void OnAfterReceivingHttpResponse(HttpResponseMessage response) => AfterReceivingHttpResponse?.Invoke(response);

        protected virtual void OnSendingOctopusRequest(OctopusRequest request) => SendingOctopusRequest?.Invoke(request);

        protected virtual void OnReceivedOctopusResponse(OctopusResponse response) => ReceivedOctopusResponse?.Invoke(response);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        private void ValidateSpaceId(SpaceResource space)
        {
            if (space == null)
            {
                throw new ArgumentNullException(nameof(space));
            }
        }
    }
}