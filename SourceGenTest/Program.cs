using System;
using System.Collections.Generic;
using SourceGenTest.Entities;

namespace SourceGenTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var person = new Person("Daniel");
            person.BuyPet("Johnny");
            person.BuyPet("Pinsel");

            foreach(var pet in person.Pets)
                Console.WriteLine($"Pet: {pet.Name}");

            Console.WriteLine("Done");
        }
    }
}