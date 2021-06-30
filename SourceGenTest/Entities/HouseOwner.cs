using System.Collections.Generic;
using SourceGen;

namespace SourceGenTest.Entities
{
    public abstract partial class HouseOwner
    {
        [GenerateListAccess(Protected = true)] public IReadOnlyList<House> Houses { get; } = new List<House>();
    }

    public partial class FancyHouseOwner : HouseOwner
    {
        [GenerateListAccess] public IReadOnlyList<House> MoreHouses { get; } = new List<House>();

        [GenerateListAccess(Protected = true)] public IReadOnlyList<Pet> Pets { get; } = new List<Pet>();

        public void Do()
        {
            AddToHouses(new House());
            AddToMoreHouses(new House());
        }
    }
}