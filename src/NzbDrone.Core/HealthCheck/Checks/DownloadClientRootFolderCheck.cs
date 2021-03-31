using System;
using System.Linq;
using NLog;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Localization;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ProviderAddedEvent<IDownloadClient>))]
    [CheckOn(typeof(ProviderUpdatedEvent<IDownloadClient>))]
    [CheckOn(typeof(ProviderDeletedEvent<IDownloadClient>))]
    [CheckOn(typeof(ModelEvent<RootFolder>))]
    [CheckOn(typeof(ModelEvent<RemotePathMapping>))]

    public class DownloadClientRootFolderCheck : HealthCheckBase, IProvideHealthCheck
    {
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly IRootFolderService _rootFolderService;
        private readonly Logger _logger;

        public DownloadClientRootFolderCheck(IProvideDownloadClient downloadClientProvider,
                                      IRootFolderService rootFolderService,
                                      Logger logger,
                                      ILocalizationService localizationService)
            : base(localizationService)
        {
            _downloadClientProvider = downloadClientProvider;
            _rootFolderService = rootFolderService;
            _logger = logger;
        }

        public override HealthCheck Check()
        {
            var clients = _downloadClientProvider.GetDownloadClients();
            var rootFolders = _rootFolderService.All();

            foreach (var client in clients)
            {
                try
                {
                    var status = client.GetStatus();
                    var folders = status.OutputRootFolders;
                    if (folders != null)
                    {
                        foreach (var folder in folders)
                        {
                            if (rootFolders.Any(r => r.Path == folder.FullPath))
                            {
                                return new HealthCheck(GetType(), HealthCheckResult.Warning, string.Format(_localizationService.GetLocalizedString("DownloadClientCheckDownloadingToRoot"), client.Definition.Name, folder.FullPath), "#downloads_in_root_folder");
                            }
                        }
                    }
                }
                catch (DownloadClientException ex)
                {
                    _logger.Debug(ex, "Unable to communicate with {0}", client.Definition.Name);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unknown error occured in DownloadClientRootFolderCheck HealthCheck");
                }
            }

            return new HealthCheck(GetType());
        }
    }
}