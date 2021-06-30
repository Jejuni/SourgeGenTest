using System;

namespace SourceGenTest.Entities
{
    public class Pet : Entity<long>
    {
        public Pet(string name, Person person)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Person = person ?? throw new ArgumentNullException(nameof(person));
        }

        public string Name { get; }
        public Person Person { get;}
    }
}