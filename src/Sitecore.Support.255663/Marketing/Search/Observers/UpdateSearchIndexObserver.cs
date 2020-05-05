using System.Collections.Generic;
using System.Linq;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Diagnostics;
using Sitecore.Marketing.Definitions;
using Sitecore.StringExtensions;

namespace Sitecore.Support.Marketing.Search.Observers
{
  public class UpdateSearchIndexObserver<T> : Sitecore.Marketing.Search.Observers.UpdateSearchIndexObserver<T> where T : IDefinition
    {
        private readonly string searchIndexName;

        public UpdateSearchIndexObserver([NotNull] string databaseName, [NotNull] string searchIndex) : base(databaseName, searchIndex)
        {
            Assert.ArgumentNotNull(searchIndex, "searchIndex");
            this.searchIndexName = searchIndex;
        }

        public override void ProcessNotification([NotNull] IReadOnlyCollection<T> definitions)
        {
            Assert.ArgumentNotNull(definitions, "definitions");
            var indexables = new List<SitecoreItemUniqueId>();
            foreach (var id in definitions.Select(x => x.Id))
            {
                var item = base.Database.GetItem(id);
                Assert.IsNotNull(item, "Unable to update marketing definitions search index. The definition id is not found: '{0}'.", id);

                foreach (var itemUri in item.Versions.GetVersions(true).Select(vi => vi.Uri))
                {
                    Log.Info("Updating marketing definitions search index: '{0}'.".FormatWith(id), this);
                    indexables.Add(new SitecoreItemUniqueId(itemUri));
                }
            }

            IndexCustodian.IncrementalUpdate(ContentSearchManager.GetIndex(searchIndexName), indexables);
        }
    }
}