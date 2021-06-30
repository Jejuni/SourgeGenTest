namespace SourceGenTest.Entities
{
    public interface IEntity<out TId>
        where TId : struct
    {
        TId Id { get; }
    }

    public abstract class Entity<TId> : IEntity<TId> where TId : struct
    {
        protected Entity()
        {
        }

        protected Entity(TId id) => Id = id;
        public TId Id { get; }
    }
}