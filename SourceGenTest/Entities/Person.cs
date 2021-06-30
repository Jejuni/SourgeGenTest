using SourceGen;
using System;
using System.Collections.Generic;

namespace SourceGenTest.Entities
{
    public partial class Person : Entity<long>
    {
        public Person(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }

        [GenerateListAccess(Protected = true)]
        public IReadOnlyList<Pet> Pets { get; } = new List<Pet>();

        [GenerateListAccess]
        public IReadOnlyList<Pet> Derps { get; } = new List<Pet>();

        public void BuyPet(string petName) => AddToPets(new Pet(petName, this));
    }
}