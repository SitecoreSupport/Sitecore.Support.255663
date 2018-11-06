namespace Sitecore.Support.Marketing.Search.Observers.DeleteDefinition
{
  using System.Linq;
  using Microsoft.Extensions.Logging;
  using Sitecore.ContentSearch;
  using Sitecore.ContentSearch.Utilities;
  using Sitecore.Data;
  using Sitecore.Framework.Conditions;
  using Sitecore.Globalization;
  using Sitecore.Marketing;
  using Sitecore.Marketing.Definitions;
  using Sitecore.Marketing.ObservableFeed.DeleteDefinition;
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
    private readonly Database database;

    /// <summary>
    /// The search index.
    /// </summary>
    private readonly ISearchIndex searchIndex;

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

      database = Database.GetDatabase(databaseName);
      this.searchIndex = ContentSearchManager.GetIndex(searchIndex);
      this.logger = logger;
    }

    /// <summary>
    /// Gets the database.
    /// </summary>
    public Database Database
    {
      get { return database; }
    }

    /// <summary>
    /// Gets the index.
    /// </summary>
    public ISearchIndex Index
    {
      get { return searchIndex; }
    }

    /// <summary>
    /// Processes the notification.
    /// </summary>
    /// <param name="args">The arguments of delete definition feed.</param>
    public void ProcessNotification(DeleteDefinitionArgs<TDefinition> args)
    {
      Condition.Requires(args, "args").IsNotNull();

      logger.LogInformation("Deleting from marketing definitions search index: '{0}'.", args.Id);

      using (var searchContext = searchIndex.CreateSearchContext())
      {
        searchContext.GetQueryable<TIndexedDefinition>()
            .Where(c => c.Id == args.Id).ToArray()
            .Select(c => new SitecoreItemUniqueId(new ItemUri(c.Id.ToID(), Language.Parse(c.Culture.Name), Version.Parse(c.Version), Database)))
            .ForEach(c => searchIndex.Delete(c));
      }
    }
  }
}
