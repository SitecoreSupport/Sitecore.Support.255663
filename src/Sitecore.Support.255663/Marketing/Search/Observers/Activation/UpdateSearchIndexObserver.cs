using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Sitecore.ContentSearch;
using Sitecore.Data;
using Sitecore.Framework.Conditions;
using Sitecore.Marketing;
using Sitecore.Marketing.Core.ObservableFeed;
using Sitecore.Marketing.Definitions;
using Sitecore.Marketing.ObservableFeed.Activation;
using Sitecore.Marketing.Search;
using Sitecore.Marketing.xMgmt.Extensions;

namespace Sitecore.Support.Marketing.Search.Observers.Activation
{
  public class UpdateSearchIndexObserver<T> : BlockingObserver<T>, IActivationAsyncObserver<T> where T : IDefinition
  {
    /// <summary>
    /// The logger
    /// </summary>
    private readonly ILogger<UpdateSearchIndexObserver<T>> _logger;

    private readonly Database database;

    private readonly ISearchIndex searchIndex;

    [NotNull]
    public Database Database
    {
      get { return database; }
    }

    [NotNull]
    public ISearchIndex Index
    {
      get { return searchIndex; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSearchIndexObserver{T}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="repositoriesSettings">The repositories settings.</param>
    /// <param name="searchSettings">The search settings.</param>
    public UpdateSearchIndexObserver([NotNull] ILogger<UpdateSearchIndexObserver<T>> logger, [NotNull] IItemRepositoriesSettings repositoriesSettings, [NotNull] IDefinitionManagerSearchSettings searchSettings)
        : this(logger, Condition.Requires(repositoriesSettings, nameof(repositoriesSettings)).IsNotNull().Value.Database, Condition.Requires(searchSettings, nameof(searchSettings)).IsNotNull().Value.SearchIndexName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSearchIndexObserver{T}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="searchIndex">The search index name.</param>
    public UpdateSearchIndexObserver([NotNull] ILogger<UpdateSearchIndexObserver<T>> logger, [NotNull] string databaseName, [NotNull] string searchIndex)
    {
      Condition.Requires(logger, nameof(logger)).IsNotNull();
      Condition.Requires(databaseName, nameof(databaseName)).IsNotNull();
      Condition.Requires(searchIndex, nameof(searchIndex)).IsNotNull();

      database = Database.GetDatabase(databaseName);
      this.searchIndex = ContentSearchManager.GetIndex(searchIndex);
      _logger = logger;
    }

    /// <summary>
    /// Synchronous method for processing a received notification.
    /// </summary>
    /// <param name="definitions">Notification data.</param>
    public override void ProcessNotification([NotNull] IReadOnlyCollection<T> definitions)
    {
      Condition.Requires(definitions, "definitions").IsNotNull();
      var indexables = new List<SitecoreItemUniqueId>();
      foreach (var id in definitions.Select(x => x.Id))
      {
        var item = database.GetItem(id);
        Condition.Requires(item, $"Unable to update marketing definitions search index. The definition id is not found: '{id}'.").IsNotNull();

        foreach (var itemUri in item.Versions.GetVersions(true).Select(vi => vi.Uri))
        {
          _logger.LogInformation("Updating marketing definitions search index: '{0}'.", id);
          indexables.Add(new SitecoreItemUniqueId(itemUri));
        }
      }

      searchIndex.Update(indexables);
    }
  }
}