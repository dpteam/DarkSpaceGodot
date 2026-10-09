using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Items;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты каталога и генератора предметов.</summary>
public sealed class ItemTests
{
    [Fact]
    public void Catalog_Has20To30Entries()
    {
        Assert.InRange(ItemCatalog.Count, 20, 30);
    }

    [Fact]
    public void Catalog_AllCategoriesRepresented()
    {
        foreach (ItemCategory category in Enum.GetValues<ItemCategory>())
        {
            Assert.NotEmpty(ItemCatalog.ByCategory(category));
        }
    }

    [Fact]
    public void Catalog_AllEntriesValid()
    {
        Assert.All(ItemCatalog.All, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Name));
            Assert.False(string.IsNullOrWhiteSpace(item.Effect));
            Assert.False(string.IsNullOrWhiteSpace(item.Description));
            Assert.True(item.BaseValue >= 0);
            Assert.True(item.Weight > 0);
        });
    }

    [Fact]
    public void Catalog_IdMatchesIndex()
    {
        for (int i = 0; i < ItemCatalog.Count; i++)
        {
            Assert.Equal(i, ItemCatalog.GetById(i).Id);
        }
    }

    [Fact]
    public void Catalog_InvalidId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemCatalog.GetById(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemCatalog.GetById(ItemCatalog.Count));
    }

    [Fact]
    public void GenerateItem_IsDeterministic()
    {
        Item a = ItemGenerator.GenerateItem(12345);
        Item b = ItemGenerator.GenerateItem(12345);
        Assert.Equal(a, b);
    }

    [Fact]
    public void GenerateItem_ProducesValidItems()
    {
        for (ulong seed = 0; seed < 200; seed++)
        {
            Item item = ItemGenerator.GenerateItem(seed);
            Assert.False(string.IsNullOrWhiteSpace(item.Name));
            Assert.True(item.BaseValue >= 0);
            Assert.True(item.Weight > 0);
            Assert.True(Enum.IsDefined(item.Rarity));
        }
    }

    [Fact]
    public void GenerateVariant_KeepsBaseIdAndNeverDowngradesRarity()
    {
        for (int id = 0; id < ItemCatalog.Count; id++)
        {
            Item baseItem = ItemCatalog.GetById(id);
            for (ulong seed = 0; seed < 20; seed++)
            {
                Item variant = ItemGenerator.GenerateVariant(seed, id);
                Assert.Equal(baseItem.Category, variant.Category);
                Assert.True((int)variant.Rarity >= (int)baseItem.Rarity,
                    "Улучшение не должно понижать редкость базового предмета.");
            }
        }
    }

    [Fact]
    public void GenerateVariant_InvalidBaseId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemGenerator.GenerateVariant(1, ItemCatalog.Count));
    }
}
