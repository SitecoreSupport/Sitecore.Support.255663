namespace Sitecore.Support.Marketing.Search.Observers.DeleteDefinition
{
  using System;
  using System.Linq;
  using Microsoft.Extensions.Logging;
  using Sitecore.ContentSearch;
  using Sitecore.ContentSearch.Diagnostics;
  using Sitecore.ContentSearch.Utilities;
  using Sitecore.Data;
  using Sitecore.Framework.Conditions;
  using Sitecore.Globalization;
  using Sitecore.Marketing;
  using Sitecore.Marketing.Definitions;
  using Sitecore.Marketing.ObservableFeed.DeleteDefinition;
  using Sitecore.Marketing.Search;
  using Sitecore.Marketing.xMgmt.Extensions;

  /// <summary>
  /// Observer in delete definition feed responsible for deleting definition from search index.
  /// </summary>
  /// <typeparam name="TDefinition">The type of the definition.</typeparam>
  public class DeleteFromSearchIndexObserver<TDefinition> : DeleteFromSearchIndexObserver<TDefinition, TDefinition>
      where TDefinition : IDefinition
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteFromSearchIndexObserver{TDefinition}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="repositoriesSettings">The repositories settings.</param>
    /// <param name="searchSettings">The search settings.</param>
    public DeleteFromSearchIndexObserver([NotNull] ILogger<DeleteFromSearchIndexObserver<TDefinition>> logger, [NotNull] IItemRepositoriesSettings repositoriesSettings, [NotNull] IDefinitionManagerSearchSettings searchSettings)
        : this(logger, Condition.Requires(repositoriesSettings, nameof(repositoriesSettings)).IsNotNull().Value.Database, Condition.Requires(searchSettings, nameof(searchSettings)).IsNotNull().Value.SearchIndexName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteFromSearchIndexObserver{TDefinition}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="searchIndex">The search index name.</param>
    public DeleteFromSearchIndexObserver([NotNull] ILogger<DeleteFromSearchIndexObserver<TDefinition>> logger, string databaseName, string searchIndex)
        : base(logger, databaseName, searchIndex)
    {
    }
  }

  /// <summary>
  /// Observer in delete definition feed responsible for deleting definition from search index.
  /// </summary>
  /// <typeparam name="TDefinition">The type of the definition.</typeparam>
  /// <typeparam name="TIndexedDefinition">The type of the indexed definition.</typeparam>
  public class DeleteFromSearchIndexObserver<TDefinition, TIndexedDefinition> : IDeleteDefinitionObserver<TDefinition>
      where TDefinition : IDefinition
      where TIndexedDefinition : TDefinition
  {
    /// <summary>
    /// The database.
    /// </summary>
    //private readonly Database database;
    private readonly string databaseName;

    /// <summary>
    /// The search index.
    /// </summary>
    // private readonly ISearchIndex searchIndex;
    private readonly string searchIndexName;

    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteFromSearchIndexObserver{TDefinition}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="repositoriesSettings">The repositories settings.</param>
    /// <param name="searchSettings">The search settings.</param>
    public DeleteFromSearchIndexObserver([NotNull] ILogger<DeleteFromSearchIndexObserver<TDefinition, TIndexedDefinition>> logger, [NotNull] IItemRepositoriesSettings repositoriesSettings, [NotNull] IDefinitionManagerSearchSettings searchSettings)
        : this(logger, Condition.Requires(repositoriesSettings, nameof(repositoriesSettings)).IsNotNull().Value.Database, Condition.Requires(searchSettings, nameof(searchSettings)).IsNotNull().Value.SearchIndexName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteFromSearchIndexObserver{TDefinition, TIndexedDefinition}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="searchIndex">The search index name.</param>
    public DeleteFromSearchIndexObserver([NotNull] ILogger<DeleteFromSearchIndexObserver<TDefinition, TIndexedDefinition>> logger, string databaseName, string searchIndex)
    {
      Condition.Requires(logger, nameof(logger)).IsNotNull();
      Condition.Requires(databaseName, "databaseName").IsNotNull();
      Condition.Requires(searchIndex, "searchIndex").IsNotNull();

      this.databaseName = databaseName;
      this.searchIndexName = searchIndex;
      this.logger = logger;
    }

    /// <summary>
    /// Processes the notification.
    /// </summary>
    /// <param name="args">The arguments of delete definition feed.</param>
    public void ProcessNotification(DeleteDefinitionArgs<TDefinition> args)
    {
      var database = Database.GetDatabase(this.databaseName);

      ISearchIndex searchIndex = null;
      try
      {
        searchIndex = ContentSearchManager.GetIndex(this.searchIndexName);
      }
      catch (Exception e)
      {
        SearchLog.Log.Error("Failed to access " + searchIndexName + " search index.", e);
        return;
      }

      Condition.Requires(args, "args").IsNotNull();

      logger.LogInformation("Deleting from marketing definitions search index: '{0}'.", args.Id);

      using (var searchContext = searchIndex.CreateSearchContext())
      {
        searchContext.GetQueryable<TIndexedDefinition>()
            .Where(c => c.Id == args.Id).ToArray()
            .Select(c => new SitecoreItemUniqueId(new ItemUri(c.Id.ToID(), Language.Parse(c.Culture.Name), Data.Version.Parse(c.Version), database)))
            .ForEach(c => searchIndex.Delete(c));
      }
    }
  }
}
