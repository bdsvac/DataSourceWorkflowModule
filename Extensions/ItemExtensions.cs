using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.SecurityModel;

namespace HI.Shared.DataSourceWorkflowModule.Extensions
{
    public static class ItemExtensions
    {	
        private const string ContentEditorUrlFormat = "{0}://{1}/sitecore/shell/Applications/Content Editor?id={2}&amp;sc_content=master&amp;fo={2}&amp;vs={3}&amp;la={4}";

        public static RenderingReference[] GetRenderingReferences(this Item i)
        {
            return i == null ? new RenderingReference[0] : i.Visualization.GetRenderings(Sitecore.Context.Device, false);
        }

        public static List<Item> GetAllUniqueDataSourceItems(this Item i, bool includePersonalizationDataSourceItems = true, bool includeMultiVariateTestDataSourceItems = true)
        {
            var list = new List<Item>();
            foreach (RenderingReference reference in i.GetRenderingReferences())
            {
                list.AddUnqiueDataSourceItem(reference.GetDataSourceItem());

                if (includePersonalizationDataSourceItems)
                {
                    foreach (var dataSourceItem in reference.GetPersonalizationDataSourceItem())
                    {
                        list.AddUnqiueDataSourceItem(dataSourceItem);
                    }
                }

                if (includeMultiVariateTestDataSourceItems)
                {
                    foreach (Item dataSourceItem in reference.GetMultiVariateTestDataSourceItems())
                    {
                        list.AddUnqiueDataSourceItem(dataSourceItem);
                    }
                }
            }
            return list;
        }

        public static List<Item> GetDataSourceItems(this Item i)
        {
            List<Item> list = new List<Item>();
            foreach (RenderingReference reference in i.GetRenderingReferences())
            {
                Item dataSourceItem = reference.GetDataSourceItem();
                if (dataSourceItem != null)
                {
                    list.Add(dataSourceItem);
                }
            }
            return list;
        }

        public static Item GetDataSourceItem(this RenderingReference reference)
        {
            if (reference != null)
            {
                return GetDataSourceItem(reference.Settings.DataSource, reference.Database);
            }
            return null;
        }

        public static List<Item> GetPersonalizationDataSourceItems(this Item i)
        {
            var list = new List<Item>();
            foreach (var reference in i.GetRenderingReferences())
            {
                list.AddRange(reference.GetPersonalizationDataSourceItem());
            }
            return list;
        }

        private static IEnumerable<Item> GetPersonalizationDataSourceItem(this RenderingReference reference)
        {
            var list = new List<Item>();
            if (reference != null && reference.Settings.Rules != null && reference.Settings.Rules.Count > 0)
            {
                list.AddRange(reference.Settings.Rules.Rules.SelectMany(r => r.Actions).OfType<SetDataSourceAction<ConditionalRenderingsRuleContext>>().Select(setDataSourceAction => GetDataSourceItem(setDataSourceAction.DataSource, reference.Database)).Where(dataSourceItem => dataSourceItem != null));
            }
            return list;
        }

        public static List<Item> GetMultiVariateTestDataSourceItems(this Item i)
        {
            var list = new List<Item>();
            foreach (var reference in i.GetRenderingReferences())
            {
                list.AddRange(reference.GetMultiVariateTestDataSourceItems());
            }
            return list;
        }

        private static IEnumerable<Item> GetMultiVariateTestDataSourceItems(this RenderingReference reference)
        {
            var list = new List<Item>();
            if (reference != null && !string.IsNullOrEmpty(reference.Settings.MultiVariateTest))
            {
                using (new SecurityDisabler())
                {
                    // Sitecore 7 to Sitecore 8:
                    //var mvVariateTestForLang = Sitecore.Analytics.Testing.TestingUtils.TestingUtil.MultiVariateTesting.GetVariableItem(reference);
                    
                    // Sitecore 8.1 and above:
                    var contentTestStore = new Sitecore.ContentTesting.Data.SitecoreContentTestStore();
                    var mvVariateTestForLang = contentTestStore.GetMultivariateTestVariable(reference, reference.Language);
                    
                    var variableItem = (mvVariateTestForLang != null) ? mvVariateTestForLang.InnerItem : null;
                    if (variableItem == null) return list;
                    foreach (Item mvChild in variableItem.Children)
                    {
                        var mvDataSourceItem = mvChild.GetInternalLinkFieldItem("Datasource");
                        if (mvDataSourceItem != null)
                        {
                            list.Add(mvDataSourceItem);
                        }
                    }
                }
            }
            return list;
        }

        private static bool ListContainsItem(IEnumerable<Item> list, Item dataSourceItem)
        {
            var uniqueId = dataSourceItem.GetUniqueId();
            return list.Any(e => e.GetUniqueId() == uniqueId);
        }

        private static void AddUnqiueDataSourceItem(this List<Item> list, Item dataSourceItem)
        {
            if (dataSourceItem != null && !ListContainsItem(list, dataSourceItem))
            {
                list.Add(dataSourceItem);
            }
        }

        private static Item GetDataSourceItem(string id, Database db)
        {
            Guid itemId;
            return Guid.TryParse(id, out itemId)
                                    ? db.GetItem(new ID(itemId))
                                    : db.GetItem(id);
        }

        public static string GetContentEditorUrl(this Item i)
        {
            var requestUri = HttpContext.Current.Request.Url;
            return string.Format(ContentEditorUrlFormat, requestUri.Scheme, requestUri.Host, WebUtility.HtmlEncode(i.ID.ToString()), i.Version.Number, i.Language.CultureInfo.Name);
        }

        

        public static Item GetInternalLinkFieldItem(this Item i, string internalLinkFieldName)
        {
            if (i == null) return null;
            InternalLinkField ilf = i.Fields[internalLinkFieldName];
            if (ilf != null && ilf.TargetItem != null)
            {
                return ilf.TargetItem;
            }
            return null;
        }

       
    }
}
