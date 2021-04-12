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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Octopus.Client.Logging;
using Octopus.Client.Util;

namespace Octopus.Client
{   
    /// <summary>
    /// The Octopus Deploy RESTful HTTP API client.
    /// </summary>
    public class OctopusAsyncClient : IOctopusAsyncClient
    {
        private static readonly ILog Logger = LogProvider.For<OctopusAsyncClient>();
     
        private readonly OctopusServerEndpoint serverEndpoint;
        private readonly JsonSerializerSettings defaultJsonSerializerSettings = JsonSerialization.GetDefaultSerializerSettings();
        private readonly HttpClient client;
        private readonly CookieContainer cookieContainer = new CookieContainer();
        private readonly Uri cookieOriginUri;
        private readonly bool ignoreSslErrors = false;
        private bool ignoreSslErrorMessageLogged = false;
        private string antiforgeryCookieName = null;

        // Use the Create method to instantiate
        protected OctopusAsyncClient(OctopusServerEndpoint serverEndpoint, OctopusClientOptions options, bool addCertificateCallback, string requestingTool)
        {
            var clientOptions = options ?? new OctopusClientOptions();
            this.serverEndpoint = serverEndpoint;
            cookieOriginUri = BuildCookieUri(serverEndpoint);
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                Credentials = serverEndpoint.Credentials ?? CredentialCache.DefaultNetworkCredentials,
            };

            if (clientOptions.Proxy != null)
            {
                handler.UseProxy = true;
                handler.Proxy = new ClientProxy(clientOptions.Proxy, clientOptions.ProxyUsername, clientOptions.ProxyPassword);
            }

#if HTTP_CLIENT_SUPPORTS_SSL_OPTIONS
            handler.SslProtocols = options.SslProtocols;
            if(addCertificateCallback)
            {
                ignoreSslErrors = options.IgnoreSslErrors;
                handler.ServerCertificateCustomValidationCallback = IgnoreServerCertificateCallback;
            }
#endif

            if (serverEndpoint.Proxy != null)
                handler.Proxy = serverEndpoint.Proxy;

            client = new HttpClient(handler, true);
            client.Timeout = clientOptions.Timeout;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add(ApiConstants.ApiKeyHttpHeaderName, serverEndpoint.ApiKey);
            client.DefaultRequestHeaders.Add("User-Agent", new OctopusCustomHeaders(requestingTool).UserAgent);
            Repository = new OctopusAsyncRepository(this);
        }

        private Uri BuildCookieUri(OctopusServerEndpoint octopusServerEndpoint)
        {
            // The CookieContainer is a bit funny - it sets the cookie without the port, but doesn't ignore the port when retreiving cookies
            // From what I can see it uses the Uri.Authority value - which contains the port number
            // We need to clear the port in order to successfully get cookies for the same origin
            var uriBuilder = new UriBuilder(octopusServerEndpoint.OctopusServer.Resolve("/")) {Port = 0};
            return uriBuilder.Uri;
        }

        private bool IgnoreServerCertificateCallback(HttpRequestMessage message, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
            {
                return true;
            }

            var warning = $@"The following certificate errors were encountered when establishing the HTTPS connection to the server: {errors}
Certificate subject name: {certificate.SubjectName.Name}
Certificate thumbprint:   {certificate.Thumbprint}";

            if (ignoreSslErrors)
            {
                if (!ignoreSslErrorMessageLogged)
                {
                    Logger.Warn(warning);
                    Logger.Warn("Because IgnoreSslErrors was set, this will be ignored.");
                    ignoreSslErrorMessageLogged = true;
                }
                return true;
            }

            Logger.Error(warning);
            return false;
        }

        public IOctopusSpaceAsyncRepository ForSpace(SpaceResource space)
        {
            ValidateSpaceId(space);
            return new OctopusAsyncRepository(this, RepositoryScope.ForSpace(space));
        }

        public IOctopusSystemAsyncRepository ForSystem()
        {
            return new OctopusAsyncRepository(this, RepositoryScope.ForSystem());
        }

        public static async Task<IOctopusAsyncClient> Create(OctopusServerEndpoint serverEndpoint, OctopusClientOptions options = null)
        {
#if HTTP_CLIENT_SUPPORTS_SSL_OPTIONS
            try
            {
                return await Create(serverEndpoint, options, true).ConfigureAwait(false);
            }
            catch (PlatformNotSupportedException ex)
            {
                if (options?.IgnoreSslErrors ?? false)
                    throw new Exception("This platform does not support ignoring SSL certificate errors", ex);
                return await Create(serverEndpoint, options, false).ConfigureAwait(false);
            }
#else
            return await Create(serverEndpoint, options, false).ConfigureAwait(false);
#endif
        }

