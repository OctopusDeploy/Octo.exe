using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Exceptions;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client
{
    /// <summary>
    /// Contract for a client to the Octopus Deploy HTTP API.
    /// </summary>
    public interface IOctopusAsyncClient : IDisposable
    {
        /// <summary>
        /// Gets a document that identifies the Octopus Server (from /api) and provides links to the resources available on the
        /// server. Instead of hardcoding paths,
        /// clients should use these link properties to traverse the resources on the server. This document is lazily loaded so
        /// that it is only requested once for
        /// the current <see cref="IOctopusAsyncClient" />.
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
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This call is a blocking call. For a non-blocking call, access the root document through the IOctopusSystemAsyncRepository instead: client.Repository.LoadRootDocument()", false)]
        RootResource RootDocument { get; }
        
        /// <summary>
        /// Occurs when a request is about to be sent.
        /// </summary>
        event Action<OctopusRequest> SendingOctopusRequest;

        /// <summary>
        /// Occurs when a response is received from the Octopus Server.
        /// </summary>
        event Action<OctopusResponse> ReceivedOctopusResponse;

        /// <summary>
        /// Occurs when a request is about to be sent.
        /// </summary>
        event Action<HttpRequestMessage> BeforeSendingHttpRequest;

        /// <summary>
        /// Occurs when a response has been received.
        /// </summary>
        event Action<HttpResponseMessage> AfterReceivedHttpResponse;

        /// <summary>
        /// A simplified interface to commonly-used parts of the API.
        /// </summary>
        IOctopusAsyncRepository Repository { get;}

        /// <summary>
        /// Indicates whether a secure (SSL) connection is being used to communicate with the server.
        /// </summary>
        bool IsUsingSecureConnection { get; }

        /// <summary>
        /// Fetches a collection of resources from the server using the HTTP GET verb. The collection itself will usually be
        /// limited in size (pagination) and links to the next page of data is available in the <see cref="Resource.Links" />
        /// property.
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
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>The collection of resources from the server.</returns>
        Task<ResourceCollection<TResource>> List<TResource>(string path, object pathParameters = null, CancellationToken token = default);

        /// <summary>
        /// Fetches a collection of resources from the server using the HTTP GET verb. All result pages will be retrieved.
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
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>The collection of resources from the server.</returns>
        Task<IReadOnlyList<TResource>> ListAll<TResource>(string path, object pathParameters = null, CancellationToken token = default);

        /// <summary>
        /// Fetches a collection of resources from the server one page at a time using the HTTP GET verb.
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
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="getNextPage">
        /// A callback invoked for each page of data found. If the callback returns <c>true</c>, the next
        /// page will also be requested.
        /// </param>
        /// <param name="token">A cancellation token</param>
        /// <returns>The collection of resources from the server.</returns>
        Task Paginate<TResource>(string path, Func<ResourceCollection<TResource>, bool> getNextPage, CancellationToken token = default);

        /// <summary>
        /// Fetches a collection of resources from the server one page at a time using the HTTP GET verb.
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
        /// <param name="path">The path from which to fetch the resources.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="getNextPage">
        /// A callback invoked for each page of data found. If the callback returns <c>true</c>, the next
        /// page will also be requested.
        /// </param>
        /// <param name="token">A cancellation token</param>
        /// <returns>The collection of resources from the server.</returns>
        Task Paginate<TResource>(string path, object pathParameters, Func<ResourceCollection<TResource>, bool> getNextPage, CancellationToken token = default);

        /// <summary>
        /// Fetches a single resource from the server using the HTTP GET verb.
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
        /// <param name="path">The path from which to fetch the resource.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>The resource from the server.</returns>
        Task<TResource> Get<TResource>(string path, object pathParameters = null, CancellationToken token = default);

        /// <summary>
        /// Creates a resource at the given URI on the server using the POST verb, then performs a fresh GET request to fetch
        /// the created item.
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
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>The latest copy of the resource from the server.</returns>
        Task<TResource> Create<TResource>(string path, TResource resource, object pathParameters = null, CancellationToken token = default);

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb.
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
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="token">A cancellation token</param>
        Task Post<TResource>(string path, TResource resource, object pathParameters = null, CancellationToken token = default);

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb, and retrieve the response.
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
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="token">A cancellation token</param>
        Task<TResponse> Post<TResource, TResponse>(string path, TResource resource, object pathParameters = null, CancellationToken token = default);

        /// <summary>
        /// Sends a command to a resource at the given URI on the server using the POST verb.
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
        /// <param name="token">A cancellation token</param>
        Task Post(string path, CancellationToken token = default);

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
        /// <param name="token">A cancellation token</param>
        Task Put(string path, CancellationToken token = default);

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
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="token">A cancellation token</param>
        Task Put<TResource>(string path, TResource resource, object pathParameters = null, CancellationToken token = default);

        /// <summary>
        /// Updates the resource at the given URI on the server using the PUT verb, then performs a fresh GET request to reload
        /// the data.
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
        /// <param name="path">The path to the resource to update.</param>
        /// <param name="resource">The resource to update.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>The latest copy of the resource from the server.</returns>
        Task<TResource> Update<TResource>(string path, TResource resource, object pathParameters = null, CancellationToken token = default);

        /// <summary>
        /// Deletes the resource at the given URI from the server using a the DELETE verb. Deletes in Octopus happen
        /// asynchronously via a background task
        /// that is executed by the Octopus Server. The payload returned by delete will be the task that was created on the
        /// server.
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
        /// <param name="path">The path to the resource to delete.</param>
        /// <param name="pathParameters">If the <c>path</c> is a URI template, parameters to use for substitution.</param>
        /// <param name="resource">An optional resource to pass as the body of the request.</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A task resource that provides details about the background task that deletes the specified resource.</returns>
        Task Delete(string path, object pathParameters = null, object resource = null, CancellationToken token = default);

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
        /// <param name="token">A cancellation token</param>
        /// <returns>A stream containing the content of the resource.</returns>
        Task<Stream> GetContent(string path, object pathParameters = null, CancellationToken token = default);

        /// <summary>
        /// Creates or updates the raw content of the resource at the specified path, using the PUT verb.
        /// </summary>
        /// <param name="path">The path to the resource to create or update.</param>
        /// <param name="contentStream">A stream containing content of the resource.</param>
        /// <param name="token">A cancellation token</param>
        Task PutContent(string path, Stream contentStream, CancellationToken token = default);

        Uri QualifyUri(string path, object parameters = null);

        /// <summary>
        /// Sign in
        /// </summary>
        /// <param name="loginCommand"></param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task SignIn(LoginCommand loginCommand, CancellationToken token = default);

        /// <summary>
        /// Sign out
        /// </summary>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task SignOut(CancellationToken token = default);

        /// <summary>
        /// Get a repository for the given space
        /// </summary>
        /// <param name="space"></param>
        /// <returns></returns>
        IOctopusSpaceAsyncRepository ForSpace(SpaceResource space);

        /// <summary>
        /// Get a repository for system only
        /// </summary>
        /// <returns></returns>
        IOctopusSystemAsyncRepository ForSystem();
    }
}