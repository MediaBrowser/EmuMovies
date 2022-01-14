using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System.IO;
using MediaBrowser.Model.Drawing;

namespace EmuMovies
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin, IHasWebPages, IHasThumbImage
    {
        private const string EmuMoviesApiKey = @"4D8621EE919A13EB6E89B7EDCA6424FC33D6";

        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IConfigurationManager _config;

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "EmuMovies"; }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "emumovies",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.emumovies.html",
                    EnableInMainMenu = true,
                    MenuSection = "server",
                    MenuIcon = "closed_caption"
                },
                new PluginPageInfo
                {
                    Name = "emumoviesjs",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.emumovies.js"
                }
            };
        }

        private Guid _id = new Guid("076204A5-8820-4776-95C4-5F585C41AC12");
        public override Guid Id
        {
            get { return _id; }
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }



        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin" /> class.
        /// </summary>
        public Plugin(IConfigurationManager config, ILogManager logManager, IHttpClient httpClient)
            : base()
        {
            Instance = this;
            _logger = logManager.GetLogger("EmuMovies");
            _httpClient = httpClient;
            _config = config;
        }

        private DateTime _keyDate;
        private string _emuMoviesToken;
        private readonly SemaphoreSlim _emuMoviesApiKeySemaphore = new SemaphoreSlim(1, 1);
        private const double TokenExpirationMinutes = 9.5;

        private bool IsTokenValid
        {
            get
            {
                return !String.IsNullOrEmpty(_emuMoviesToken) &&
                       (DateTime.Now - _keyDate).TotalMinutes <= TokenExpirationMinutes;
            }
        }

        /// <summary>
        /// Gets the EmuMovies token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        public async Task<string> GetEmuMoviesToken(CancellationToken cancellationToken)
        {
            if (!IsTokenValid)
            {
                await _emuMoviesApiKeySemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                // Check if it was set by another thread while waiting
                if (IsTokenValid)
                {
                    _emuMoviesApiKeySemaphore.Release();
                    return _emuMoviesToken;
                }

                try
                {
                    var token = await GetEmuMoviesTokenInternal(cancellationToken).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(token))
                    {
                        _keyDate = DateTime.Now;
                    }

                    _emuMoviesToken = token;
                }
                catch (Exception ex)
                {
                    // Log & throw
                    _logger.ErrorException("Error getting token from EmuMovies", ex);

                    throw;
                }
                finally
                {
                    _emuMoviesApiKeySemaphore.Release();
                }
            }

            return _emuMoviesToken;
        }

        private EmuMoviesOptions GetConfig()
        {
            return _config.GetEmuMoviesOptions();
        }

        /// <summary>
        /// Gets the emu db token internal.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> GetEmuMoviesTokenInternal(CancellationToken cancellationToken)
        {
            var config = GetConfig();

            if (string.IsNullOrEmpty(config.EmuMoviesUsername) || string.IsNullOrEmpty(config.EmuMoviesPassword))
            {
                return null;
            }

            var url = String.Format(EmuMoviesUrls.Login, config.EmuMoviesUsername, config.EmuMoviesPassword, EmuMoviesApiKey);

            try
            {
                using (var response = await _httpClient.SendAsync(new HttpRequestOptions
                {

                    Url = url,
                    CancellationToken = cancellationToken

                }, "GET").ConfigureAwait(false))
                {
                    using (var stream = response.Content)
                    {
                        var doc = new XmlDocument();
                        doc.Load(stream);

                        if (doc.HasChildNodes)
                        {
                            var resultNode = doc.SelectSingleNode("Results/Result");

                            if (resultNode != null && resultNode.Attributes != null)
                            {
                                var sessionId = resultNode.Attributes["Session"].Value;

                                if (sessionId != null)
                                    return sessionId;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.ErrorException("Error retrieving EmuMovies token", e);
            }

            return null;
        }
    }
}
