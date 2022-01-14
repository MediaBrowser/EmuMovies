using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace EmuMovies
{
    public static class ConfigurationExtension
    {
        public static EmuMoviesOptions GetEmuMoviesOptions(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<EmuMoviesOptions>("emumovies");
        }
    }

    public class EmuMoviesConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new ConfigurationStore[]
            {
                new ConfigurationStore
                {
                    Key = "emumovies",
                    ConfigurationType = typeof (EmuMoviesOptions)
                }
            };
        }
    }

    public class EmuMoviesOptions
    {
        /// <summary>
        /// Gets or sets the EmuMovies Username
        /// </summary>
        public string EmuMoviesUsername { get; set; }

        /// <summary>
        /// Gets or sets the EmuMovies Password
        /// </summary>
        public string EmuMoviesPassword { get; set; }
    }
}
