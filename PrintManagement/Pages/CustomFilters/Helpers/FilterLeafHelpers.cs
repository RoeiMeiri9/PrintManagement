using Microsoft.UI.Xaml;
using PrintManagement.Models.Entity;
using PrintManagement.Models.Filter;
using PrintManagement.Pages.CustomFilters.Components.TreeViewLeaf;
using PrintManagement.Resources;
using PrintManagement.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrintManagement.Pages.CustomFilters.Helpers;

public partial class FilterLeafHelpers
{
    private static readonly FilterStorageService fss = new();

    public static async Task<FilterLeaf> BuildEntityRootLeafAsync<TEntity>(
    string rootName,
    Icons rootIcon,
    FilterPages pageType,
    int totalItems,
    Func<GenericFilterDefinition, int> countCalculator, // <--- פה ההלפר מקבל את כלי החישוב מהעמוד
    Guid? specialAllId = null,
    IEnumerable<GenericFilterDefinition>? defaultSeedFilters = null)
    where TEntity : EntityModel
    {
        var filters = await fss.GetAllAsync<TEntity>();

        if (!filters.Any() && defaultSeedFilters != null)
        {
            foreach (var def in defaultSeedFilters)
                await fss.SaveAsync<TEntity>(def);
            filters = await fss.GetAllAsync<TEntity>();
        }

        var rootLeaf = new FilterLeaf
        {
            Name = rootName,
            LeafIcon = rootIcon,
            FilterPage = pageType,
            IsRoot = true
        };

        // פילטר "הכל"
        rootLeaf.Children.Add(new FilterLeaf
        {
            ID = specialAllId ?? Guid.NewGuid(),
            Name = $"All {rootName}",
            LeafIcon = Icons.Filter,
            DisplayTotalItems = Visibility.Visible,
            TotalItems = totalItems,
            FilterPage = pageType,
        });

        // הוספת שאר הפילטרים עם החישוב הדינמי שהגיע מהעמוד
        foreach (var filter in filters)
        {
            rootLeaf.Children.Add(new FilterLeaf
            {
                ID = filter.ID,
                Name = filter.Name,
                LeafIcon = Icons.Filter,
                DisplayTotalItems = filter.DisplayTotalItems == true ? Visibility.Visible : Visibility.Collapsed,
                TotalItems = countCalculator(filter), // <--- מפעילים את החישוב כאן!
                FilterPage = pageType,
                Filter = filter
            });
        }

        return rootLeaf;
    }
}