        internal static async Task<IOctopusAsyncClient> Create(OctopusServerEndpoint serverEndpoint, OctopusClientOptions options, string requestingTool)
        {
#if HTTP_CLIENT_SUPPORTS_SSL_OPTIONS
            try
            {
                return await Create(serverEndpoint, options, true, requestingTool);
            }
            catch (PlatformNotSupportedException ex)
            {
                if (options?.IgnoreSslErrors ?? false)
                    throw new Exception("This platform does not support ignoring SSL certificate errors", ex);
                return await Create(serverEndpoint, options, false, requestingTool);
            }
#else
            return await Create(serverEndpoint, options, false, requestingTool);
#endif
        }

        private static async Task<IOctopusAsyncClient> Create(OctopusServerEndpoint serverEndpoint, OctopusClientOptions options, bool addHandler, string requestingTool = null)
        {
            var client = new OctopusAsyncClient(serverEndpoint, options ?? new OctopusClientOptions(), addHandler, requestingTool);
            // User used to see this exception 
            // System.PlatformNotSupportedException: The handler does not support custom handling of certificates with this combination of libcurl (7.29.0) and its SSL backend
            await client.Repository.LoadRootDocument().ConfigureAwait(false);
            return client;
        }

        /// <summary>
        /// Indicates whether a secure (SSL) connection is being used to communicate with the server.
        /// </summary>
        public bool IsUsingSecureConnection => serverEndpoint.IsUsingSecureConnection;

        public async Task SignIn(LoginCommand loginCommand)
        {
            if (loginCommand.State == null)
            {
                loginCommand.State = new LoginState { UsingSecureConnection = IsUsingSecureConnection };
            }
            await Post(await Repository.Link("SignIn").ConfigureAwait(false), loginCommand).ConfigureAwait(false);

            // Capture the cookie name here so that the Dispatch method does not rely on the rootDocument to get the InstallationId
            antiforgeryCookieName = cookieContainer.GetCookies(cookieOriginUri)
                .Cast<Cookie>()
                .Single(c => c.Name.StartsWith(ApiConstants.AntiforgeryTokenCookiePrefix)).Name;

            Repository = new OctopusAsyncRepository(this);
        }

        public async Task SignOut()
        {
            await Post(await Repository.Link("SignOut").ConfigureAwait(false)).ConfigureAwait(false);
            antiforgeryCookieName = null;
        }

        /// <summary>
        /// Occurs when a request is about to be sent.
        /// </summary>
        public event Action<HttpRequestMessage> BeforeSendingHttpRequest;

        /// <summary>
        /// Occurs when a response has been received.
        /// </summary>
        public event Action<HttpResponseMessage> AfterReceivedHttpResponse;

        /// <summary>
        /// Gets a document that identifies the Octopus Server (from /api) and provides links to the resources available on the
        /// server. Instead of hardcoding paths,
        /// clients should use these link properties to traverse the resources on the server. This document is lazily loaded so
        /// that it is only requested once for
        /// the current <see cref="IOctopusAsyncClient" />.
        /// </summary>
        public RootResource RootDocument => Repository.LoadRootDocument().GetAwaiter().GetResult();

        /// <summary>
        /// Occurs when a request is about to be sent.
        /// </summary>
        public event Action<OctopusRequest> SendingOctopusRequest;

        /// <summary>
        /// Occurs when a response is received from the Octopus Server.
        /// </summary>
        public event Action<OctopusResponse> ReceivedOctopusResponse;

        /// <summary>
        /// Fetches a single resource from the server using the HTTP GET verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path from which to fetch the resource.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <returns>
        /// The resource from the server.
        /// </returns>
        public async Task<TResource> Get<TResource>(string path, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            var response = await DispatchRequest<TResource>(new OctopusRequest("GET", uri), true).ConfigureAwait(false);
            return response.ResponseResource;
        }

        public IOctopusAsyncRepository Repository { get; protected set; }

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
        public async Task<ResourceCollection<TResource>> List<TResource>(string path, object pathParameters = null)
        {
            return await Get<ResourceCollection<TResource>>(path, pathParameters).ConfigureAwait(false);
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
        public async Task<IReadOnlyList<TResource>> ListAll<TResource>(string path, object pathParameters = null)
        {
            var resources = new List<TResource>();
            await Paginate<TResource>(path, pathParameters, r =>
            {
                resources.AddRange(r.Items);
                return true;
            }).ConfigureAwait(false);
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
        public async Task Paginate<TResource>(string path, object pathParameters, Func<ResourceCollection<TResource>, bool> getNextPage)
        {
            var page = await List<TResource>(path, pathParameters).ConfigureAwait(false);

            while (getNextPage(page) && page.Items.Count > 0 && page.HasLink("Page.Next"))
            {
                page = await List<TResource>(page.Link("Page.Next")).ConfigureAwait(false);
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
        public Task Paginate<TResource>(string path, Func<ResourceCollection<TResource>, bool> getNextPage)
        {
            return Paginate(path, null, getNextPage);
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
        public async Task<TResource> Create<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            var response = await DispatchRequest<TResource>(new OctopusRequest("POST", uri, requestResource: resource), true).ConfigureAwait(false);
            return await Get<TResource>(response.Location).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to create.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        public Task Post<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            return DispatchRequest<TResource>(new OctopusRequest("POST", uri, requestResource: resource), false);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to post.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        public async Task<TResponse> Post<TResource, TResponse>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);
            var response = await DispatchRequest<TResponse>(new OctopusRequest("POST", uri, requestResource: resource), true).ConfigureAwait(false);
            return response.ResponseResource;
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb.
        /// </summary>
        /// <param name="path">The path to the container resource.</param>
        public Task Post(string path)
        {
            var uri = QualifyUri(path);

            return DispatchRequest<string>(new OctopusRequest("POST", uri), false);
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
        public Task Put<TResource>(string path, TResource resource)
        {
            var uri = QualifyUri(path);

            return DispatchRequest<TResource>(new OctopusRequest("PUT", uri, requestResource: resource), false);
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
        public Task Put(string path)
        {
            var uri = QualifyUri(path);

            return DispatchRequest<string>(new OctopusRequest("PUT", uri), false);
        }

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the PUT verb.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="path">The path to the container resource.</param>
        /// <param name="resource">The resource to create.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        public Task Put<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            return DispatchRequest<TResource>(new OctopusRequest("PUT", uri, requestResource: resource), false);
        }

        /// <summary>
        /// Deletes the resource at the given URI from the server using a the DELETE verb.
        /// </summary>
        /// <param name="path">The path to the resource to delete.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="resource">An optional resource to pass as the body of the request.</param>
        public Task Delete(string path, object pathParameters = null, object resource = null)
        {
            var uri = QualifyUri(path, pathParameters);

            return DispatchRequest<string>(new OctopusRequest("DELETE", uri, resource), true);
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
        public async Task<TResource> Update<TResource>(string path, TResource resource, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);

            await DispatchRequest<TResource>(new OctopusRequest("PUT", uri, requestResource: resource), false).ConfigureAwait(false);
            var result = await DispatchRequest<TResource>(new OctopusRequest("GET", uri), true).ConfigureAwait(false);
            return result.ResponseResource;
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
        public async Task<Stream> GetContent(string path, object pathParameters = null)
        {
            var uri = QualifyUri(path, pathParameters);
            var response = await DispatchRequest<Stream>(new OctopusRequest("GET", uri), true).ConfigureAwait(false);
            return response.ResponseResource;
        }

        /// <summary>
        /// Creates or updates the raw content of the resource at the specified path, using the PUT verb.
        /// </summary>
        /// <param name="path">The path to the resource to create or update.</param>
        /// <param name="contentStream">A stream containing content of the resource.</param>
        public Task PutContent(string path, Stream contentStream)
        {
            if (contentStream == null) throw new ArgumentNullException("contentStream");
            var uri = QualifyUri(path);
            return DispatchRequest<Stream>(new OctopusRequest("PUT", uri, requestResource: contentStream), false);
        }

        public Uri QualifyUri(string path, object parameters = null)
        {
            var dictionary = parameters as IDictionary<string, object>;

            path = (dictionary == null) ? UrlTemplate.Resolve(path, parameters) : UrlTemplate.Resolve(path, dictionary);

            return serverEndpoint.OctopusServer.Resolve(path);
        }

        protected virtual async Task<OctopusResponse<TResponseResource>> DispatchRequest<TResponseResource>(OctopusRequest request, bool readResponse)
        {
            using (var message = new HttpRequestMessage())
            {
                message.RequestUri = request.Uri;
                message.Method = new HttpMethod(request.Method);

                if (request.Method == "PUT" || request.Method == "DELETE")
                {
                    message.Method = HttpMethod.Post;
                    message.Headers.Add("X-HTTP-Method-Override", request.Method);
                }

                if (!string.IsNullOrEmpty(antiforgeryCookieName))
                {
                    var antiforgeryCookie = cookieContainer.GetCookies(cookieOriginUri)
                        .Cast<Cookie>()
                        .SingleOrDefault(c => string.Equals(c.Name, antiforgeryCookieName));
                    if (antiforgeryCookie != null)
                    {
                        message.Headers.Add(ApiConstants.AntiforgeryTokenHttpHeaderName, antiforgeryCookie.Value);
                    }
                }

                OnSendingOctopusRequest(request);

                OnBeforeSendingHttpRequest(message);

                if (request.RequestResource != null)
                    message.Content = GetContent(request);

                Logger.Trace($"DispatchRequest: {message.Method} {message.RequestUri}");

                var completionOption = readResponse
                    ? HttpCompletionOption.ResponseContentRead
                    : HttpCompletionOption.ResponseHeadersRead;
                try
                {
                    using (var response = await client.SendAsync(message, completionOption).ConfigureAwait(false))
                    {
                        OnAfterReceivedHttpResponse(response);

                        if (!response.IsSuccessStatusCode)
                            throw await OctopusExceptionFactory.CreateException(response).ConfigureAwait(false);

                        var resource = readResponse
                            ? await ReadResponse<TResponseResource>(response).ConfigureAwait(false)
                            : default(TResponseResource);

                        var locationHeader = response.Headers.Location?.OriginalString;
                        var octopusResponse = new OctopusResponse<TResponseResource>(request, response.StatusCode,
                            locationHeader, resource);
                        OnReceivedOctopusResponse(octopusResponse);

                        return octopusResponse;
                    }
                }
                catch (TaskCanceledException)
                {
                    throw new TimeoutException($"Timeout getting response from {request.Uri}, client timeout is set to {client.Timeout}.");
                }
            }
        }

        protected virtual void OnSendingOctopusRequest(OctopusRequest request) => SendingOctopusRequest?.Invoke(request);

        protected virtual void OnBeforeSendingHttpRequest(HttpRequestMessage request) => BeforeSendingHttpRequest?.Invoke(request);

        protected virtual void OnAfterReceivedHttpResponse(HttpResponseMessage response) => AfterReceivedHttpResponse?.Invoke(response);

        protected virtual void OnReceivedOctopusResponse(OctopusResponse response) => ReceivedOctopusResponse?.Invoke(response);

        private HttpContent GetContent(OctopusRequest request)
        {
            var requestStreamContent = request.RequestResource as Stream;
            if (requestStreamContent != null)
            {
                var streamContent = new StreamContent(requestStreamContent);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                return streamContent;
            }

            var fileUploadContent = request.RequestResource as FileUpload;
            if (fileUploadContent != null)
            {
                var formContent = new MultipartFormDataContent();
                var streamContent = new StreamContent(fileUploadContent.Contents);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                formContent.Add(streamContent, "file", fileUploadContent.FileName);
                return formContent;
            }

            var text = JsonConvert.SerializeObject(request.RequestResource, defaultJsonSerializerSettings);

            var content = new StringContent(text, Encoding.UTF8, "application/json");
            return content;
        }

        private async Task<T> ReadResponse<T>(HttpResponseMessage response)
        {
            var content = response.Content;

            if (typeof(T) == typeof(Stream))
            {
                var stream = new MemoryStream();
                await content.CopyToAsync(stream).ConfigureAwait(false);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)(object)stream;
            }

            if (typeof(T) == typeof(byte[]))
                return (T)(object)await content.ReadAsByteArrayAsync().ConfigureAwait(false);

            var str = await content.ReadAsStringAsync().ConfigureAwait(false);
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

        private void ValidateSpaceId(SpaceResource space)
        {
            if (space == null)
            {
                throw new ArgumentNullException(nameof(space));
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            client?.Dispose();
        }
    }
}